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

        public async void QueueTask(string program, string queue = "api", int priority = 5, string args = null)
        {
            var client = new HttpClient() { BaseAddress = rocUri };
            client.DefaultRequestHeaders.Add("X-API-TOKEN", apiToken);
            var content = new FormUrlEncodedContent(new KeyValuePair<string, string>[] {
                new KeyValuePair<string, string>("program", program),
                new KeyValuePair<string, string>("queueName", queue),
                new KeyValuePair<string, string>("priority", $"{priority}"),
                new KeyValuePair<string, string>("args", args),
            });
            // curl 'program=Delivery&args={"dropoffs": [{"location": "Room 3"}]}' http://example.org/api/v2/tasks
            var response = await client.PostAsync($"{apiBase}/tasks", content);
            response.EnsureSuccessStatusCode();
        }

        public void QueueDelivery(string args, string queue = "api", int priority = 5)
        {
            // for args, see http://docs.savioke.com/api-dev/#api-Tasks-NewDelivery
            // TODO: higher-level than JObject
            QueueTask("Delivery", queue, priority, args);
        }

        public void QueueGoto(string place, string message, string queue = "api", int priority = 5)
        {
            // var args = JObject.Parse($"{{ \"location\":\"{place}\", message:'{message}', language:'en', timeout:'240' }}");
            QueueTask("Goto", queue, priority, $"{{ \"location\":\"{place}\"}}");
        }

        public async void CancelTask(string id)
        {
            var client = new HttpClient() { BaseAddress = rocUri };
            client.DefaultRequestHeaders.Add("X-API-TOKEN", apiToken);
            var response = await client.DeleteAsync($"{apiBase}/tasks/:{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<JObject> GetApi(string api) // TODO: higher-level than JObject
        {
            var client = new HttpClient() { BaseAddress = rocUri };
            client.DefaultRequestHeaders.Add("X-API-TOKEN", apiToken);
            var response = await client.GetAsync($"{apiBase}/{api}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return JObject.Parse(result);
        }

        public async Task<JObject> GetAllPlaces() // TODO: higher-level than JObject
        {
            return await GetApi("places");
        }

        public async Task<JObject> GetPlaces(string id) // TODO: higher-level than JObject
        {
            return await GetApi($"places/:{id}");
        }

        public async Task<JObject> GetAllRobots() // TODO: higher-level than JObject
        {
            return await GetApi("robots");
        }

        public async Task<JObject> GetRobot(string id) // TODO: higher-level than JObject
        {
            return await GetApi($"robots/:{id}");
        }

        public async Task<JObject> GetAllTasks() // TODO: higher-level than JObject
        {
            return await GetApi("tasks");
        }

        public async Task<JObject> GetTask(string id) // TODO: higher-level than JObject
        {
            return await GetApi($"tasks/:{id}");
        }
    }
}