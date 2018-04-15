namespace I.Do.Everything
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Collections.Generic;
    using I.Do.Brief;
    using I.Do.Speech;
    using I.Do.Windows;
    using I.Do.Relay;
    using I.Do.Queue;
    using I.Do.Faces;

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
            var uri = config["amazonSqsUri"];
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

        #region face

        private static Faces faces;

        private static void InitFace(Machine machine)
        {
            faces = new Faces(
                config["azureCognitiveKey"],
                config["azureCognitiveUri"],
                config["faceDirectory"],
                double.Parse(config["faceConfidenceThreshold"]));
        }

        private static void WatchFaces(string directory, bool debug, Machine machine)
        {
            var watcher = new FileSystemWatcher(directory) { IncludeSubdirectories = true, EnableRaisingEvents = true };
            var busy = false;
            watcher.Created += (_, e) =>
            {
                if (!busy)
                {
                    busy = true;
                    Console.WriteLine($"Face reco {e.FullPath}");
                    byte[] image = null;
                    var attempt = 3;
                    do
                    {
                        try
                        {
                            image = File.ReadAllBytes(e.FullPath);
                            break;
                        }
                        catch (Exception)
                        {
                            Thread.Sleep(100); // file lock
                        }
                    } while (attempt-- > 0);

                    if (image != null)
                    {
                        foreach (var f in faces.RecoFaces(image, debug))
                        {
                            var key = $"faceGreeting.{f}";
                            var cmd = config.ContainsKey(key) ? config[key] : config["faceGreeting.default"];
                            machine.Execute(String.Format(cmd, f));
                        }
                    }
                    busy = false;
                }
                else
                {
                    Console.WriteLine("Face reco busy...");
                }
            };
        }

        #endregion face

        private static void AddDomainSpecificCommands(Machine machine)
        {
            machine.Context.AddWord10("say", "Speak given string", s => speech.Say(s));
            machine.Context.AddWord20("phrase", "Add phrase to speech recognition grammar; bind to Brief expression (`phrase 'hello [say \"hi there\"]`)", (p, b) => speechCommands.Add(p, b));
            machine.Context.AddWord00("speechreco", "Start speech recognition, after having added `phrase` bindings (`reco`)", () => speech.SetGrammar(Speech.Choices(speechCommands.Select(kv => Speech.Phrase(kv.Key, Brief.Print(kv.Value.Reverse()))).ToArray())));
            machine.Context.AddWord20("window", "Show window in foreground by process name; optionally maximized (`window \"Skype\" true`)", (n, m) => Windows.ShowWindow(n, m));
            machine.Context.AddWord10("key", "Send key to forground app (`key '^{q}`)", k => Windows.SendKey(k));
            machine.Context.AddWord20("config", "Set configuration value (`config 'port 80`)", (k, v) => config[k] = v is IEnumerable<Word> ? Brief.Print(v) : v.ToString());
            machine.Context.AddWord00("faces-train", "Create and train faces in Azure Cognitive Services (`faces-train \"myfaces/\")`", () => faces.TrainFaces());
            machine.Context.AddWord10("faces-reco", "Recognize faces in given image file (`faces-reco \"test.jpg\"`)", f => faces.RecoFaces((string)f, true));
            machine.Context.AddWord20("faces-watch", "Begin watching given directory and children for face images [debug mode optional] (`faces-watch \"c:/test\"` true)", (dir, debug) => WatchFaces(dir, debug, machine));
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
            InitFace(machine);

            Machine.ReadEvalPrintLoop("i-do", machine);
        }
    }
}
