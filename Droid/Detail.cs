using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Nearest.ViewModels;

using Nearest.Models;
using Android.Graphics;

namespace Nearest.Droid
{
	[Activity (
		Label = "Detail",
		ScreenOrientation = ScreenOrientation.Portrait
	)]			
	public class Detail : Activity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			var nearestTrainColor = Intent.GetIntExtra ("nearestTrainColor", -1);
			if (nearestTrainColor > 0) {
				Window window = this.Window;
				View decorView = Window.DecorView;

				var uiOptions = (int)decorView.SystemUiVisibility;
				var newUiOptions = (int)uiOptions;

				newUiOptions |= (int)SystemUiFlags.LowProfile;
				newUiOptions |= (int)SystemUiFlags.HideNavigation;
				newUiOptions |= (int)SystemUiFlags.Immersive;

				var isFullscreen = false;
				if (isFullscreen) {
					//window.AddFlags (WindowManagerFlags.Fullscreen);
					//window.SetFlags (WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
					newUiOptions |= (int)SystemUiFlags.Fullscreen;
				}

				window.AddFlags (WindowManagerFlags.DrawsSystemBarBackgrounds);
				window.SetStatusBarColor (Color.ParseColor (Resources.GetString (nearestTrainColor)));
				decorView.SystemUiVisibility = (StatusBarVisibility)newUiOptions;
				decorView.SetBackgroundResource (nearestTrainColor);
			}

			SetContentView (Resource.Layout.Detail);

			Button ButtonClose = FindViewById<Button> (Resource.Id.DetailClose);
			ButtonClose.Click += delegate(object sender, EventArgs e) {
				this.Finish ();
			};

			var nearestTrain = Intent.GetStringExtra ("nearestTrain");
			if (nearestTrain != null) {
				var train = Newtonsoft.Json.JsonConvert.DeserializeObject<Train> (nearestTrain);

				TextView DetailRouteId = FindViewById<TextView> (Resource.Id.DetailRouteId);
				DetailRouteId.Text = train.route_id;

				TextView DetailHeadsign = FindViewById<TextView> (Resource.Id.DetailHeadsign);
				DetailHeadsign.Text = train.trip_headsign;

				TextView DetailStopName = FindViewById<TextView> (Resource.Id.DetailStopName);
				DetailStopName.Text = train.stop_name;

				TextView DetailStopTime1 = FindViewById<TextView> (Resource.Id.DetailStopTime1);
				DetailStopTime1.Text = train.GetTimeInMinutes ();
			} else {
				System.Console.WriteLine ("Detail: Json error!!");
			}

			var fartherTrains = Intent.GetStringExtra ("fartherTrains");
			if (fartherTrains != null) {
				System.Console.WriteLine ("has Farther Trains");
				var trains = Newtonsoft.Json.JsonConvert.DeserializeObject <List<Train>> (fartherTrains);
				TextView DetailStopTime2 = FindViewById<TextView> (Resource.Id.DetailStopTime2);
				TextView DetailStopTime3 = FindViewById<TextView> (Resource.Id.DetailStopTime3);
				TextView DetailStopTime4 = FindViewById<TextView> (Resource.Id.DetailStopTime4);
				var i = 2;
				if (trains.Count > 0) {
					foreach (var fartherTrain in trains) {
						var TimeInMinutes = fartherTrain.GetTimeInMinutes ();
						switch (i) {
						case 2:
							DetailStopTime2.Text = TimeInMinutes;
							break;
						case 3:
							DetailStopTime3.Text = TimeInMinutes;
							break;
						case 4:
							DetailStopTime4.Text = TimeInMinutes;
							break;
						}
						i++;
					}
				}
			} else {
				System.Console.WriteLine ("has not Farther Trains");
			}

		}
	}
}

