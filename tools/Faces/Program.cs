using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Faces
{
    class Program
    {
        const string KEY = "05e23a6b8a494ac3ba2a3d49053ccf48";
        const string ENDPOINT = "https://westcentralus.api.cognitive.microsoft.com";

        private static int THROTTLE = 3000;

        private static HttpClient GetClient()
        {
            Thread.Sleep(THROTTLE); // throttle
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", KEY);
            return client;
        }

        private static async Task<string> SendData(byte[] data, string type, string uri, Func<string, HttpContent, Task<HttpResponseMessage>> sendFn)
        {
            Thread.Sleep(THROTTLE); // throttle
            using (var content = new ByteArrayContent(data))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue(type);
                var response = await sendFn(uri, content);
                var result = await response.Content.ReadAsStringAsync();
                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    throw new Exception(result, ex);
                }
                return result;
            }
        }

        private static async Task<string> SendJson(string json, string uri, Func<string, HttpContent, Task<HttpResponseMessage>> sendFn)
        {
            return await SendData(Encoding.UTF8.GetBytes(json), "application/json", uri, sendFn);
        }

        private static async Task<string> PostJson(string json, string uri)
        {
            return await SendJson(json, uri, GetClient().PostAsync);
        }

        static async Task DeletePersonGroup(string groupId)
        {
            Thread.Sleep(THROTTLE); // throttle
            var client = GetClient();
            var response = await client.DeleteAsync($"{ENDPOINT}/face/v1.0/persongroups/{groupId}");
            if (response.ReasonPhrase == "Not Found") return; // this is fine
            response.EnsureSuccessStatusCode();
        }

        static async Task CreatePersonGroup(string groupId, string groupName)
        {
            await SendJson($"{{ name: '{groupName}' }}", $"{ENDPOINT}/face/v1.0/persongroups/{groupId}", GetClient().PutAsync);
        }

        static async Task<string> CreatePerson(string groupId, string personName)
        {
            var response = await PostJson($"{{ name: '{personName}' }}", $"{ENDPOINT}/face/v1.0/persongroups/{groupId}/persons");
            return JObject.Parse(response)["personId"].ToString();
        }

        static async Task<string> AddPersonFace(string groupId, string personId, byte[] faceImage)
        {
            var uri = $"{ENDPOINT}/face/v1.0/persongroups/{groupId}/persons/{personId}/persistedFaces";
            return await SendData(faceImage, "application/octet-stream", uri, GetClient().PostAsync);
        }

        static async Task<string> Train(string groupId)
        {
            return await PostJson(string.Empty, $"{ENDPOINT}/face/v1.0/persongroups/{groupId}/train");
        }

        static async Task<bool> TrainingInProgress(string groupId)
        {
            Thread.Sleep(THROTTLE); // throttle
            var response = await GetClient().GetAsync($"{ENDPOINT}/face/v1.0/persongroups/{groupId}/training");
            var result = await response.Content.ReadAsStringAsync();
            var status = JObject.Parse(result)["status"].ToString();
            if (status == "failed") throw new Exception($"Training failed: {result}");
            return status != "succeeded";
        }

        static async Task<IEnumerable<string>> DetectFaces(byte[] image)
        {
            var uri = $"{ENDPOINT}/face/v1.0/detect?returnFaceId=true&returnFaceLandmarks=true&returnFaceAttributes=age,gender,smile,facialHair,glasses,emotion,hair,makeup,occlusion,accessories";
            var result = await SendData(image, "application/octet-stream", uri, GetClient().PostAsync);
            var detected = JObject.Parse($"{{ detected: {result} }}");
            var faces = detected["detected"];
            return faces.Select(f => f["faceId"].ToString());
        }

        static async Task<string> IdentifyFaces(IEnumerable<string> faceIds, string groupId)
        {
            var faces = JsonConvert.SerializeObject(faceIds);
            var json = $"{{ personGroupId: '{groupId}', maxNumOfCandidatesReturned: 1, confidenceThreshold: 0, faceIds: {faces} }}";
            return await PostJson(json, $"{ENDPOINT}/face/v1.0/identify");
        }

        static async Task RepopulatePersonGroup(string groupId, string groupName, string peoplePath)
        {
            // See: https://westus.dev.cognitive.microsoft.com/docs/services/563879b61984550e40cbbe8d/operations/563879b61984550f3039523c
            // create people and upload face images
            await DeletePersonGroup(groupId);
            Console.WriteLine($"Deleted person group (groupId='{groupId}')");
            await CreatePersonGroup(groupId, groupName);
            Console.WriteLine($"Recreated person group (groupId='{groupId}', groupName='{groupName}')");
            var people = new JObject();
            foreach (var d in Directory.GetDirectories(peoplePath))
            {
                var personName = Path.GetFileName(d);
                var personId = await CreatePerson(groupId, personName);
                people.Add(personId, personName);
                Console.WriteLine($"  Added person: '{personName}' (id={personId})");
                foreach (var p in Directory.GetFiles(d))
                {
                    var faceImage = File.ReadAllBytes(p);
                    var finished = false;
                    while (!finished)
                    {
                        try
                        {
                            await AddPersonFace(groupId, personId, faceImage);
                            finished = true;
                            Console.WriteLine($"    Added face: '{Path.GetFileName(p)}'");
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("RateLimitExceeded"))
                            {
                                Console.WriteLine("    RATE LIMIT EXCEEDED (throttling 20 seconds)");
                                Thread.Sleep(THROTTLE * 7); // throttle (rate limit)
                            }
                            else
                            {
                                finished = true;
                            }
                        }
                    }
                }
            }
            File.WriteAllText($"{peoplePath}/people.json", people.ToString());
            // */

            // train face reco
            await Train(groupId);
            Console.WriteLine("Training...");
            while (true)
            {
                try
                {
                    while (await TrainingInProgress(groupId))
                    {
                        Console.WriteLine("Still training...");
                        Thread.Sleep(10000);
                    }
                    break;
                }
                catch
                {
                    Console.WriteLine("RATE LIMIT EXCEEDED (throttling 10 seconds)");
                }
            }
        }
        static async Task TestFaceReco(string groupId, string peoplePath)
        {
            THROTTLE = 0; // disable throttling
            var people = JObject.Parse(File.ReadAllText($"{peoplePath}/../people/people.json"));
            foreach (var p in Directory.GetFiles(peoplePath))
            {
                var image = File.ReadAllBytes(p);
                var finished = false;
                while (!finished)
                {
                    try
                    {
                        var detected = await DetectFaces(image);
                        Console.WriteLine($"  Detected faces: {detected.Count()} ('{Path.GetFileName(p)}')");
                        var ids = await IdentifyFaces(detected, groupId);
                        foreach (var f in JObject.Parse($"{{ ids: {ids} }}")["ids"])
                        {
                            Console.WriteLine($"    Face ({f["faceId"]})");
                            foreach (var c in f["candidates"])
                            {
                                var i = c["personId"].ToString();
                                Console.WriteLine($"      Canditate: {people[i]} (confidence={c["confidence"]})");
                            }
                        }
                        finished = true;
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("RateLimitExceeded"))
                        {
                            Console.WriteLine("    RATE LIMIT EXCEEDED (throttling 20 seconds)");
                            Thread.Sleep(THROTTLE * 7); // throttle (rate limit)
                        }
                        else
                        {
                            finished = true;
                        }
                    }
                }
            }
        }

        static async void Process(string personGroupId, string personGroupName)
        {
            await RepopulatePersonGroup(personGroupId, personGroupName, "../../../people");
            await TestFaceReco(personGroupId, "../../../test");
            Console.WriteLine("DONE!");
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Face Enrollment Tool");
            Process("kuka", "KukaGroup");
            Console.ReadLine();
        }
    }
}
