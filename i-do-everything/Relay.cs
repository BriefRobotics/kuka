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

        public async Task<string> QueueTask(string program, string queue = "api", int priority = 5, string args = null)
        {
            var client = new HttpClient() { BaseAddress = rocUri };
            client.DefaultRequestHeaders.Add("X-API-TOKEN", apiToken);
            var content = new FormUrlEncodedContent(new KeyValuePair<string, string>[] {
                new KeyValuePair<string, string>("program", program),
                new KeyValuePair<string, string>("args", args),
            });
            var response = await client.PostAsync($"{apiBase}/tasks", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public string QueueDelivery(string args, string queue = "api", int priority = 5)
        {
            // for args, see http://docs.savioke.com/api-dev/#api-Tasks-NewDelivery
            // TODO: higher-level than JObject
            var result = QueueTask("Delivery", queue, priority, args);
            result.Wait();
            return result.Result;
        }

        public string QueueGoto(string place, string message = "Hello!", string queue = "api", int priority = 5)
        {
            var result = QueueTask("Goto", queue, priority, $"{{\"location\":\"{place}\",\"timeout\":0}}");
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
            var result = GetApiAsync("tasks");
            result.Wait();
            return result.Result;
        }

        public string GetAllPlaces() // TODO: higher-level than JObject
        {
            return GetApi("places");
        }

        public string GetPlaces(string id) // TODO: higher-level than JObject
        {
            return GetApi($"places/:{id}");
        }

        public string GetAllRobots() // TODO: higher-level than JObject
        {
            return GetApi("robots");
        }

        public string GetRobot(string id) // TODO: higher-level than JObject
        {
            return GetApi($"robots/:{id}");
        }

        public string GetAllTasks() // TODO: higher-level than JObject
        {
            return GetApi("tasks");
        }

        public string GetTask(string id) // TODO: higher-level than JObject
        {
            return GetApi($"tasks/:{id}");
        }
    }
}