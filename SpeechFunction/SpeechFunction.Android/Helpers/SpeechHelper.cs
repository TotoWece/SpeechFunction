﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Java.Util;

using Android.Speech.Tts;
using Android.App;

using SpeechFunction.Helpers;
using SpeechFunction.Models;

[assembly: Xamarin.Forms.Dependency(typeof(SpeechFunction.Droid.Helpers.SpeechHelper))]
namespace SpeechFunction.Droid.Helpers
{
    public class SpeechHelper : Java.Lang.Object, ISpeechHelper, Android.Speech.Tts.TextToSpeech.IOnInitListener, IDisposable
    {
        const int DefaultMaxSpeechLength = 4000;
        readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        Android.Speech.Tts.TextToSpeech textToSpeech;
        string text;
        CrossLanguage? language;
        float pitch, speakRate;
        float? volume;
        bool initialized;
        int count;

        TaskCompletionSource<bool> initTcs;
        Task Init()
        {
            if (initialized)
                return Task.FromResult(true);

            initTcs = new TaskCompletionSource<bool>();

            Console.WriteLine("Current version: " + (int)global::Android.OS.Build.VERSION.SdkInt);
            Android.Util.Log.Info("CrossTTS", "Current version: " + (int)global::Android.OS.Build.VERSION.SdkInt);
            textToSpeech = new Android.Speech.Tts.TextToSpeech(Application.Context, this);

            return initTcs.Task;
        }

        public void OnInit(OperationResult status)
        {
            if (status.Equals(OperationResult.Success))
            {
                initialized = true;
                initTcs.TrySetResult(true);
            }
            else
            {
                initTcs.TrySetException(new ArgumentException("Failed to initialize TTS engine"));
            }
        }

        public async Task Speak(string text, CrossLanguage? crossLocale = null, float? pitch = null, float? speakRate = null, float? volume = null, CancellationToken cancelToken = default(CancellationToken))
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text), "Text can not be null");

            if (text.Length >= MaxSpeechInputLength)
                throw new ArgumentException(nameof(text), "Text length is over the maximum speech input length.");

            try
            {
                await semaphore.WaitAsync(cancelToken);
                this.text = text;
                language = crossLocale;
                this.pitch = pitch == null ? 1.0f : pitch.Value;
                this.speakRate = speakRate == null ? 1.0f : speakRate.Value;
                this.volume = volume;


                // TODO: need to wait lock so not to break people using queuing mechanism
                await Init();


                await Speak(cancelToken);

            }
            finally
            {
                if (semaphore.CurrentCount == 0)
                    semaphore.Release();
            }
        }


        private void SetDefaultLanguage() => SetDefaultLanguageNonLollipop();

        private void SetDefaultLanguageNonLollipop()
        {
            if (textToSpeech == null)
                return;
            //disable warning because we are checking ahead of time.
#pragma warning disable 0618
            var sdk = (int)global::Android.OS.Build.VERSION.SdkInt;
            if (sdk >= 18)
            {
                try
                {

#if __ANDROID_18__
                    if (textToSpeech.DefaultLanguage == null && textToSpeech.Language != null)
                        textToSpeech.SetLanguage(textToSpeech.Language);
                    else if (textToSpeech.DefaultLanguage != null)
                        textToSpeech.SetLanguage(textToSpeech.DefaultLanguage);
#endif
                }
                catch
                {
                    if (textToSpeech.Language != null)
                        textToSpeech.SetLanguage(textToSpeech.Language);
                }
            }
            else
            {
                if (textToSpeech.Language != null)
                    textToSpeech.SetLanguage(textToSpeech.Language);
            }
#pragma warning restore 0618
        }

        async Task Speak(CancellationToken cancelToken)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (language.HasValue && !string.IsNullOrWhiteSpace(language.Value.Language))
            {
                Locale locale = null;
                if (!string.IsNullOrWhiteSpace(language.Value.Country))
                    locale = new Locale(language.Value.Language, language.Value.Country);
                else
                    locale = new Locale(language.Value.Language);

                var result = textToSpeech.IsLanguageAvailable(locale);
                if (result == LanguageAvailableResult.CountryAvailable)
                {
                    textToSpeech.SetLanguage(locale);
                }
                else
                {
                    Console.WriteLine("Locale: " + locale + " was not valid, setting to default.");
                    SetDefaultLanguage();
                }
            }
            else
            {
                SetDefaultLanguage();
            }

            var tcs = new TaskCompletionSource<object>();

            textToSpeech.SetPitch(pitch);
            textToSpeech.SetSpeechRate(speakRate);

            textToSpeech.SetOnUtteranceProgressListener(new TtsProgressHelper(tcs));

#pragma warning disable CS0618 // Type or member is obsolete
            count++;

            var map = new Dictionary<string, string>
            {
                [Android.Speech.Tts.TextToSpeech.Engine.KeyParamUtteranceId] = count.ToString()
            };

            if (volume.HasValue)
            {
                map.Add(Android.Speech.Tts.TextToSpeech.Engine.KeyParamVolume, volume.ToString());
            }
            textToSpeech.Speak(text, QueueMode.Flush, map);
#pragma warning restore CS0618 // Type or member is obsolete

            void OnCancel()
            {
                textToSpeech.Stop();
                tcs.TrySetCanceled();
            }

            using (cancelToken.Register(OnCancel))
            {
                await tcs.Task;
            }
        }

        public async Task<IEnumerable<CrossLanguage>> GetInstalledLanguages()
        {
            await Init();
            if (textToSpeech != null && initialized)
            {
                var version = (int)global::Android.OS.Build.VERSION.SdkInt;
                var isLollipop = version >= 21;
                if (isLollipop)
                {
                    try
                    {
                        //in a different method as it can crash on older target/compile for some reason
                        return GetInstalledLanguagesLollipop();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Something went horribly wrong, defaulting to old implementation to get languages: " + ex);
                    }
                }

                var languages = new List<CrossLanguage>();
                var allLocales = Locale.GetAvailableLocales();
                foreach (var locale in allLocales)
                {

                    try
                    {
                        var result = textToSpeech.IsLanguageAvailable(locale);

                        if (result == LanguageAvailableResult.CountryAvailable)
                        {
                            languages.Add(new CrossLanguage { Country = locale.Country, Language = locale.Language, DisplayName = locale.DisplayName });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error checking language; " + locale + " " + ex);
                    }
                }

                return languages.GroupBy(c => c.ToString())
                      .Select(g => g.First());
            }
            else
            {
                return Locale.GetAvailableLocales()
                  .Where(a => !string.IsNullOrWhiteSpace(a.Language) && !string.IsNullOrWhiteSpace(a.Country))
                  .Select(a => new CrossLanguage { Country = a.Country, Language = a.Language, DisplayName = a.DisplayName })
                  .GroupBy(c => c.ToString())
                  .Select(g => g.First());
            }
        }

        private IEnumerable<CrossLanguage> GetInstalledLanguagesLollipop()
        {
            var sdk = (int)global::Android.OS.Build.VERSION.SdkInt;
            if (sdk < 21)
                return new List<CrossLanguage>();

#if __ANDROID_21__
            return textToSpeech.AvailableLanguages
              .Select(a => new CrossLanguage { Country = a.Country, Language = a.Language, DisplayName = a.DisplayName });
#endif
        }

        public int MaxSpeechInputLength =>
            (int)Android.OS.Build.VERSION.SdkInt < 18 ? DefaultMaxSpeechLength : Android.Speech.Tts.TextToSpeech.MaxSpeechInputLength;

        void IDisposable.Dispose()
        {
            textToSpeech?.Stop();
            textToSpeech?.Dispose();
            textToSpeech = null;
            initialized = false;
        }
    }
}