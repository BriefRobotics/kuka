using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace I.Do.Relay
{
    public class Relay
    {
        private const string apiBase = "/api/v2";

        private readonly Uri rocUri;
        private readonly string apiToken;

        public Relay(string rocUri, string apiToken)
        {
            this.rocUri = new Uri(rocUri);
            this.apiToken = apiToken;
        }

        public async Task<string> QueueTask(string program, string queue, int priority = 5, string args = null)
        {
            var client = new HttpClient() { BaseAddress = rocUri };
            client.DefaultRequestHeaders.Add("X-API-TOKEN", apiToken);
            var content = new FormUrlEncodedContent(new KeyValuePair<string, string>[] {
                new KeyValuePair<string, string>("program", program),
                new KeyValuePair<string, string>("queueName", queue),
                new KeyValuePair<string, string>("args", args),
            });
            var response = await client.PostAsync($"{apiBase}/tasks", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        /* unused public string QueueDelivery(string args, string queue = "api", int priority = 5)
        {
            // for args, see http://docs.savioke.com/api-dev/#api-Tasks-NewDelivery
            // TODO: higher-level than JObject
            var result = QueueTask("Delivery", queue, priority, args);
            result.Wait();
            return result.Result;
        } */

        public string QueueGoto(string place, string queue, string message = "Hello!", int priority = 5)
        {
            var result = QueueTask("Goto", queue, priority, $"{{\"location\":\"{place}\",\"timeout\":1}}");
            result.Wait();
            return result.Result;
        }

        public string QueueWander(IEnumerable<string> places, string queue, string message = "Hello!", int priority = 5)
        {
            var locs = string.Join(",", places);
            var result = QueueTask("Wander", queue, priority, $"{{\"locations\":[{locs}]}}");
            result.Wait();
            return result.Result;
        }

        public async void CancelTask(string id)
        {
            var client = new HttpClient() { BaseAddress = rocUri };
            client.DefaultRequestHeaders.Add("X-API-TOKEN", apiToken);
            var response = await client.DeleteAsync($"{apiBase}/tasks/{id}");
            response.EnsureSuccessStatusCode();
            var foo = await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetApiAsync(string api) // TODO: higher-level than JObject
        {
            var client = new HttpClient() { BaseAddress = rocUri };
            client.DefaultRequestHeaders.Add("X-API-TOKEN", apiToken);
            var response = await client.GetAsync($"{apiBase}/{api}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }

        public string GetApi(string api)
        {
            var result = GetApiAsync(api);
            result.Wait();
            return result.Result;
        }

        /* NOT USED public string GetAllPlaces() // TODO: higher-level than JObject
        {
            return GetApi("places");
        }*/

        /* NOT USED public string GetPlaces(string id) // TODO: higher-level than JObject
        {
            return GetApi($"places/:{id}");
        }*/

        /* NOT USED public string GetAllRobots() // TODO: higher-level than JObject
        {
            return GetApi("robots");
        }*/

        /* NOT USED public string GetRobot(string id) // TODO: higher-level than JObject
        {
            return GetApi($"robots/:{id}");
        }*/

        public string GetAllTasks() // TODO: higher-level than JObject
        {
            // TODO: on given queue?!
            return GetApi("tasks");
        }

        /* NOT USED public string GetTask(string id) // TODO: higher-level than JObject
        {
            return GetApi($"tasks/:{id}");
        }*/
    }
}