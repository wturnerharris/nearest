using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Graphics;

using Nearest.Models;

namespace Nearest.Droid
{
	[Activity(
		Label = "Detail",
		ScreenOrientation = ScreenOrientation.Portrait
	)]
	public class Detail : Activity, View.IOnTouchListener
	{
		public bool isFullscreen;
		Train train;
		List<Train> trains;
		public List<View> detailLabels;
		public List<View> timeLabels;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			var nearestTrainColor = Intent.GetIntExtra("nearestTrainColor", -1);
			var nearestTrainTextColor = Intent.GetIntExtra("nearestTrainTextColor", -1);

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
			ButtonClose.Click += (sender, e) => Finish();

			LinearLayout detailView = FindViewById<LinearLayout>(Resource.Id.DetailInfo);
			detailView.SetOnTouchListener(this);
			detailLabels = MainActivity.GetViewsByTag(detailView, "detail");
			timeLabels = MainActivity.GetViewsByTag(detailView, "time");
			List<View> points = MainActivity.GetViewsByTag(detailView, "point");

			var nearestTrain = Intent.GetStringExtra("nearestTrain");
			if (nearestTrain != null)
			{
				train = Newtonsoft.Json.JsonConvert.DeserializeObject<Train>(nearestTrain);
				if (detailLabels.Count > 0 && train != null)
				{
					TextView DetailRouteId = FindViewById<TextView>(Resource.Id.DetailRouteId);
					DetailRouteId.Text = train.route_id;
					int t = 0;
					foreach (TextView detailLabel in detailLabels)
					{
						detailLabel.SetTextColor(new Color(nearestTrainTextColor));
						switch (t)
						{
							case 0:
								detailLabel.Text = "TO: " + train.trip_headsign;
								break;
							case 1:
								detailLabel.Text = train.stop_name;
								break;
							case 2:
								detailLabel.Text = train.TimeString;
								break;
						}
						t++;
					}
				}
			}

			var fartherTrains = Intent.GetStringExtra("fartherTrains");
			if (fartherTrains != null)
			{
				trains = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Train>>(fartherTrains);
				int i = 0;
				int ce = 0;
				if (timeLabels.Count > 0 && trains != null && trains.Count > 0)
				{
					foreach (TextView timeLabel in timeLabels)
					{
						timeLabel.Text = "";
						if (i < trains.Count)
						{
							Train fartherTrain = trains[i];
							timeLabel.Visibility = ViewStates.Visible;
							timeLabel.Text = fartherTrain.TimeString;
							points[i + ce].Visibility = ViewStates.Visible;
							points[i + ce + 1].Visibility = ViewStates.Visible;
						}
						else
						{
							timeLabel.Visibility = ViewStates.Invisible;
							points[i + ce].Visibility = ViewStates.Invisible;
							points[i + ce + 1].Visibility = ViewStates.Invisible;
						}
						i++;
						ce++;
					}
				}
			}
		}

		/// <summary>
		/// Raises the touch event.
		/// </summary>
		/// <param name="v">View.</param>
		/// <param name="e">MotionEvent.</param>
		public bool OnTouch(View v, MotionEvent e)
		{
			switch (e.Action)
			{
				case MotionEventActions.Move:
					//still moving
					break;
				case MotionEventActions.Down:
					//finger down
					SetTimeLabels("arrival_time");
					break;
				case MotionEventActions.Up:
					//finger up
					SetTimeLabels("TimeString");
					break;
			}

			return true;
		}

		void SetTimeLabels(string property)
		{
			int i = 0;
			(detailLabels[2] as TextView).Text = (string)GetProperty(train, property);
			foreach (TextView timeLabel in timeLabels)
			{
				if (trains.Equals(null))
					continue;
				timeLabel.Text = (string)GetProperty(trains[i], property);
				i++;
			}
		}

		object GetProperty(object obj, string prop)
		{
			return obj?.GetType().GetProperty(prop).GetValue(obj);
		}
	}
}