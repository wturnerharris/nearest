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

			LinearLayout detailView = FindViewById<LinearLayout> (Resource.Id.DetailInfo);
			List<View> detailLabels = MainActivity.GetViewsByTag (detailView, "detail");
			List<View> timeLabels = MainActivity.GetViewsByTag (detailView, "time");

			var nearestTrain = Intent.GetStringExtra ("nearestTrain");
			if (nearestTrain != null) {
				var train = Newtonsoft.Json.JsonConvert.DeserializeObject<Train> (nearestTrain);
				if (detailLabels.Count > 0 && train != null) {
					TextView DetailRouteId = FindViewById<TextView> (Resource.Id.DetailRouteId);
					DetailRouteId.Text = train.route_id;
					var t = 0;
					foreach (TextView detailLabel in detailLabels) {
						switch (t) {
						case 0:
							detailLabel.Text = train.trip_headsign;
							break;
						case 1:
							detailLabel.Text = train.stop_name;
							break;
						case 2:
							detailLabel.Text = train.GetTimeInMinutes ();
							break;
						}
						t++;
					}
				}
			} else {
				System.Console.WriteLine ("Detail: Json error!!");
			}

			var fartherTrains = Intent.GetStringExtra ("fartherTrains");
			if (fartherTrains != null) {
				System.Console.WriteLine ("has Farther Trains");
				var trains = Newtonsoft.Json.JsonConvert.DeserializeObject <List<Train>> (fartherTrains);
				var i = 0;
				if (timeLabels.Count > 0 && trains != null && trains.Count > 0) {
					foreach (TextView timeLabel in timeLabels) {
						if (i < trains.Count) {
							var fartherTrain = trains [i];
							var TimeInMinutes = fartherTrain.GetTimeInMinutes ();
							timeLabel.Text = TimeInMinutes;
						} else {
							timeLabel.Text = "";
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

