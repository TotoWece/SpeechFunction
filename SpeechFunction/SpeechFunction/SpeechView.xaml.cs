using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using SpeechFunction.Models;
using Plugin.TextToSpeech;
using Plugin.TextToSpeech.Abstractions;

namespace SpeechFunction
{
    public partial class SpeechView : ContentPage
    {
        IEnumerable<CrossLanguage> locales;
        static CrossLanguage? locale = null;
        //IEnumerable<CrossLocale> locales;
        //static CrossLocale? locale = null;

        public SpeechView()
        {
            InitializeComponent();
            GetLanguage();           
        }

        async void GetLanguage()
        {
            locales = await App.SpeechHelper.GetInstalledLanguages();
            //locales = await CrossTextToSpeech.Current.GetInstalledLanguages();
            
            var langAvailable = locales.Select(a => a.DisplayName.ToString()).ToArray();
            var languageList = langAvailable.OrderBy(t => t).Distinct().ToList();
            languagePicker.ItemsSource = languageList;
        }

        async void SpeakButton_Clicked(object sender, EventArgs e)
        {
            speakButton.IsEnabled = false;
            var text = textEntry.Text;

            if (languagePicker.SelectedItem != null)
                locale = locales.FirstOrDefault(l => l.DisplayName.ToString() == languagePicker.SelectedItem.ToString());

            if (!string.IsNullOrWhiteSpace(text))
            {
                await App.SpeechHelper.Speak(text,
                    pitch: (float)pitchSlider.Value,
                    speakRate: (float)rateSlider.Value,
                    volume: (float)volumeSlider.Value,
                    crossLocale: locale);
                //await CrossTextToSpeech.Current.Speak(text,
                //    pitch: (float)pitchSlider.Value,
                //    speakRate: (float)rateSlider.Value,
                //    volume: (float)volumeSlider.Value,
                //    crossLocale: locale);
            }
            speakButton.IsEnabled = true;
        }
    }
}