using System;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Query;

namespace MyPlagins
{
    public class RentingsChecker : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Extract the tracing service for use in debugging sandboxed plug-ins.  
            // If you are not registering the plug-in in the sandbox, then you do  
            // not have to add any tracing service related code.  
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Obtain the organization service reference which you will need for  
            // web service calls.  
            IOrganizationServiceFactory serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);



            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity entity = (Entity)context.InputParameters["Target"];

                try
                {
                    // Plug-in business logic goes here.  
                    EntityReference customer = entity.GetAttributeValue<EntityReference>("new_customer");
                    if (entity.Contains("new_customer") == false)
                    {
                        Entity preImage = (Entity)context.PreEntityImages["Image"];
                        customer = preImage.GetAttributeValue<EntityReference>("new_customer");
                    }

                    int currentStatusValue = entity.GetAttributeValue<OptionSetValue>("statuscode").Value;

                    int statusValue = 100000002;
                    int maxRecords = 10;

                    QueryExpression query = new QueryExpression("new_rent");
                    query.ColumnSet = new ColumnSet(new string[] { "statuscode", "new_customer" });
                    query.Criteria.AddCondition("statuscode", ConditionOperator.Equal, statusValue);
                    query.Criteria.AddCondition("new_customer", ConditionOperator.Equal, customer.Id);

                    EntityCollection collection = service.RetrieveMultiple(query);

                    if (collection.Entities.Count >= maxRecords && currentStatusValue == statusValue)
                    {
                        throw new InvalidPluginExecutionException("Prohibited creation of more than 10 rents in status Renting per one owner");
                    }
                }

                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in MyPlug-in.", ex);
                }

                catch (Exception ex)
                {
                    tracingService.Trace("MyPlugin: {0}", ex.ToString());
                    throw;
                }
            }
        }
    }
}
