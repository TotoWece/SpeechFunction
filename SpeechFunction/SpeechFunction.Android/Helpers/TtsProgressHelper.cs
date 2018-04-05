using System;
using System.Threading.Tasks;
using Android.Speech.Tts;

namespace SpeechFunction.Droid.Helpers
{
    public class TtsProgressHelper : UtteranceProgressListener
    {
        readonly TaskCompletionSource<object> completionSource;

        public TtsProgressHelper(TaskCompletionSource<object> tcs) => completionSource = tcs;

        public override void OnDone(string utteranceId) =>
            completionSource?.TrySetResult(null);

        public override void OnError(string utteranceId) =>
            completionSource?.TrySetException(new ArgumentException("Error with TTS engine on progress listener"));

        public override void OnStart(string utteranceId)
        {
        }
    }
}