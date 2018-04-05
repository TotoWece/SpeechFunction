using SpeechFunction.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace SpeechFunction
{
	public partial class App : Application
	{
        public static ISpeechHelper SpeechHelper => DependencyService.Get<ISpeechHelper>();

        public App ()
		{
			InitializeComponent();
			MainPage = new SpeechView();
		}

		protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}
}
