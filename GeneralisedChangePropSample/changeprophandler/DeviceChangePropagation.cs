// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using Azure.Core.Pipeline;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Azure;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Company.Function
{
    public static class DeviceChangePropagation
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static string adtServiceUrl = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");

        [FunctionName("DeviceChangePropagation")]
        public static async Task Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            DigitalTwinsClient client;
            // Authenticate on ADT APIs
            try
            {
                var credentials = new DefaultAzureCredential();
                client = new DigitalTwinsClient(new Uri(adtServiceUrl), credentials, new DigitalTwinsClientOptions { Transport = new HttpClientTransport(httpClient) });
                log.LogInformation("ADT service client connection created.");
            }
            catch (Exception e)
            {
                log.LogError($"ADT service client connection failed. {e}");
                return;
            }

            if (client != null)
            {
                if (eventGridEvent != null && eventGridEvent.Data != null)
                {
                    string twinId = eventGridEvent.Subject.ToString();
                    JObject message = (JObject)JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());

                    log.LogInformation($"Reading event from {twinId}: {eventGridEvent.EventType}: {message["data"]}");

                    List<BasicRelationship> defineSourceRels = await FindDefinedbyRelations(client, twinId, log);
                    if ( (defineSourceRels != null) && (defineSourceRels.Count > 0)) {

                        // Read properties which values have been changed in each operation
                        foreach (var operation in message["data"]["patch"])
                        {
                            string opValue = (string)operation["op"];
                            if (opValue.Equals("replace") || opValue.Equals("add"))
                            {
                                string propertyPath = ((string)operation["path"]);
                                foreach( BasicRelationship rel in defineSourceRels) {

                                    if (rel.Properties["devicePropPath"].ToString().Equals(propertyPath)) {
                                        await UpdateTwinPropertyAsync(client, rel.SourceId, rel.Properties["propPath"].ToString(), operation["value"].Value<float>(), log);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static async Task<List<BasicRelationship>> FindDefinedbyRelations(DigitalTwinsClient client, string child, ILogger log)
        {
            List<BasicRelationship> result = new List<BasicRelationship>();

            try
            {
                AsyncPageable<IncomingRelationship> rels = client.GetIncomingRelationshipsAsync(child);

                await foreach (IncomingRelationship ie in rels)
                {
                    if (ie.RelationshipName == "definedby")
                    {
                        result.Add(await client.GetRelationshipAsync<BasicRelationship>(ie.SourceId, ie.RelationshipId));
                    }
                }
            }
            catch (RequestFailedException exc)
            {
                log.LogInformation($"*** Error in retrieving parent:{exc.Status}:{exc.Message}");
            }

            return result;
        }
        private static async Task UpdateTwinPropertyAsync(DigitalTwinsClient client, string twinId, string propertyPath, object value, ILogger log)
        {
            // If the twin does not exist, this will log an error
            try
            {
                var updateTwinData = new JsonPatchDocument();
                updateTwinData.AppendReplace(propertyPath, value);

                log.LogInformation($"UpdateTwinPropertyAsync sending {updateTwinData}");
                await client.UpdateDigitalTwinAsync(twinId, updateTwinData);
            }
            catch (RequestFailedException exc)
            {
                log.LogInformation($"*** Error:{exc.Status}/{exc.Message}");
            }
        }
    }
}
