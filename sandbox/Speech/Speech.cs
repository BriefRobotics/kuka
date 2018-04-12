using System;
using System.Speech.Synthesis;
using System.Speech.Recognition;

namespace Speech
{
    public class Speech
    {
        private readonly SpeechSynthesizer synth = new SpeechSynthesizer();
        private readonly SpeechRecognitionEngine reco = new SpeechRecognitionEngine();

        public event EventHandler<RecognitionResult> Recognized;

        public Speech(GrammarBuilder grammar)
        {
            synth.SelectVoiceByHints(VoiceGender.Female);
            reco.SetInputToDefaultAudioDevice();
            reco.LoadGrammar(new Grammar(grammar));
            reco.RecognizeAsync(RecognizeMode.Multiple);
            reco.SpeechRecognized += (_, e) => Recognized?.Invoke(this, e.Result);
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
            reco.RecognizeAsyncStop(); // poor man's echo cancelation
            synth.SpeakAsync(say);
            reco.RecognizeAsync(RecognizeMode.Multiple);
        }
    }
}
