namespace I.Do.Everything
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using I.Do.Brief;
    using I.Do.Speech;

    class Program
    {
        private static Speech speech = new Speech();
        private static Dictionary<string, LinkedList<Word>> speechCommands = new Dictionary<string, LinkedList<Word>>();

        private static void AddDomainSpecificCommands(Machine machine)
        {
            machine.Context.Storage.Add("port", 11411); // default port number
            machine.Context.AddWord("port", "Set port number on which to listen for HTTP commands (`port 11411`, `port <int>`)", c => { c.Storage.Remove("port"); c.Storage.Add("port", c.Pop()); return c; });
            machine.Context.AddWord("say", "Speak given string", c => { speech.Say(c.Pop()); return c; });
            machine.Context.AddWord("phrase", "Add phrase to speech recognition grammar; bind to Brief expression (`phrase 'hello [say \"hi there\"])", c => { speechCommands.Add(c.Pop(), c.Pop()); return c; });
            machine.Context.AddWord("reco", "Start speech recognition, after having added `phrase` bindings (`reco`)", c => { speech.SetGrammar(Speech.Choices(speechCommands.Select(kv => Speech.Phrase(kv.Key, Brief.Print(kv.Value.Reverse()))).ToArray())); return c; });
        }

        public static void Main(string[] args)
        {
            var machine = new Machine();
            AddDomainSpecificCommands(machine);

            speech.Recognized += (_, e) =>
            {
                Console.WriteLine($"Heard: '{e.Text}' (confidence={e.Confidence})");
                if (e.Confidence > 0.1) machine.Execute((string)e.Semantics.Value);
            };

            machine.Execute(string.Join(" ", args)); // process any command line
            Machine.ReadEvalPrintLoop("i-do", machine);
        }
    }
}
