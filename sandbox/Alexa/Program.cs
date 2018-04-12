using System;
using Amazon;
using Amazon.SQS;

namespace Alexa
{
    class Program
    {
        private const string KEY = "AKIAJYREIDOQAAD24UMQ";
        private const string SECRET = "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";
        private const string URL = "https://sqs.us-east-1.amazonaws.com/660181231855/KukaBot";
        private static readonly RegionEndpoint REGION = RegionEndpoint.USEast1;

        static async void ProcessQueue()
        {
            var client = new AmazonSQSClient(KEY, SECRET, REGION);
            var draining = true;
            while (true)
            {
                var resp = await client.ReceiveMessageAsync(URL);
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
                            Console.WriteLine($"Message: {m.Body}");
                        }
                        await client.DeleteMessageAsync(URL, m.ReceiptHandle);
                        Console.WriteLine($"Deleted: {m.ReceiptHandle}");
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello Alexa World!");
            ProcessQueue();
            Console.ReadLine();
        }
    }
}
