using System;
using System.Globalization;
using System.Speech.Synthesis;
using System.Speech.Recognition;

namespace I.Do.Speech
{
    public class Speech
    {
        private readonly SpeechSynthesizer synth = new SpeechSynthesizer();
        private readonly SpeechRecognitionEngine reco = new SpeechRecognitionEngine();

        public event EventHandler<RecognitionResult> Recognized;

        public void SetGrammar(GrammarBuilder grammar)
        {
            try
            {
                var d = new GrammarBuilder();
                d.AppendDictation();
                var g = new Grammar(new Choices(grammar, d));
                reco.SetInputToDefaultAudioDevice();
                reco.UnloadAllGrammars();
                reco.LoadGrammar(new Grammar(grammar));
                reco.RecognizeAsync(RecognizeMode.Multiple);
                reco.SpeechRecognized += (_, e) => Recognized?.Invoke(this, e.Result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }

        public Speech()
        {
            try
            {
                synth.SelectVoiceByHints(VoiceGender.Female);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }

        public Speech(GrammarBuilder grammar)
            : this()
        {
            SetGrammar(grammar);
        }

        public static GrammarBuilder Phrase(string phrase, string semantics, bool optional = false)
        {
            var gb = optional ? new GrammarBuilder(phrase, 0, 1) : new GrammarBuilder(phrase);
            gb.Append(new SemanticResultValue(semantics));
            return gb;
        }

        public static GrammarBuilder Choices(params GrammarBuilder[] choices)
        {
            return new GrammarBuilder(new Choices(choices));
        }

        public static GrammarBuilder Sequence(params GrammarBuilder[] seq)
        {
            var gb = new GrammarBuilder();
            foreach (var s in seq) gb.Append(s);
            return gb;
        }

        public static GrammarBuilder Dictation()
        {
            var dict = new GrammarBuilder();
            dict.AppendDictation();
            return dict;
        }

        public void Say(string say)
        {
            try
            {
                reco.RecognizeAsyncStop(); // poor man's echo cancelation
                synth.Speak(say);
                if (reco.Grammars.Count > 0) reco.RecognizeAsync(RecognizeMode.Multiple);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
    }
}
