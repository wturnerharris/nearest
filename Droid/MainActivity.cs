using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;

using Android;
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
using Android.Support.Design;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Gms.Location;
using Android.Gms.Common.Apis;
using Android.Util;

using Nearest.ViewModels;
using Nearest.Models;

using Runnable = Java.Lang.Runnable;

namespace Nearest.Droid
{
	[Activity (
		Label = "Nearest", 
		MainLauncher = true, 
		Icon = "@drawable/icon", 
		ScreenOrientation = ScreenOrientation.Portrait
	)]
	public class MainActivity : AppCompatActivity, 
	GoogleApiClient.IConnectionCallbacks, 
	GoogleApiClient.IOnConnectionFailedListener, 
	Android.Gms.Location.ILocationListener
	{
		public TrainListViewModel trainLVM;
		public Location CurrentLocation;
		GoogleApiClient googleApiClient;

		/**
		 * Public UI Elements
		 * 
		 */
		public RelativeLayout mainLayout;
		public LinearLayout northLayout, southLayout;
		public bool isFullscreen = false;

		readonly string[] PermissionsLocation = {
			Manifest.Permission.AccessCoarseLocation,
			Manifest.Permission.AccessFineLocation
		};

		const int RequestLocationId = 0;

		protected override void OnCreate (Bundle savedInstanceState)
		{
			RequestWindowFeature (WindowFeatures.NoTitle);
			base.OnCreate (savedInstanceState);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			// Set Typeface and Styles
			TypefaceStyle tfs = TypefaceStyle.Normal;
			Typeface HnBd = Typeface.CreateFromAsset (Assets, "fonts/HelveticaNeueLTCom-Bd.ttf");
			Typeface HnLt = Typeface.CreateFromAsset (Assets, "fonts/HelveticaNeueLTCom-Lt.ttf");
			Typeface HnMd = Typeface.CreateFromAsset (Assets, "fonts/HelveticaNeueLTCom-Roman.ttf");

			mainLayout = FindViewById<RelativeLayout> (Resource.Id.mainLayout);
			int childCount = mainLayout.ChildCount;

			// Main app title and tagline
			for (var i = 0; i < childCount; i++) {
				switch (i) {
				case 0:
					TextView title = (TextView)mainLayout.GetChildAt (i);
					title.SetTypeface (HnBd, tfs);
					break;
				case 1:
					TextView tagLine = (TextView)mainLayout.GetChildAt (i);
					tagLine.SetTypeface (HnLt, tfs);
					break;
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

			/**
			 * Define UI actions
			 * 
			 */
			mainLayout.Click += delegate(object sender, EventArgs e) {
				if (isFullscreen) {
					Window.ClearFlags (WindowManagerFlags.Fullscreen);
					isFullscreen = false;
				} else {
					Window.SetFlags (WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
					isFullscreen = true;
				}
				ShowAlert (isFullscreen ? "Fullscreen enabled" : "Fullscreen disabled");
			};
			for (var i = 0; i < 2; i++) {
				var direction = i == 0 ? southLayout : northLayout;
				var dirString = i == 0 ? "South" : "North";

				TextView times = (TextView)direction.FindViewWithTag (tag: "time");
				TextView label = (TextView)direction.FindViewWithTag (tag: "label");
				times.SetTypeface (HnMd, tfs);
				label.SetTypeface (HnLt, tfs);

				Button button = (Button)direction.FindViewWithTag (tag: "button");
				button.FindViewWithTag (tag: "button").Click += delegate {
					//StartActivity (typeof(Detail));
					//Animation hyperspaceJump = AnimationUtils.LoadAnimation (this, Resource.Animation.tada);

					if (trainLVM != null) {
						ActivityOptions options = ActivityOptions.MakeScaleUpAnimation (button, 0, 0, 60, 60);
						Intent pendingIntent = new Intent (this, typeof(Detail));
						var toJson = Newtonsoft.Json.JsonConvert.SerializeObject (trainLVM.stopList);
						pendingIntent.PutExtra ("trainLVM", toJson);
						pendingIntent.PutExtra ("direction", dirString);
						StartActivity (pendingIntent, options.ToBundle ());
					} else {
						button.Text = "!";
						button.SetBackgroundResource (
							GetTrainColor ("")
						);
					}
				};
			}

			// check connections
			//handleConnections ("onCreate");
		}

		/**
		 * Resume Activity
		 * 
		 */
		protected override void OnResume ()
		{
			base.OnResume ();
			handleConnections ("onResume");
		}

		/**
		 * Pause Activity
		 * 
		 */
		protected override void OnPause ()
		{
			base.OnPause ();
			EndLocationUpdates ();
		}

		/**
		 * Handle connections; network and location
		 * 
		 */
		public void handleConnections (String source)
		{
			if (IsConnected ()) {
				try {
					Task.Run (() => TryGetLocation ());
				} catch (Exception ex) {
					Report ("Exception (handleConnections):\n" +
					ex.Message.ToString (), 0);
				}
			} else {
				// Create ui-based handler for no internet
				Report ("No internet is available", 2);
			}
			return;
		}

		/**
		 * Detect internet access
		 * 
		 */
		public bool IsConnected ()
		{
			ConnectivityManager connectivityManager = (ConnectivityManager)GetSystemService (ConnectivityService);
			NetworkInfo activeConnection = connectivityManager.ActiveNetworkInfo;
			bool IsConnected = (activeConnection != null) && activeConnection.IsConnected;
			return IsConnected;
		}

		/**
		 * Detect Location
		 * 
		 */
		async void TryGetLocation ()
		{
			Report ("Getting location info", 0);
			if ((int)Build.VERSION.SdkInt < 23) {
				await GetLocationAsync ();
				return;
			}

			await GetLocationPermissionAsync ();
		}

		async Task GetLocationPermissionAsync ()
		{
			//Check to see if any permission in our group is available, if one, then all are
			const string permission = Manifest.Permission.AccessFineLocation;
			if (CheckSelfPermission (permission) == (int)Permission.Granted) {
				Report ("Location permission granted.", 0);
				await GetLocationAsync ();
				return;
			}

			//need to request permission
			if (ShouldShowRequestPermissionRationale (permission)) {
				Report ("Should show reason for permission.", 0);
				//Explain to the user why we need to read the contacts
				Snackbar.Make (FindViewById (Resource.Id.CoordinatorView), 
					"Location access is required to show trains nearest you.", Snackbar.LengthIndefinite)
					.SetAction ("OK", v => RequestPermissions (PermissionsLocation, RequestLocationId))
					.Show ();
				return;
			}
			//Finally request permissions with the list of permissions and Id
			RequestPermissions (PermissionsLocation, RequestLocationId);
		}

		public override async void OnRequestPermissionsResult 
		(int requestCode, string[] permissions, Permission[] grantResults)
		{
			Report ("Request Code:" + requestCode.ToString (), 0);
			switch (requestCode) {
			case RequestLocationId:
				{
					if (grantResults [0] == Permission.Granted) {
						Report ("Permission granted.", 0);
						//Permission granted
						await GetLocationAsync ();
					} else {
						//Permission Denied :(
						//Disabling location functionality
						Report ("Location permission is denied.", 0);
						Snackbar.Make (FindViewById (Resource.Id.CoordinatorView), 
							"Location permission is denied.", Snackbar.LengthIndefinite)
							.SetAction ("Try again?", v => RequestPermissions (PermissionsLocation, RequestLocationId))
							.Show ();
					}
				}
				break;
			}
		}

		public async Task GetLocationAsync ()
		{ 
			try {
				Report ("Getting location async...", 0);
				// register for location updates
				await Task.Run (() => {
					googleApiClient = new GoogleApiClient.Builder (this)
						.AddApi (Android.Gms.Location.LocationServices.API)
						.AddConnectionCallbacks (this)
						.AddOnConnectionFailedListener (this)
						.Build ();
					googleApiClient.Connect ();
				});
			} catch (Exception ex) {
				Report ("Unable to get location\nDBG Exception: " + ex.ToString (), 2);
			}
		}

		public void EndLocationUpdates ()
		{
			if (googleApiClient != null) {
				LocationServices.FusedLocationApi.RemoveLocationUpdates (googleApiClient, this);
			}
		}

		/**
		 * Set next train info
		 * 
		 */
		public void SetNextTrains ()
		{
			if (trainLVM.IsBusy) {
				Report ("Still getting trains...", 0);
			} else {
				Report ("Setting next trains...", 0);
				// Loop throuh south and north view groups 
				for (var i = 0; i < 2; i++) {
					Report ("SetNextTrain" + (i + 1).ToString (), 0);
					var path = i == 0 ? southLayout : northLayout;
					var button = (Button)path.FindViewWithTag (tag: "button");
					var time = (TextView)path.FindViewWithTag (tag: "time");

					if (trainLVM.stopList.Count > 0) {
						foreach (var direction in trainLVM.stopList) {
							var nearestDirection = trainLVM.stopList [i] [0].next_train;
							if (nearestDirection != null) {
								button.Text = nearestDirection.route_id;
								button.SetBackgroundResource (GetTrainColor (nearestDirection.route_id));
								button.SetTextColor (Color.White);
								time.Text = Train.time (nearestDirection.ts);
							} else {
								button.Text = "!";
								button.SetBackgroundResource (GetTrainColor ("0"));
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
		public int GetTrainColor (String StopId)
		{
			int resourceDrawable;
			switch (StopId) {
			case "1":
			case "2":
			case "3":
				resourceDrawable = Resource.Drawable.Red;
				break;
			case "A":
			case "C":
			case "E":
				resourceDrawable = Resource.Drawable.Blue;
				break;
			case "N":
			case "Q":
			case "R":
				resourceDrawable = Resource.Drawable.Yellow;
				break;
			case "4":
			case "5":
			case "6":
			case "G": /** TODO: This is alternate green */
				resourceDrawable = Resource.Drawable.Green;
				break;
			case "B":
			case "D":
			case "F":
			case "M":
				resourceDrawable = Resource.Drawable.Orange;
				break;
			case "7":
				resourceDrawable = Resource.Drawable.Purple;
				break;
			case "J":
			case "Z":
				resourceDrawable = Resource.Drawable.Brown;
				break;
			case "L":
			default: 
				resourceDrawable = Resource.Drawable.Gray;
				break;
			}
			return resourceDrawable;
		}

		/**
		 * Get train list view model
		 * 
		 */
		public void GetTrainModels (Location locationData)
		{
			if (locationData != null) {
				Report (
					"Lat: " + locationData.Latitude +
					"\nLong: " + locationData.Longitude,
					2
				);

				try {
					trainLVM = new TrainListViewModel (locationData.Latitude, locationData.Longitude);
					Task.Run (
						() => trainLVM.GetTrainsAsync ()
					).ContinueWith (
						task => RunOnUiThread (
							() => SetNextTrains ()
						)
					).Wait ();
				} catch (Exception ex) {
					Report ("Exception: " + ex.Message.ToString (), 0);
				} finally {
					if (!this.trainLVM.IsBusy) {
						Report ("Train schedule received", 0);
					} else {
						EndLocationUpdates ();
					}
				}
			} else {
				Report ("problem getting GetTrainModelsAsync", 2);
			}
		}

		public void Report (String msg, int verbosity)
		{
			RunOnUiThread (() => {
				var appName = GetString (Resource.String.title) ?? "Nearest";
				switch (verbosity) {
				case 0:
					Log.Debug (appName, "DBG: " + msg);
					break;
				case 1:
					Toast.MakeText (this, msg, ToastLength.Short).Show ();
					break;
				case 2:
					Report (msg, 0);
					Report (msg, 1);
					break;
				}
			});
			return;
		}

		public void ShowAlert (String str)
		{
			var alertString = str == null ? "Unknown Issue" : str;

			View coordinatorLayoutView = FindViewById<CoordinatorLayout> (Resource.Id.CoordinatorView);
			if (coordinatorLayoutView != null) {
				Snackbar.Make (coordinatorLayoutView, alertString, Snackbar.LengthShort).Show ();
			} else {
				ActivityOptions options = ActivityOptions.MakeScaleUpAnimation (mainLayout, 0, 0, 60, 60);
				Intent pendingIntent = new Intent (this, typeof(Alert));
				pendingIntent.PutExtra ("alertBoxText", alertString);
				pendingIntent.PutExtra ("parentWidth", mainLayout.Width);
				StartActivity (pendingIntent, options.ToBundle ());
			}

			return;
		}

		public void OnLocationChanged (Location NewLocation)
		{
			// Show latest location
			var l = DescribeLocation (NewLocation);
			Report ("OnLocationChanged:\n" + l, 0);
			EndLocationUpdates ();
			GetTrainModels (NewLocation);
		}

		public async void OnConnected (Bundle connectionHint)
		{           
			// Get Last known location
			var lastLocation = LocationServices.FusedLocationApi.GetLastLocation (googleApiClient);
			Report (lastLocation == null ? "NULL" : DescribeLocation (lastLocation), 0);

			await RequestLocationUpdates ();
		}

		async Task RequestLocationUpdates ()
		{
			// Describe our location request
			var locationRequest = new LocationRequest ()
				.SetInterval (10000)
				.SetFastestInterval (1000)
				.SetPriority (LocationRequest.PriorityHighAccuracy);

			// Check to see if we can request updates first
			if (await CheckLocationAvailability (locationRequest)) {

				// Request updates
				await LocationServices.FusedLocationApi.RequestLocationUpdates (googleApiClient,
					locationRequest, 
					this);
			}
		}

		async Task<bool> CheckLocationAvailability (LocationRequest locationRequest)
		{
			// Build a new request with the given location request
			var locationSettingsRequest = new LocationSettingsRequest.Builder ()
				.AddLocationRequest (locationRequest)
				.Build ();

			// Ask the Settings API if we can fulfill this request
			var locationSettingsResult = await LocationServices.SettingsApi.CheckLocationSettingsAsync (googleApiClient, locationSettingsRequest);


			// If false, we might be able to resolve it by showing the location settings 
			// to the user and allowing them to change the settings
			if (!locationSettingsResult.Status.IsSuccess) {

				if (locationSettingsResult.Status.StatusCode == LocationSettingsStatusCodes.ResolutionRequired)
					locationSettingsResult.Status.StartResolutionForResult (this, 101);
				else
					Toast.MakeText (this, "Location Services Not Available for the given request.", ToastLength.Long).Show ();

				return false;
			}

			return true;
		}

		public void OnConnectionSuspended (int cause)
		{
			Console.WriteLine ("GooglePlayServices Connection Suspended: {0}", cause);
		}

		public void OnConnectionFailed (Android.Gms.Common.ConnectionResult result)
		{
			Console.WriteLine ("GooglePlayServices Connection Failed: {0}", result);
		}

		protected override async void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult (requestCode, resultCode, data);

			// See if we returned from a location settings dialog 
			// and if succesfully, we can try location updates again
			if (requestCode == 101) {
				if (resultCode == Result.Ok)
					await RequestLocationUpdates ();
				else
					Report ("Failed to resolve Location Settings changes", 1);
			}
		}

		public string DescribeLocation (Location location)
		{
			CurrentLocation = location;
			return string.Format ("{0}: {1}, {2} @ {3}",
				location.Provider,
				location.Latitude,
				location.Longitude,
				new DateTime (1970, 1, 1, 0, 0, 0).AddMilliseconds (location.Time));
		}

	}
}