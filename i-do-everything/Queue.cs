using System;
using System.Threading;
using Amazon;
using Amazon.SQS;

namespace I.Do.Queue
{
    public class Queue
    {
        public readonly string uri;
        public readonly string key;
        public readonly string secret;
        public readonly RegionEndpoint region;
        public readonly Action<string> callback;

        public Queue(string uri, string region, string key, string secret, Action<string> callback)
        {
            this.uri = uri;
            this.key = key;
            this.secret = secret;
            this.region = RegionEndpoint.GetBySystemName(region);
            this.callback = callback;
        }

        public void Start()
        {
            (new Thread(new ThreadStart(Process)) { IsBackground = true }).Start();
        }

        public void Process()
        {
            var client = new AmazonSQSClient(key, secret, region);
            var draining = true; // ignore messages from before startup
            while (true)
            {
                try
                {
                    var resp = client.ReceiveMessage(uri);
                    if (resp.Messages.Count == 0)
                    {
                        draining = false;
                    }
                    else
                    {
                        foreach (var m in resp.Messages)
                        {
                            if (!draining) // ignore messages sent while app closed
                            {
                                try
                                {
                                    callback(m.Body);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"ERROR: {ex.Message}");
                                }
                                finally
                                {
                                    try
                                    {
                                        client.DeleteMessage(uri, m.ReceiptHandle);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"ERROR: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                }
            }
        }
    }
}
