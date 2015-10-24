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

using Nearest.ViewModels;
using Nearest.Models;

namespace Nearest.Droid
{
	[Activity (
		Label = "Nearest", 
		MainLauncher = true, 
		Icon = "@drawable/icon", 
		ScreenOrientation = ScreenOrientation.Portrait
	)]
	public class MainActivity : AppCompatActivity, ILocationListener
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
		public bool isFullscreen = false;
		public Intent pendingIntent;

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
			pendingIntent = new Intent (this, typeof(Detail));

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
			};
			for (var i = 0; i < 2; i++) {
				var direction = i == 0 ? southLayout : northLayout;

				TextView times = (TextView)direction.FindViewWithTag (tag: "time");
				TextView label = (TextView)direction.FindViewWithTag (tag: "label");
				times.SetTypeface (HnMd, tfs);
				label.SetTypeface (HnLt, tfs);

				Button button = (Button)direction.FindViewWithTag (tag: "button");
				int count = i;
				button.FindViewWithTag (tag: "button").Click += delegate {
					//StartActivity (typeof(Detail));
					//Animation hyperspaceJump = AnimationUtils.LoadAnimation (this, Resource.Animation.tada);

					ActivityOptions options = ActivityOptions.MakeScaleUpAnimation (button, 0, 0, 60, 60);
					if (pendingIntent == null) {
						pendingIntent = new Intent (this, typeof(Detail));
					}
					if (trainLVM != null) {
						var toJson = Newtonsoft.Json.JsonConvert.SerializeObject (trainLVM.stopList);
						pendingIntent.PutExtra ("trainLVM", toJson);
					}
					StartActivity (pendingIntent, options.ToBundle ());

					button.Text = string.Format ("{0}", count++);
					button.SetBackgroundResource (
						count % 2 == 0 ? Resource.Drawable.Orange : Resource.Drawable.Purple
					);
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
			if (locationMgr != null) {
				locationMgr.RemoveUpdates (this);
			}
		}

		/**
		 * Handle connections; network and location
		 * 
		 */
		public void handleConnections (String source)
		{
			Toast.MakeText (this, "Getting location info", ToastLength.Long).Show ();
			if (IsConnected ()) {
				try {
					Task.RunOnUIThread (() => TryGetLocation ());
					Task.RunOnUIThread (() => getTrainModelsAsync ());
				} catch (Exception ex) {
					System.Console.WriteLine ("DBG: Exception (handleConnections):\n" +
					ex.Message.ToString ());
				}
			} else {
				// Create ui-based handler for no internet
				System.Console.WriteLine ("DBG: No internet is available");
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
				System.Console.WriteLine ("DBG: Location permission granted.");
				await GetLocationAsync ();
				return;
			}

			//need to request permission
			if (ShouldShowRequestPermissionRationale (permission)) {
				System.Console.WriteLine ("DBG: Should show reason for permission.");
				//Explain to the user why we need to read the contacts
				Snackbar.Make (mainLayout, 
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
			System.Console.WriteLine ("DBG: Request Code:" + requestCode.ToString ());
			switch (requestCode) {
			case RequestLocationId:
				{
					if (grantResults [0] == Permission.Granted) {
						System.Console.WriteLine ("DBG: permission available");
						//Permission granted
						Toast.MakeText (this,
							"Location permission is available, getting lat/long",
							ToastLength.Long).Show ();
						
						await GetLocationAsync ();
					} else {
						//Permission Denied :(
						//Disabling location functionality
						System.Console.WriteLine ("DBG: Location permission is denied.");
						Toast.MakeText (this,
							"Location permission is denied",
							ToastLength.Long).Show ();
					}
				}
				break;
			}
		}

		public async Task GetLocationAsync ()
		{ 
			System.Console.WriteLine ("DBG: Getting Location Async...");
			try {
				location = getLocation ();
				await Task.Run (() => getLocation ());
			} catch (Exception ex) {
				Toast.MakeText (this,
					"Unable to get location",
					ToastLength.Long).Show ();
				System.Console.WriteLine ("DBG: Unable to get location: " + ex.ToString ());
			} finally {
				
			}
		}

		public Location getLocation ()
		{
			locationMgr = GetSystemService (Context.LocationService) as LocationManager;

			Criteria locationCriteria = new Criteria ();
			locationCriteria.Accuracy = Accuracy.Fine;
			locationCriteria.PowerRequirement = Power.NoRequirement;
			String LocationManagerBestProvider = locationMgr.GetBestProvider (locationCriteria, true);

			string[] providers = {
				LocationManager.PassiveProvider,
				LocationManagerBestProvider,
				LocationManager.NetworkProvider
			};
			int i;
			for (i = 0; i < providers.Length; i++) {
				System.Console.WriteLine ("DBG: Trying to get location, attempt #" + i.ToString ());
				if (providers [i] != null && locationMgr.IsProviderEnabled (providers [i])) {
					System.Console.WriteLine ("DBG: Requesting Updates for " + providers [i]);
					locationMgr.RequestLocationUpdates (providers [i], 100, 0, this);
				}
			}
			location = locationMgr.GetLastKnownLocation (providers [i - 1]);
			return location;
		}

		/**
		 * Set next train info
		 * 
		 */
		public void setNextTrains ()
		{
			if (trainLVM.IsBusy) {
				Toast.MakeText (this,
					"TrainVLM is busy",
					ToastLength.Long).Show ();
				System.Console.WriteLine ("DBG: Async busy");
			} else {
				System.Console.WriteLine ("DBG: Setting next trains...");
				// Loop throuh south and north view groups 
				for (var i = 0; i < 2; i++) {
					System.Console.WriteLine ("DBG: SetNextTrain" + i.ToString ());
					var path = i == 0 ? southLayout : northLayout;
					var button = (Button)path.FindViewWithTag (tag: "button");
					var time = (TextView)path.FindViewWithTag (tag: "time");

					if (trainLVM.stopList.Count > 0) {
						foreach (var direction in trainLVM.stopList) {
							var nearestDirection = trainLVM.stopList [i] [0].next_train;
							if (nearestDirection != null) {
								button.Text = nearestDirection.route_id;
								button.SetBackgroundResource (getTrainColor (nearestDirection.route_id));
								button.SetTextColor (Color.White);
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
		public async Task getTrainModelsAsync ()
		{
			if (location != null) {
				Toast.MakeText (this,
					"Lat: " + location.Latitude +
					"\nLong: " + location.Longitude,
					ToastLength.Long).Show ();
				System.Console.WriteLine (
					"DBG: Lat: " + location.Latitude +
					"\nDBG: Long: " + location.Longitude
				);

				try {
					trainLVM = new TrainListViewModel (location.Latitude, location.Longitude);
					await trainLVM.GetTrainsAsync ();
				} catch (Exception ex) {
					System.Console.WriteLine ("DBG: Exception: " + ex.Message.ToString ());
				} finally {
					if (!this.trainLVM.IsBusy) {
						Toast.MakeText (this,
							"Train schedule received",
							ToastLength.Long).Show ();
						setNextTrains ();
					}
				}
			} else {
				Toast.MakeText (this,
					"Location not available",
					ToastLength.Long).Show ();
				System.Console.WriteLine ("DBG: problem getting getTrainModelsAsync");
			}
		}

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
			if (locationChanged != null) {
				location = locationChanged;
				locationMgr.RemoveUpdates (this);
				Task.Run (() => getTrainModelsAsync ());
			}
		}
	}
}