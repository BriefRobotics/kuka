﻿namespace I.Do.Everything
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
                    if (e.Confidence > 0.1 && e.Semantics.Value != null) machine.Execute((string)e.Semantics.Value);
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

        private static void Goto(string place, string message)
        {
            Console.WriteLine($"Going to {place} (message={message})");
            // relay.QueueGoto(place, message);
        }

        #endregion relay

        #region queue

        private static Queue queue;

        private static void InitQueue(Machine machine)
        {
            var role = config["role"];
            var sqs = config["amazonSqsBaseUri"];
            var uri = $"{sqs}/{role}";
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
                    Console.WriteLine($"Message queue command error: {ex.Message} ({m})");
                }
            });
            queue.Start();
            Console.WriteLine($"Role: {role} ({uri})");
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
            var lastSeen = new Dictionary<string, DateTime>();
            var repeatTime = double.Parse(config["faceRepeatSeconds"]);
            var watcher = new FileSystemWatcher(directory) { IncludeSubdirectories = true, EnableRaisingEvents = true };
            var wait = new EventWaitHandle(false, EventResetMode.AutoReset);
            string imagePath = null;
            watcher.Created += (_, e) =>
            {
                imagePath = e.FullPath;
                wait.Set();
                if (debug) Console.WriteLine($"Image: {imagePath}");
            };

            (new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    wait.WaitOne();
                    if (debug) Console.WriteLine($"Face reco {imagePath}");
                    byte[] image = null;
                    var attempt = 3;
                    do
                    {
                        try
                        {
                            image = File.ReadAllBytes(imagePath);
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
                            if (debug) Console.WriteLine($"Recognized {f}");
                            var last = lastSeen.ContainsKey(f) ? lastSeen[f] : DateTime.MinValue;
                            if ((DateTime.Now - last).TotalSeconds > repeatTime)
                            {
                                var key = $"faceGreeting.{f}";
                                var cmd = config.ContainsKey(key) ? config[key] : config["faceGreeting.default"];
                                machine.Execute(String.Format(cmd, f));
                            }
                            else
                            {
                                if (debug) Console.WriteLine("  Already seen recently...");
                            }
                            lastSeen[f] = DateTime.Now;
                        }
                    }
                }
            })) { IsBackground = true }).Start();
        }

        #endregion face

        private static void AddDomainSpecificCommands(Machine machine)
        {
            machine.Context.AddWord10("say", "Speak given string", s => speech.Say(s));
            machine.Context.AddWord20("phrase", "Add phrase to speech recognition grammar; bind to Brief expression (`phrase 'hello [say \"hi there\"]`)", (p, b) => speechCommands.Add(p, b));
            machine.Context.AddWord00("speechreco", "Start speech recognition, after having added `phrase` bindings (`reco`)", () => speech.SetGrammar(Speech.Choices(speechCommands.Select(kv => Speech.Phrase(kv.Key, Brief.Print(kv.Value))).ToArray())));
            machine.Context.AddWord20("window", "Show window in foreground by process name; optionally maximized (`window \"Skype\" true`)", (n, m) => Windows.ShowWindow(n, m));
            machine.Context.AddWord10("key", "Send key to forground app (`key '^{q}`)", k => Windows.SendKey(k));
            machine.Context.AddWord20("config", "Set configuration value (`config 'port 80`)", (k, v) => config[k] = v is IEnumerable<Word> ? Brief.Print(v) : v.ToString());
            machine.Context.AddWord00("faces-train", "Create and train faces in Azure Cognitive Services (`faces-train \"myfaces/\")`", () => faces.TrainFaces());
            machine.Context.AddWord10("faces-reco", "Recognize faces in given image file (`faces-reco \"test.jpg\"`)", f => faces.RecoFaces((string)f, true));
            machine.Context.AddWord20("faces-watch", "Begin watching given directory and children for face images [debug mode optional] (`faces-watch \"c:/test\"` true)", (dir, debug) => WatchFaces(dir, debug, machine));
            machine.Context.AddWord20("goto", "Send relay to given place with given message (`goto \"booth\" \"Hello\"`)", (p, m) => Goto(p, m));
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
