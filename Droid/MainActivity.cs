using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Android.OS;
using Android.Text;
using Android.Graphics;
using Android.Locations;
using Android.Net;

//using Android.Database.Sqlite;

using Nearest.ViewModels;
using Nearest.Models;
using Javax.Microedition.Khronos.Opengles;

namespace Nearest.Droid
{
	[Activity (
		Label = "Nearest", 
		MainLauncher = true, 
		Icon = "@drawable/icon", 
		ScreenOrientation = ScreenOrientation.Portrait
	)]
	public class MainActivity : Activity, ILocationListener
	{
		protected LocationManager locationMgr;
		public TrainListViewModel trainLVM;
		public Location location;

		/**
		 * Public UI Elements
		 * 
		 */
		public RelativeLayout mainLayout;
		public LinearLayout northLayout, southLayout;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			RequestWindowFeature (WindowFeatures.NoTitle);
			Window.SetFlags (WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			// Set Typeface and Styles
			Typeface HnBd = Typeface.CreateFromAsset (Assets, "fonts/HelveticaNeueLTCom-Bd.ttf");
			Typeface HnLt = Typeface.CreateFromAsset (Assets, "fonts/HelveticaNeueLTCom-Lt.ttf");
			Typeface HnMd = Typeface.CreateFromAsset (Assets, "fonts/HelveticaNeueLTCom-Roman.ttf");
			TypefaceStyle tfs = TypefaceStyle.Normal;

			// Main app title and tagline
			TextView title = FindViewById<TextView> (Resource.Id.TextViewTitle);
			TextView tagLn = FindViewById<TextView> (Resource.Id.TextViewTagline);
			TextView timeS = FindViewById<TextView> (Resource.Id.TextTimeSouth);
			TextView destS = FindViewById<TextView> (Resource.Id.TextDestSouth);
			TextView timeN = FindViewById<TextView> (Resource.Id.TextTimeNorth);
			TextView destN = FindViewById<TextView> (Resource.Id.TextDestNorth);

			mainLayout = FindViewById<RelativeLayout> (Resource.Id.mainLayout);
			int childCount = mainLayout.ChildCount;

			for (var i = 0; i < childCount; i++) {
				switch (i) {
				case 2:
					southLayout = (LinearLayout)mainLayout.GetChildAt (i);
					break;
				case 3:
					northLayout = (LinearLayout)mainLayout.GetChildAt (i);
					break;
				default:
					break;
				}
			}

			//Animation hyperspaceJump = AnimationUtils.LoadAnimation (this, Resource.Animation.tada);

			title.SetTypeface (HnBd, tfs);
			tagLn.SetTypeface (HnLt, tfs);
			timeS.SetTypeface (HnMd, tfs);
			destS.SetTypeface (HnLt, tfs);
			timeN.SetTypeface (HnMd, tfs);
			destN.SetTypeface (HnLt, tfs);

			/**
			 * Define UI actions
			 * 
			 */
			for (var i = 0; i < 2; i++) {
				var direction = i == 0 ? southLayout : northLayout;
				Button button = (Button)direction.FindViewWithTag (tag: "button");
				button.FindViewWithTag (tag: "button").Click += delegate {
					var count = 0;
					//StartActivity (typeof(Detail));

					Intent intent = new Intent (this, typeof(Detail));
					ActivityOptions options = ActivityOptions.MakeScaleUpAnimation (button, 0,
						                          0, 60, 60);
					StartActivity (intent, options.ToBundle ());

					button.Text = string.Format ("{0}", count++);
					button.SetBackgroundResource (
						count % 2 == 0 ? Resource.Drawable.orange : Resource.Drawable.purple
					);
				};
			}

			// check connections
			handleConnections ();
		}

		/**
		 * Resume Activity
		 * 
		 */
		protected override void OnResume ()
		{
			base.OnResume ();
			handleConnections ();
		}

		/**
		 * Pause Activity
		 * 
		 */
		protected override void OnPause ()
		{
			base.OnPause ();
			locationMgr.RemoveUpdates (this);
		}

		public void handleConnections ()
		{
			location = getLocation ();
			if (location != null) {
				if (isConnected ()) {
					try {
						getTrainModels ();
					} catch (Exception ex) {
						System.Console.WriteLine ("DBG: Exception (handleConnections):" + ex.Message.ToString ());
					}
				} else {
					// Create ui-based handler for no internet
					System.Console.WriteLine ("DBG: No internet is available");
				}
			} else {
				// Create ui-based handler for unknown location
				System.Console.WriteLine ("DBG: No location is available");
			}
		}

		/**
		 * Detect internet access
		 * 
		 */
		public bool isConnected ()
		{
			ConnectivityManager connectivityManager = (ConnectivityManager)GetSystemService (ConnectivityService);
			NetworkInfo activeConnection = connectivityManager.ActiveNetworkInfo;
			bool isConnected = (activeConnection != null) && activeConnection.IsConnected;
			return isConnected;
		}

		/**
		 * Detect Location
		 * 
		 */
		public Location getLocation ()
		{
			locationMgr = GetSystemService (Context.LocationService) as LocationManager;
			Criteria locationCriteria = new Criteria ();
			String bestLocationProvider, locationProviderPassive;
			locationCriteria.Accuracy = Accuracy.Fine;
			locationCriteria.PowerRequirement = Power.NoRequirement;
			locationProviderPassive = LocationManager.PassiveProvider;
			bestLocationProvider = locationMgr.GetBestProvider (locationCriteria, true);

			if (locationProviderPassive != bestLocationProvider) {
				System.Console.WriteLine ("DBG: " + locationProviderPassive + " != " + bestLocationProvider);

				if (locationMgr.IsProviderEnabled (bestLocationProvider)) {
					System.Console.WriteLine ("DBG: Requesting Updates for " + bestLocationProvider);
					locationMgr.RequestLocationUpdates (bestLocationProvider, 1000, 1, this);
					var coords = locationMgr.GetLastKnownLocation (bestLocationProvider);

					if (coords == null) {
						if (locationMgr.IsProviderEnabled (locationProviderPassive)) {
							System.Console.WriteLine ("DBG: Requesting Updates for " + locationProviderPassive);
							locationMgr.RequestLocationUpdates (locationProviderPassive, 1000, 1, this);
							return locationMgr.GetLastKnownLocation (locationProviderPassive);
						} else {
							System.Console.WriteLine ("DBG: " + locationProviderPassive +
							" is not available. Does the device have location services enabled?"
							);
						}
					} else {
						return coords;
					}
				}
			}

			return null;
		}

		/**
		 * Set next train info
		 * 
		 */
		public void setNextTrains ()
		{
			if (trainLVM.IsBusy) {
				System.Console.WriteLine ("DBG: Async busy");
			} else {
				// Loop throuh south and north view groups 
				for (var i = 0; i < 2; i++) {
					var path = i == 0 ? southLayout : northLayout;
					var button = (Button)path.FindViewWithTag (tag: "button");
					var time = (TextView)path.FindViewWithTag (tag: "time");

					if (trainLVM.stopList.Count > 0) {
						foreach (var direction in trainLVM.stopList) {
							var nearestDirection = trainLVM.stopList [i] [0].next_train;
							if (nearestDirection != null) {
								button.Text = nearestDirection.route_id;
								button.SetBackgroundResource (getTrainColor (nearestDirection.route_id));
								time.Text = Train.time (nearestDirection.ts);
							} else {
								button.Text = "!";
								button.SetBackgroundResource (getTrainColor ("0"));
								time.Text = "Problem";
							}
						}
					}
				}
			}
		}

		/**
		 * Get Train Color
		 * 
		 */
		public int getTrainColor (String StopId)
		{
			int resourceDrawable;
			switch (StopId) {
			case "1":
			case "2":
			case "3":
				resourceDrawable = Resource.Drawable.red;
				break;
			case "A":
			case "C":
			case "E":
				resourceDrawable = Resource.Drawable.blue;
				break;
			case "N":
			case "Q":
			case "R":
				resourceDrawable = Resource.Drawable.yellow;
				break;
			case "4":
			case "5":
			case "6":
			case "G": /** TODO: This is alternate green */
				resourceDrawable = Resource.Drawable.green;
				break;
			case "B":
			case "D":
			case "F":
			case "M":
				resourceDrawable = Resource.Drawable.orange;
				break;
			case "7":
				resourceDrawable = Resource.Drawable.purple;
				break;
			case "J":
			case "Z":
				resourceDrawable = Resource.Drawable.brown;
				break;
			case "L":
			default: 
				resourceDrawable = Resource.Drawable.gray;
				break;
			}
			return resourceDrawable;
		}

		/**
		 * Get List of Trains for VM
		 * 
		 */
		async void getTrainPaths ()
		{
			try {
				await trainLVM.GetTrainsAsync ();
			} catch (Exception ex) {
				System.Console.WriteLine ("DBG: Error: No Trains Found");
				System.Console.WriteLine ("DBG: Exception: " + ex.Message.ToString ());
			} finally {
				if (!this.trainLVM.IsBusy) {
					System.Console.WriteLine ("DBG: Async: getTrainPaths");
					setNextTrains ();
					locationMgr.RemoveUpdates (this);
				}
			}
		}

		/**
		 * Location Updates
		 * 
		 */
		public void OnProviderEnabled (string provider)
		{
			System.Console.WriteLine ("DBG: OnProviderEnabled");
		}

		public void OnProviderDisabled (string provider)
		{
			System.Console.WriteLine ("DBG: OnProviderDisabled");
		}

		public void OnStatusChanged (string provider, Availability status, Bundle extras)
		{
			System.Console.WriteLine ("DBG: OnStatusChanged");
		}

		public void OnLocationChanged (Location locationChanged)
		{
			System.Console.WriteLine ("DBG: OnLocationChanged");
			location = locationChanged;
			getTrainModels ();
		}

		public void getTrainModels ()
		{
			System.Console.WriteLine ("DBG: getTrainModels");
			System.Console.WriteLine (
				"DBG: Lat: " + location.Latitude +
				"\nDBG: Long: " + location.Longitude
			);
			trainLVM = new TrainListViewModel (location.Latitude, location.Longitude);
			getTrainPaths ();
		}
	}
}


