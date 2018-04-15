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

namespace I.Do.Faces
{
    public class Faces
    {
        public readonly string key;
        public readonly string endpoint;
        public readonly string peopleDirectory; // faces and test images
        public readonly double confidenceThreshold;
        public static int THROTTLE = 3000;

        public Faces(string key, string endpoint, string peopleDirectory, double confidenceThreshold)
        {
            this.key = key;
            this.endpoint = endpoint;
            this.peopleDirectory = peopleDirectory;
            this.confidenceThreshold = confidenceThreshold;
        }

        private HttpClient GetClient()
        {
            Thread.Sleep(THROTTLE); // throttle
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
            return client;
        }

        private async Task<string> SendData(byte[] data, string type, string uri, Func<string, HttpContent, Task<HttpResponseMessage>> sendFn)
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

        private async Task<string> SendJson(string json, string uri, Func<string, HttpContent, Task<HttpResponseMessage>> sendFn)
        {
            return await SendData(Encoding.UTF8.GetBytes(json), "application/json", uri, sendFn);
        }

        private async Task<string> PostJson(string json, string uri)
        {
            return await SendJson(json, uri, GetClient().PostAsync);
        }

        private async Task DeletePersonGroup(string groupId)
        {
            Thread.Sleep(THROTTLE); // throttle
            var client = GetClient();
            var response = await client.DeleteAsync($"{endpoint}/face/v1.0/persongroups/{groupId}");
            if (response.ReasonPhrase == "Not Found") return; // this is fine
            response.EnsureSuccessStatusCode();
        }

        private async Task CreatePersonGroup(string groupId, string groupName)
        {
            await SendJson($"{{ name: '{groupName}' }}", $"{endpoint}/face/v1.0/persongroups/{groupId}", GetClient().PutAsync);
        }

        private async Task<string> CreatePerson(string groupId, string personName)
        {
            var response = await PostJson($"{{ name: '{personName}' }}", $"{endpoint}/face/v1.0/persongroups/{groupId}/persons");
            return JObject.Parse(response)["personId"].ToString();
        }

        private async Task<string> AddPersonFace(string groupId, string personId, byte[] faceImage)
        {
            var uri = $"{endpoint}/face/v1.0/persongroups/{groupId}/persons/{personId}/persistedFaces";
            return await SendData(faceImage, "application/octet-stream", uri, GetClient().PostAsync);
        }

        private async Task<string> Train(string groupId)
        {
            return await PostJson(string.Empty, $"{endpoint}/face/v1.0/persongroups/{groupId}/train");
        }

        private async Task<bool> TrainingInProgress(string groupId)
        {
            Thread.Sleep(THROTTLE); // throttle
            var response = await GetClient().GetAsync($"{endpoint}/face/v1.0/persongroups/{groupId}/training");
            var result = await response.Content.ReadAsStringAsync();
            var status = JObject.Parse(result)["status"].ToString();
            if (status == "failed") throw new Exception($"Training failed: {result}");
            return status != "succeeded";
        }

        private async Task<IEnumerable<string>> DetectFaces(byte[] image)
        {
            var uri = $"{endpoint}/face/v1.0/detect?returnFaceId=true&returnFaceLandmarks=true&returnFaceAttributes=age,gender,smile,facialHair,glasses,emotion,hair,makeup,occlusion,accessories";
            var result = await SendData(image, "application/octet-stream", uri, GetClient().PostAsync);
            var detected = JObject.Parse($"{{ detected: {result} }}");
            var faces = detected["detected"];
            return faces.Select(f => f["faceId"].ToString());
        }

        private async Task<string> IdentifyFaces(IEnumerable<string> faceIds, string groupId)
        {
            var faces = JsonConvert.SerializeObject(faceIds);
            var json = $"{{ personGroupId: '{groupId}', maxNumOfCandidatesReturned: 1, confidenceThreshold: 0, faceIds: {faces} }}";
            return await PostJson(json, $"{endpoint}/face/v1.0/identify");
        }

        private async Task RepopulatePersonGroup(string groupId, string groupName, string peoplePath)
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
            File.WriteAllText($"{peopleDirectory}/people.json", people.ToString());
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

        private async Task TestFaceReco(string groupId, string peoplePath)
        {
            THROTTLE = 0; // disable throttling
            var people = JObject.Parse(File.ReadAllText($"{peopleDirectory}/people.json"));
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

        private const string personGroupId = "kuka";
        private const string personGroupName = "KukaGroup";

        public async void TrainFaces()
        {
            await RepopulatePersonGroup(personGroupId, personGroupName, $"{peopleDirectory}/people");
            await TestFaceReco(personGroupId, $"{peopleDirectory}/test");
            Console.WriteLine("DONE!");
        }

        private async Task<IEnumerable<string>> RecoFacesAsync(byte[] image, bool debug)
        {
            THROTTLE = 0; // disable throttling
            var people = JObject.Parse(File.ReadAllText($"{peopleDirectory}/people.json"));
            while (true)
            {
                try
                {
                    var detected = await DetectFaces(image);
                    if (debug) Console.WriteLine($"  Detected faces: {detected.Count()}");
                    var ids = await IdentifyFaces(detected, personGroupId);
                    var faces = new List<string>();
                    foreach (var f in JObject.Parse($"{{ ids: {ids} }}")["ids"])
                    {
                        if (debug) Console.WriteLine($"    Face ({f["faceId"]})");
                        foreach (var c in f["candidates"])
                        {
                            var i = c["personId"].ToString();
                            if (debug) Console.WriteLine($"      Candidate: {people[i]} (confidence={c["confidence"]})");
                            if (double.Parse(c["confidence"].ToString()) > confidenceThreshold)
                            {
                                faces.Add(people[i].ToString());
                            }
                        }
                    }
                    return faces;
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
                        break;
                    }
                }
            }
            throw new Exception("Should not reach here");
        }
        public IEnumerable<string> RecoFaces(byte[] image, bool debug)
        {
            var task = RecoFacesAsync(image, debug);
            task.Wait();
            return task.Result;
        }

        public IEnumerable<string> RecoFaces(string file, bool debug)
        {
            return RecoFaces(File.ReadAllBytes(file), debug);
        }
    }
}
