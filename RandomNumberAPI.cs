using Microsoft.Xrm.Sdk;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RandomNumberWebApi
{
    public class RandomNumberAPI : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the execution context from the service provider
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Obtain the tracing service for debugging
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("Plugin execution started.");

            try
            {
                // Check if the plugin is triggered on an update of the "Approval Status"
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    Entity entity = (Entity)context.InputParameters["Target"];

                    // Check if the approval status is "In Review" and if the risk score is not already set
                    if (entity.Contains("contoso_applicationstatus") && ((OptionSetValue)entity["contoso_applicationstatus"]).Value == 463270001) // Replace with your actual value
                    {
                        if (!entity.Contains("contoso_riskscores") || (int)entity["contoso_riskscores"] == 0) // Only update if not already set
                        {
                            tracingService.Trace("Approval status is 'In Review' and risk score is not set.");

                            // Retrieve the random number from the API
                            using (HttpClient client = new HttpClient())
                            {
                                client.BaseAddress = new Uri("https://sdcbb12-aaaqgvhhgga7b6hc.southindia-01.azurewebsites.net/api/");
                                HttpResponseMessage response = client.GetAsync("WeatherForecast").Result;

                                if (response.IsSuccessStatusCode)
                                {
                                    string apiResponse = response.Content.ReadAsStringAsync().Result;
                                    tracingService.Trace("API response received: " + apiResponse);

                                    // Ensure the API response contains the expected field
                                    JObject jsonResponse = JObject.Parse(apiResponse);
                                    if (jsonResponse["number"] != null)
                                    {
                                        int randomNumber = int.Parse(jsonResponse["number"].ToString());

                                        // Update the entity with the random number as the risk score
                                        entity["contoso_riskscores"] = randomNumber;

                                        tracingService.Trace("Updating entity with risk score: " + randomNumber);

                                        // Update the record in the CRM database
                                        IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                                        IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                                        service.Update(entity);

                                        tracingService.Trace("Entity updated successfully.");
                                    }
                                    else
                                    {
                                        tracingService.Trace("API response does not contain 'number' field.");
                                        throw new InvalidPluginExecutionException("API response does not contain 'number' field.");
                                    }
                                }
                                else
                                {
                                    tracingService.Trace("Failed to retrieve random number from API. Status Code: " + response.StatusCode);
                                    throw new InvalidPluginExecutionException("Failed to retrieve random number from API. Status Code: " + response.StatusCode);
                                }
                            }
                        }
                        else
                        {
                            tracingService.Trace("Risk score is already set. No action taken.");
                        }
                    }
                    else
                    {
                        tracingService.Trace("Approval status is not 'In Review'. No action taken.");
                    }
                }
                else
                {
                    tracingService.Trace("Target is not an Entity.");
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace("Exception: " + ex.Message);
                throw new InvalidPluginExecutionException("An error occurred in the UpdateRiskScorePlugin: " + ex.Message);
            }
        }
    }
}