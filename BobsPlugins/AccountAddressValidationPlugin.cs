using System;
using System.Linq;
using System.Net.Http;
using System.ServiceModel;
using System.Web;
using System.Xml.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace BobsPlugins
{
    public class AccountAddressValidationPlugin : IPlugin
    {
        private const string _USPS_USERID = "152IMPRO8029";
        private const string _USPS_BASEURL = "https://secure.shippingapis.com/ShippingAPI.dll";

        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity entity = (Entity)context.InputParameters["Target"];

                // Obtain the organization service reference which you will need for  
                // web service calls.  
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try
                {
                    tracingService.Trace("Starting AccountAddressValidation");
                    // Plug-in business logic goes here.  
                    if (entity != null && entity.LogicalName.Equals("account"))
                    {
                        tracingService.Trace("Account identitfied");

                        // on UPDATE only got the updated field...

                        var address = new Address(entity);
                        if (context.MessageName.Equals("Update"))
                        {
                            tracingService.Trace("Retrieving Address from UPDATE:");
                            var currentAddressFromDynamics = service.Retrieve("account", address.Id, new ColumnSet("address1_line1", "address1_line2", "address1_city", "address1_stateorprovince", "address1_postalcode"));
                            var currentAddress = new Address(currentAddressFromDynamics);
                            address.MergeAddresses(currentAddress);
                        }

                        var xml = $@"<AddressValidateRequest USERID='{_USPS_USERID}'> 
                                    <Revision> 1 </Revision>
                                    <Address ID = '0'>
                                    <Address1>{address.Line2}</Address1>
                                    <Address2>{address.Line1}</Address2>
                                    <City>{address.City}</City>
                                    <State>{address.State}</State>
                                    <Zip5>{address.PostalCode}</Zip5>
                                    <Zip4 />
                                    </Address>
                                    </AddressValidateRequest>";
                        var encodedXML = HttpUtility.UrlEncode(xml);
                        tracingService.Trace($"AccountAddressValidation XML: {xml}");
                        
                        using (var client = new HttpClient())
                        {
                            tracingService.Trace("Making GET call");
                            var response = client.GetAsync($"{_USPS_BASEURL}?API=VERIFY&XML={encodedXML}").Result;
                            if (response.IsSuccessStatusCode)
                            {
                                tracingService.Trace("Successful GET Call");
                                var content = response.Content.ReadAsStringAsync().Result;
                                var responseXML = XDocument.Parse(content);

                                string errorDescription;
                                if (USPSXmlHelper.ErrorExists(responseXML, out errorDescription))
                                    throw new InvalidPluginExecutionException(string.IsNullOrWhiteSpace(errorDescription) ? "Invalid Address" : errorDescription);

                                var validAddress = USPSXmlHelper.GetAddress(responseXML);

                                entity["address1_line1"] = validAddress.Line1;
                                entity["address1_line2"] = validAddress.Line2;
                                entity["address1_city"] = validAddress.City;
                                entity["address1_stateorprovince"] = validAddress.State;
                                entity["address1_postalcode"] = validAddress.PostalCode;
                            }
                            else
                            {
                                tracingService.Trace("GET call failed");
                                throw new InvalidPluginExecutionException("Something went wrong calling the USPS api."); // CHANGE THIS
                            }
                        }
                    }
                    tracingService.Trace("Got to end of plugin");
                }

                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in FollowUpPlugin.", ex);
                }

                catch (Exception ex)
                {
                    tracingService.Trace("AccountAddressValidationPlugin: {0}", ex.ToString());
                    throw;
                }
            }
        }
    }
}
