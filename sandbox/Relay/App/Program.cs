using System;
using Savioke.Relay;

namespace RelayApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello Savioke Relay World!");

            var relay = new Relay("https://kuka.savioke.com/api/v2/tasks", "token");
            relay.QueueGoto("foo", "Hi there!");
        }
    }
}
