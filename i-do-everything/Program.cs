namespace I.Do.Everything
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using I.Do.Brief;
    using I.Do.Speech;
    using I.Do.Windows;
    using I.Do.Relay;
    using I.Do.Queue;

    class Program
    {
        private static readonly Config config = new Config();

        #region speech

        private static readonly Speech speech = new Speech();
        private static readonly Dictionary<string, LinkedList<Word>> speechCommands = new Dictionary<string, LinkedList<Word>>();

        private static void InitSpeech(Machine machine)
        {
            speech.Recognized += (_, e) =>
            {
                Console.WriteLine($"Heard: '{e.Text}' (confidence={e.Confidence})");
                try
                {
                    if (e.Confidence > 0.1) machine.Execute((string)e.Semantics.Value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Speech command error: {ex.Message}");
                }
            };
        }

        #endregion speech

        #region relay

        private static Relay relay;

        private static void InitRelay()
        {
            relay = new Relay(config["relayRocUri"], config["relayApiToken"]); // set in config.b
        }

        #endregion relay

        #region queue

        private static Queue queue;

        private static void InitQueue(Machine machine)
        {
            var uri = config.MachineSpecific("amazonSqsUri");
            var region = config["amazonSqsRegion"];
            var key = config["amazonSqsKey"];
            var secret = config["amazonSqsSecret"];
            queue = new Queue(uri, region, key, secret, m =>
            {
                try
                {
                    machine.Execute(m);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Message queue command error: {ex.Message}");
                }
            });
            queue.Start();
        }

        #endregion queue

        private static void AddDomainSpecificCommands(Machine machine)
        {
            machine.Context.AddWord10("say", "Speak given string", s => speech.Say(s));
            machine.Context.AddWord20("phrase", "Add phrase to speech recognition grammar; bind to Brief expression (`phrase 'hello [say \"hi there\"])", (p, b) => speechCommands.Add(p, b));
            machine.Context.AddWord00("reco", "Start speech recognition, after having added `phrase` bindings (`reco`)", () => speech.SetGrammar(Speech.Choices(speechCommands.Select(kv => Speech.Phrase(kv.Key, Brief.Print(kv.Value.Reverse()))).ToArray())));
            machine.Context.AddWord20("window", "Show window in foreground by process name; optionally maximized (`window \"Skype\" true)", (n, m) => Windows.ShowWindow(n, m));
            machine.Context.AddWord10("key", "Send key to forground app (`key '^{q})", k => Windows.SendKey(k));
            machine.Context.AddWord20("config", "Set configuration value (`config 'port 80`)", (k, v) => config[k] = v);
        }

        public static void Main(string[] args)
        {
            var machine = new Machine();
            AddDomainSpecificCommands(machine);

            machine.Execute("load 'init.b"); // initial script
            machine.Execute(string.Join(" ", args)); // process any command line

            InitSpeech(machine);
            InitRelay();
            InitQueue(machine);

            Machine.ReadEvalPrintLoop("i-do", machine);
        }
    }
}
