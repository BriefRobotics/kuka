using System;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Web;

namespace Faces
{
    class Program
    {
        const string KEY = "05e23a6b8a494ac3ba2a3d49053ccf48";
        const string ENDPOINT = "https://westcentralus.api.cognitive.microsoft.com";

        static async Task DeletePersonGroup(string id)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", KEY);
            var uri = $"{ENDPOINT}/face/v1.0/persongroups/{id}";
            var response = await client.DeleteAsync(uri);
            if (response.ReasonPhrase == "Not Found") return; // this is fine
            response.EnsureSuccessStatusCode();
        }

        static async Task CreatePersonGroup(string id, string name)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", KEY);
            var uri = $"{ENDPOINT}/face/v1.0/persongroups/{id}";
            var data = Encoding.UTF8.GetBytes($"{{ name: '{name}' }}");
            using (var content = new ByteArrayContent(data))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await client.PutAsync(uri, content);
                response.EnsureSuccessStatusCode();
            }
        }

        static async Task CreatePerson(string id, string name)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", KEY);
            var uri = $"{ENDPOINT}/face/v1.0/persongroups/{id}";
            var data = Encoding.UTF8.GetBytes($"{{ name: '{name}' }}");
            using (var content = new ByteArrayContent(data))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await client.PutAsync(uri, content);
                response.EnsureSuccessStatusCode();
            }
        }

        static async void RepopulatePersonGroup(string id, string name)
        {
            await DeletePersonGroup(id);
            Console.WriteLine($"Deleted person group (id='{id}')");
            await CreatePersonGroup(id, name);
            Console.WriteLine($"Recreated person group (id='{id}', name='{name}')");

            // TODO: foreach person
            // Delete/Create person
            // Add faces
            // Train group
            // Test face Identify against untrained set

            // See: https://westus.dev.cognitive.microsoft.com/docs/services/563879b61984550e40cbbe8d/operations/563879b61984550f3039523c
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Face Enrollment Tool");

            var personGroupId = "Kuka";
            var personGroupName = "KukaGroup";

            RepopulatePersonGroup(personGroupId, personGroupName);
            Console.WriteLine("Waiting");

            Console.ReadLine();
        }
    }
}
