using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;

namespace Speech
{
    class Program
    {
        static void Main(string[] args)
        {
            const double CONFIDENCE_THRESHOLD = 0.8;

            var grammar = Speech.Choices(
                Speech.Phrase("Bring me some coffee", "coffee"),
                Speech.Phrase("Bring coffee", "coffee"),
                Speech.Phrase("Bring the coffee machine over", "coffee"),
                Speech.Phrase("Bring the coffee machine over here", "coffee"),
                Speech.Phrase("I need some coffee!", "coffee"),
                Speech.Phrase("I'm thirsty", "coffee"),
                Speech.Phrase("Shoot a t-shirt", "shirt"),
                Speech.Phrase("Shoot a shirt", "shirt"),
                Speech.Phrase("Fire a t-shirt", "shirt"),
                Speech.Phrase("Fire a shirt", "shirt"),
                Speech.Phrase("Blast a shirt", "shirt"),
                Speech.Phrase("Blast off a shirt", "shirt"),
                Speech.Phrase("Ready. Aim. Fire!", "shirt"),
                Speech.Dictation());
            var speech = new Speech(grammar);
            speech.Recognized += (_, r) =>
            {
                if (r.Semantics.Value == null)
                {
                    Console.WriteLine("I didn't understand:");
                    foreach (var h in r.Alternates)
                    {
                        Console.WriteLine($"    '{h.Text}'");
                    }
                }
                else
                {
                    Console.WriteLine($"Command: {r.Text} (confidence={r.Semantics.Confidence} semantics={r.Semantics.Value})");
                    if (r.Semantics.Confidence > CONFIDENCE_THRESHOLD)
                    {
                        switch (r.Semantics.Value)
                        {
                            case "coffee":
                                Console.WriteLine("    Coming with coffee!");
                                speech.Say("Coming with coffee!");
                                break;
                            case "shirt":
                                Console.WriteLine("    Blasting a shirt off!");
                                speech.Say("Blasting a shirt off!");
                                break;
                        }
                    }
                    else
                    {
                        Console.WriteLine("    Say again?");
                        speech.Say("Say again?");
                    }
                }
            };

            Console.WriteLine("Hello! I await your command...");
            speech.Say("Hello! I await your command...");

            Console.ReadLine();
        }

        private static void Speech_Recognized(object sender, RecognitionResult e)
        {
            throw new NotImplementedException();
        }
    }
}

// nice example of composable grammar DSL: https://github.com/AshleyF/VimSpeak/blob/master/Main.fs