using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SpeechFunction.Models;

namespace SpeechFunction.Helpers
{
    public interface ISpeechHelper : IDisposable
    {
        Task Speak(string text, 
            CrossLanguage? crossLocale = null, 
            float? pitch = null, 
            float? speakRate = null, 
            float? volume = null, 
            CancellationToken cancelToken = default(CancellationToken));

        Task<IEnumerable<CrossLanguage>> GetInstalledLanguages();

        int MaxSpeechInputLength { get; }
    }
}
