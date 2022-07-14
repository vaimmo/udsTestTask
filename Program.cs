using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.carsByCarClasss.Generic;
using System.Linq;

namespace TestDynamics365Application
{
    class Program
    {
        //Authenticate
        private static string connectionString = @"AuthType=OAuth; Username=; Password=; Url=; 
            AppId=51f81489-12ee-4a9e-aaae-a2591f45987d; RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;";

        private static CrmServiceClient servise;

        public static Random random = new Random();

        static void Main(string[] args)
        {
            servise = new CrmServiceClient(connectionString);

            var programStart = DateTime.Now;
            var carClasses = GetSpecificEntityList("new_carclass"); //carClassRef Entiny name = "new_carclass"
            var cars = GetSpecificEntityList("new_car"); //car Entiny name = "new_car"
            var customers = GetSpecificEntityList("contact"); //customer Entity name = "contact"
            var rentsCount = GetSpecificEntityList("new_rent").Count; //to know how many entitis exist

            Console.WriteLine("All data have been resived");
            Console.WriteLine($"Now you have {rentsCount} of entities\nWrite how many entities you need to create");
            var needToCreate = Convert.ToInt32(Console.ReadLine());

            for (int i = rentsCount; i < needToCreate; i++) //Need to create 40 000 Rent Entities
            {
                var reservedPickup = GetRangomPickupDate();
                var reservedHandover = GetRandomHandoverDate(reservedPickup);
                Entity rent = new Entity("new_rent");
                rent["new_reservedpickup"] = reservedPickup;
                rent["new_reservedhandover"] = reservedHandover;
                rent["new_carclassrent"] = GetRandomCarentEntity(carClasses);
                rent["new_car_rent"] = GetRandomCarWithCarClassRespect(rent.GetAttributeValue<EntityReference>("new_carclassrent"), cars);
                rent["new_customer"] = GetRandomCarentEntity(customers);
                rent["new_pickuplocation"] = GetRandomPickupOrHandoverLocation();
                rent["new_returnlocation"] = GetRandomPickupOrHandoverLocation();
                rent["statuscode"] = GetRandomStatus(rent);
                rent["new_price_rent"] = HowMachCustomerPaid(carClasses, rent.GetAttributeValue<EntityReference>("new_carclassrent"), reservedPickup, reservedHandover);

                servise.Create(rent);
                Console.WriteLine($"{i} entity created");
            }
            Console.WriteLine($" Programm start at {programStart}, and worked {TimeSpan.FromMinutes(DateTime.Now.Minute - programStart.Minute)}");
            Console.WriteLine("Seems all is done well");
            Console.ReadKey();
        }

        public static DateTime GetRangomPickupDate()
        {
            //pickup date/time should be somewhere between 1.1.2019 and 31.12.2020 (2 years or 731 days)
            var startDate = new DateTime(2019, 1, 1);
            //we have 1440 minutes in hour
            return startDate.AddDays(random.Next(0, 730)).AddMinutes(random.Next(0, 1439));
        }

        public static DateTime GetRandomHandoverDate(DateTime startDate)
        {
            //handover maximum 30 days
            return startDate.AddDays(random.Next(1, 29)).AddMinutes(random.Next(0, 1439));
        }

        public static List<Entity> GetSpecificEntityList(string entityName)
        {
            var service = servise;

            using (OrganizationServiceContext context = new OrganizationServiceContext(service))
            {
                var entitycarsByCarClass = from c in context.CreateQuery(entityName)
                                       select c;

                return entitycarsByCarClass.ToList();
            }
        }

        public static EntityReference GetRandomCarentEntity(List<Entity> entities)
        {
            return entities[random.Next(0, entities.Count())].ToEntityReference();
        }

        public static EntityReference GetRandomCarWithCarClassRespect(EntityReference carClassRef, List<Entity> cars)
        {
            var carsByCarClass = cars.Where(c => c.GetAttributeValue<EntityReference>("new_carclass").Id == carClassRef.Id);

            var newcarsByCarClass = carsByCarClass.ToList();

            return newcarsByCarClass[random.Next(0, newcarsByCarClass.Count())].ToEntityReference();
        }

        private static EntityReference CreateTransferReport(Entity rent, string resevedPickupHandovetDate, bool conditionType)
        {
            Entity report = new Entity("new_cartransferreport");
            report["new_car_lookup"] = rent.GetAttributeValue<EntityReference>("new_car_rent");
            report["new_type"] = conditionType; //return = true , pickup = false
            report["new_date"] = rent.GetAttributeValue<DateTime>(resevedPickupHandovetDate); //new_reservedhandover , new_reservedpickup
            report["new_damages"] = GetRandomProbabilyty(5); //Damage set YES with probability 5%
            
            if (report.GetAttributeValue<bool>("new_damages") == true)
            {
                report["new_damagedescription"] = "damage";
            }

            report.Id = servise.Create(report);
            return report.ToEntityReference();
        }

        public static OptionSetValue GetRandomPickupOrHandoverLocation()
        {
            var locations = new int[] { 100000000, 100000001, 100000002 };

            return new OptionSetValue(locations[random.Next(locations.Length)]);
        }

        public static OptionSetValue GetRandomStatus(Entity entity)
        {
            var a = random.Next(1, 1000);

            // statecode 1 = inactive ; statecode 0 = active

            if (a <= 100)
            {
                entity["statecode"] = new OptionSetValue(1);
                return new OptionSetValue((int)RentStatuses.Canceled);
            }

            if (a > 100 && 150 >= a)
            {
                return new OptionSetValue((int)RentStatuses.Created);
            }

            if (a > 150 && 200 >= a)
            {
                entity["new_paid"] = GetRandomProbabilyty(90); //paid with probability 90%
                return new OptionSetValue((int)RentStatuses.Confirmed);
            }

            if (a > 200 && 250 >= a)
            {
                entity["new_pickupreportlookup"] = CreateTransferReport(entity, "new_reservedpickup", false);
                entity["new_actualpickup"] = entity.GetAttributeValue<DateTime>("new_reservedpickup");
                entity["new_paid"] = GetRandomProbabilyty(99.9); //paid with probability 99.9%
                return new OptionSetValue((int)RentStatuses.Renting);
            }

            else
            {
                entity["statecode"] = new OptionSetValue(1);
                entity["new_pickupreportlookup"] = CreateTransferReport(entity, "new_reservedpickup", false);
                entity["new_returnreportlookup"] = CreateTransferReport(entity, "new_reservedhandover", true);
                entity["new_actualpickup"] = entity.GetAttributeValue<DateTime>("new_reservedpickup");
                entity["new_actualreturn"] = entity.GetAttributeValue<DateTime>("new_reservedhandover");
                entity["new_paid"] = GetRandomProbabilyty(99.98); //paid with probability 99.98%
                return new OptionSetValue((int)RentStatuses.Returned);
            }
        }

        private static bool GetRandomProbabilyty(double probabilityPercent)
        {
            var a = random.Next(1, 10000) => probabilityPercent * 100 ? true : false;

            return a;
        }

        private static Money HowMachCustomerPaid(List<Entity> carsClassList, EntityReference carClassRef, DateTime startDate, DateTime endDate)
        {
            var rentedDays = endDate - startDate;

            var currentCar = carsClassList.FirstOrDefault(c => c.Id == carClassRef.Id);

            return new Money(currentCar.GetAttributeValue<Money>("new_price").Value * (decimal)rentedDays.TotalDays);
        }
    }
    
    public enum RentStatuses
    {
            Created = 100000000, //should be 5% probability
            Confirmed = 100000001, //should be 5% probability
            Renting = 100000002, //should be 5% probability
            Canceled = 100000005, //should be 10% probability
            Returned = 2 //should be 75% probability
    }
}
