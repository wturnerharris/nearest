using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;

using Nearest.Models;
using Android.Graphics;

namespace Nearest.Droid
{
	[Activity(
		Label = "Detail",
		ScreenOrientation = ScreenOrientation.Portrait
	)]
	public class Detail : Activity
	{
		public bool isFullscreen;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			var nearestTrainColor = Intent.GetIntExtra("nearestTrainColor", -1);
			if (nearestTrainColor > 0)
			{
				Window window = Window;
				View decorView = Window.DecorView;

				var uiOptions = (int)decorView.SystemUiVisibility;
				var newUiOptions = uiOptions;

				newUiOptions |= (int)SystemUiFlags.LowProfile;
				newUiOptions |= (int)SystemUiFlags.HideNavigation;
				newUiOptions |= (int)SystemUiFlags.Immersive;

				if (isFullscreen)
				{
					//window.AddFlags (WindowManagerFlags.Fullscreen);
					//window.SetFlags (WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
					newUiOptions |= (int)SystemUiFlags.Fullscreen;
				}

				window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
				window.SetStatusBarColor(Color.ParseColor(Resources.GetString(nearestTrainColor)));
				decorView.SystemUiVisibility = (StatusBarVisibility)newUiOptions;
				decorView.SetBackgroundResource(nearestTrainColor);
			}

			SetContentView(Resource.Layout.Detail);

			Button ButtonClose = FindViewById<Button>(Resource.Id.DetailClose);
			ButtonClose.Click += delegate (object sender, EventArgs e)
			{
				Finish();
			};

			LinearLayout detailView = FindViewById<LinearLayout>(Resource.Id.DetailInfo);
			List<View> detailLabels = MainActivity.GetViewsByTag(detailView, "detail");
			List<View> timeLabels = MainActivity.GetViewsByTag(detailView, "time");

			var nearestTrain = Intent.GetStringExtra("nearestTrain");
			if (nearestTrain != null)
			{
				Train train = Newtonsoft.Json.JsonConvert.DeserializeObject<Train>(nearestTrain);
				if (detailLabels.Count > 0 && train != null)
				{
					TextView DetailRouteId = FindViewById<TextView>(Resource.Id.DetailRouteId);
					DetailRouteId.Text = train.route_id;
					int t = 0;
					foreach (TextView detailLabel in detailLabels)
					{
						switch (t)
						{
							case 0:
								detailLabel.Text = "TO: " + train.trip_headsign;
								break;
							case 1:
								detailLabel.Text = train.stop_name;
								break;
							case 2:
								detailLabel.Text = train.GetTimeInMinutes();
								break;
						}
						t++;
					}
				}
			}
			else
			{
				Console.WriteLine("Detail: Json error!!");
			}

			var fartherTrains = Intent.GetStringExtra("fartherTrains");
			if (fartherTrains != null)
			{
				Console.WriteLine("Detail: has Farther Trains");
				List<Train> trains = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Train>>(fartherTrains);
				int i = 0;
				if (timeLabels.Count > 0 && trains != null && trains.Count > 0)
				{
					foreach (TextView timeLabel in timeLabels)
					{
						timeLabel.Text = "";
						if (i < trains.Count)
						{
							Train fartherTrain = trains[i];
							var TimeInMinutes = fartherTrain.GetTimeInMinutes();
							timeLabel.Text = TimeInMinutes;
						}
						i++;
					}
				}
				else
				{
					Console.WriteLine("Detail: no Farther Trains");
				}
			}
		}
	}
}