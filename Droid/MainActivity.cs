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
using Android.Support.V4.Widget;
using Android.Gms.Location;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Util;

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
	public class MainActivity : AppCompatActivity, 
	View.IOnTouchListener,
	GoogleApiClient.IConnectionCallbacks, 
	GoogleApiClient.IOnConnectionFailedListener, 
	Android.Gms.Location.ILocationListener,
	SwipeRefreshLayout.IOnRefreshListener
	{
		public TrainListViewModel trainLVM;
		GoogleApiClient googleApiClient;

		public RelativeLayout mainLayout, subLayout;
		public View coordinatorView;
		public SwipeRefreshLayout swipeLayout;
		public ScrollView scrollView;
		public ImageButton swipeButton;

		public bool isFullscreen = false;
		public bool isAtTop = true;
		TimeSpan lastUpdated;

		readonly string[] PermissionsLocation = {
			Manifest.Permission.AccessCoarseLocation,
			Manifest.Permission.AccessFineLocation
		};

		public int startY = 0;
		const int RequestLocationId = 0;

		/// <summary>
		/// Raises the create event.
		/// </summary>
		/// <param name="savedInstanceState">Saved instance state.</param>
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
			subLayout = FindViewById<RelativeLayout> (Resource.Id.subLayout);

			coordinatorView = (View)FindViewById (Resource.Id.CoordinatorView);

			swipeLayout = FindViewById<SwipeRefreshLayout> (Resource.Id.swipeContainer);
			swipeLayout.SetOnRefreshListener (this);
			swipeLayout.SetColorSchemeResources (Resource.Color.red);
			
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
				default:
					break;
				}
			}

			scrollView = FindViewById<ScrollView> (Resource.Id.scrollView);
			scrollView.SetOnTouchListener (this);

			var allButtons = GetViewsByTag (scrollView, "button");
			foreach (Button button in allButtons) {
				button.SetTypeface (HnLt, tfs);
			}

			swipeButton = FindViewById<ImageButton> (Resource.Id.swipeButton);
			swipeButton.SetOnTouchListener (this);

			/**
			 * Define UI actions
			 * 
			 */
			/*swipeButton.Click += delegate(object sender, EventArgs e) {
				if (isFullscreen) {
					Window.ClearFlags (WindowManagerFlags.Fullscreen);
					isFullscreen = false;
				} else {
					Window.SetFlags (WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
					isFullscreen = true;
				}
				ShowAlert (isFullscreen ? "Fullscreen enabled" : "Fullscreen disabled");
			};*/
			for (var i = 0; i < 2; i++) {
				LinearLayout direction = (LinearLayout)mainLayout.GetChildAt (i + 2);

				TextView times = (TextView)direction.FindViewWithTag (tag: "time");
				TextView label = (TextView)direction.FindViewWithTag (tag: "label");
				times.SetTypeface (HnMd, tfs);
				label.SetTypeface (HnLt, tfs);

				Button button = (Button)direction.FindViewWithTag (tag: "button");
				SetTrainsNotice (button, times);
			}
		}

		/// <summary>
		/// Raises the touch event.
		/// </summary>
		/// <param name="v">View.</param>
		/// <param name="e">MotionEvent.</param>
		public bool OnTouch (View v, MotionEvent e)
		{
			int direction = -15;
			if (v.Equals (scrollView)) {
				return true;
			}
			if (startY == 0) {
				startY = (int)e.RawY;
			} else {
				direction = startY - (int)e.RawY;
			}
			switch (e.Action) {
			case MotionEventActions.Down:
			//finger down
				break;
			case MotionEventActions.Move:
			//still moving
				break;
			case MotionEventActions.Up:
			//finger up
				break;
			case MotionEventActions.Cancel:
				if (direction > 15) {
					scrollView.SmoothScrollTo (0, swipeButton.Top);
					int[] state = new int[] { Android.Resource.Attribute.StateExpanded };
					swipeButton.SetImageState (state, false);
				} else if (direction < -15) {
					scrollView.SmoothScrollTo (0, mainLayout.Top);
					swipeButton.SetImageState (new int[] { }, false);
				}
				startY = 0;
				break;
			}

			return true;
		}

		/// <summary>
		/// Raises the refresh event.
		/// </summary>
		public void OnRefresh ()
		{
			if (swipeLayout.Refreshing) {
				HandleConnections ();
			}
		}

		/// <summary>
		/// Raises the resume event.
		/// </summary>
		protected override void OnResume ()
		{
			base.OnResume ();
			var lastUpdate = (DateTime.Now.TimeOfDay - lastUpdated).TotalSeconds;
			Report (String.Format (GetString (Resource.String.info_last_updated), lastUpdate.ToString ()), 0);
			if (lastUpdated.TotalSeconds == 0 || lastUpdate > 30) {
				HandleConnections ();
			} else {
				SetNextTrains ("Resuming.");
			}
		}

		/// <Docs>Called as part of the activity lifecycle when an activity is going into
		///  the background, but has not (yet) been killed.</Docs>
		/// <summary>
		/// Raises the pause event.
		/// </summary>
		protected override void OnPause ()
		{
			base.OnPause ();
			EndLocationUpdates ();
		}

		/// <summary>
		/// Handles the connections.
		/// </summary>
		public void HandleConnections ()
		{
			if (IsConnected ()) {
				if (IsGooglePlayServicesInstalled ()) {
					try {
						Task.Run (() => TryGetLocation ());
					} catch (Exception ex) {
						Report (GetString (Resource.String.error_exception) + ex.Message.ToString (), 0);
					}
				} else {
					Report (GetString (Resource.String.common_google_play_services_api_unavailable_text), 0);
					Snackbar.Make (coordinatorView, 
						Resource.String.error_play_missing, 
						Snackbar.LengthIndefinite)
					.SetAction (Resource.String.snackbar_button_ok, v => HandleConnections ())
					.Show ();
				}
			} else {
				Report (GetString (Resource.String.error_no_internet), 0);
				Snackbar.Make (coordinatorView, 
					Resource.String.error_no_internet, 
					Snackbar.LengthIndefinite)
				.SetAction (Resource.String.snackbar_button_try_again, v => HandleConnections ())
				.Show ();
			}
			return;
		}

		/// <summary>
		/// Determines whether this instance is connected.
		/// </summary>
		/// <returns><c>true</c> if this instance is connected; otherwise, <c>false</c>.</returns>
		public bool IsConnected ()
		{
			ConnectivityManager connectivityManager = (ConnectivityManager)GetSystemService (ConnectivityService);
			NetworkInfo activeConnection = connectivityManager.ActiveNetworkInfo;
			bool IsConnected = (activeConnection != null) && activeConnection.IsConnected;
			return IsConnected;
		}

		/// <summary>
		/// Tries to get the location.
		/// </summary>
		async void TryGetLocation ()
		{
			Report ("Getting location info", 0);
			if ((int)Build.VERSION.SdkInt < 23) {
				await GetLocationAsync ();
				return;
			}

			await GetLocationPermissionAsync ();
		}

		/// <summary>
		/// Gets the location permission async.
		/// </summary>
		/// <returns>The location permission async.</returns>
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
				Snackbar.Make (coordinatorView, 
					Resource.String.error_location_required, 
					Snackbar.LengthIndefinite)
				.SetAction (Resource.String.snackbar_button_ok, 
					v => RequestPermissions (PermissionsLocation, RequestLocationId))
				.Show ();
				return;
			}
			//Finally request permissions with the list of permissions and Id
			RequestPermissions (PermissionsLocation, RequestLocationId);
		}

		/// <summary>
		/// Raises the request permissions result event.
		/// </summary>
		/// <param name="requestCode">Request code.</param>
		/// <param name="permissions">Permissions.</param>
		/// <param name="grantResults">Grant results.</param>
		public override async void OnRequestPermissionsResult 
		(int requestCode, string[] permissions, Permission[] grantResults)
		{
			Report ("Request Code:" + requestCode.ToString (), 0);
			switch (requestCode) {
			case RequestLocationId:
				{
					if (grantResults [0] == Permission.Granted) {
						//Permission granted
						Report ("Permission granted.", 0);
						await GetLocationAsync ();
					} else {
						//Permission Denied :(
						Report (GetString (Resource.String.error_location_permission), 0);
						Snackbar.Make (coordinatorView, 
							Resource.String.error_location_permission, 
							Snackbar.LengthIndefinite)
						.SetAction (Resource.String.snackbar_button_try_again, 
							v => RequestPermissions (PermissionsLocation, RequestLocationId))
							.Show ();
					}
				}
				break;
			}
		}

		/// <summary>
		/// Gets the location async.
		/// </summary>
		/// <returns>The location async.</returns>
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

		/// <summary>
		/// Ends the location updates.
		/// </summary>
		public void EndLocationUpdates ()
		{
			if (googleApiClient != null) {
				LocationServices.FusedLocationApi.RemoveLocationUpdates (googleApiClient, this);
				lastUpdated = DateTime.Now.TimeOfDay;
			}
		}

		/// <summary>
		/// Determines whether this instance is google play services installed.
		/// </summary>
		/// <returns><c>true</c> if this instance is google play services installed; otherwise, <c>false</c>.</returns>
		bool IsGooglePlayServicesInstalled ()
		{
			int queryResult = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable (this);
			if (queryResult == ConnectionResult.Success) {
				return true;
			}

			if (GoogleApiAvailability.Instance.IsUserResolvableError (queryResult)) {
				string errorString = GoogleApiAvailability.Instance.GetErrorString (queryResult);
				Report (String.Format ("There is a problem with Google Play Services: {0} - {1}", 
					queryResult, errorString), 0);
			}
			return false;
		}

		/// <summary>
		/// Gets views by tag.
		/// </summary>
		/// <returns>The views by tag.</returns>
		/// <param name="root">Root View.</param>
		/// <param name="tag">Tag.</param>
		public static List<View> GetViewsByTag (ViewGroup root, String tag)
		{
			List<View> views = new List<View> ();
			int childCount = root.ChildCount;
			for (int i = 0; i < childCount; i++) {
				View child = root.GetChildAt (i);
				if (child is ViewGroup) {
					views.AddRange (GetViewsByTag ((ViewGroup)child, tag));
				} 

				Object tagObj = child.Tag;
				if (tagObj != null && tagObj.ToString () == tag) {
					views.Add (child);
				}

			}
			return views;
		}

		/// <summary>
		/// Sets the next trains.
		/// </summary>
		public void SetNextTrains (String origin)
		{
			RunOnUiThread (() => {
				if (trainLVM == null) {
					Report (origin + " TrainLVM not setup yet.", 0);
					return;
				}
				if (trainLVM.IsBusy) {
					Report (origin + " Still getting trains.", 0);
				} else {
					Report (origin + " Setting next trains.", 0);
					int dir = 0;
					// Loop throuh south and north view groups 
					foreach (List<Stop> stops in trainLVM.stopList) {
						int idx = 0;
						foreach (Stop stop in stops) {
							//no more than 5, including the main
							if (idx > 4) {
								continue;
							}
							LinearLayout path;
							if (idx < 1) {
								path = (LinearLayout)mainLayout.GetChildAt (dir + 2);
							} else {
								path = (LinearLayout)subLayout.GetChildAt (dir);
								subLayout.Visibility = ViewStates.Visible;
								swipeButton.Visibility = ViewStates.Visible;
							}
							var button = (Button)path.FindViewWithTag (tag: "button");
							var buttons = GetViewsByTag (path, "button");
							var time = (TextView)path.FindViewWithTag (tag: "time");
							int subIdx = idx - 1;

							if (idx > 0 && buttons.Count > 0 && subIdx < buttons.Count) {
								button = (Button)buttons [subIdx];
							}

							Report ("SetNextTrain " + dir + " " + idx, 0);
							var nearestTrain = stop.next_train;
							var fartherTrains = stop.trains;
							if (nearestTrain != null) {
								var TrainColor = GetTrainColor (nearestTrain.route_id);

								button.Text = nearestTrain.route_id;
								button.SetBackgroundResource (GetTrainColorDrawable (nearestTrain.route_id));
								button.SetTextColor (Color.White);

								if (time != null) {
									time.Text = Train.time (nearestTrain.ts);
								}
								EventHandler GetDetails = null;
								GetDetails = delegate(object sender, EventArgs args) {
									Report ("Click Event: " + idx, 0);
									//StartActivity (typeof(Detail));
									//Animation hyperspaceJump = AnimationUtils.LoadAnimation (this, Resource.Animation.tada);
									ActivityOptions options = ActivityOptions.MakeScaleUpAnimation (button, 0, 0, 60, 60);
									Intent pendingIntent = new Intent (this, typeof(Detail));

									var toJsonNearestTrain = Newtonsoft.Json.JsonConvert.SerializeObject (nearestTrain);
									pendingIntent.PutExtra ("nearestTrain", toJsonNearestTrain);
									pendingIntent.PutExtra ("nearestTrainColor", TrainColor);

									var toJsonFartherTrains = Newtonsoft.Json.JsonConvert.SerializeObject (fartherTrains);
									pendingIntent.PutExtra ("fartherTrains", toJsonFartherTrains);

									StartActivity (pendingIntent, options.ToBundle ());

									button.Click -= GetDetails;
								};
								if (!button.HasOnClickListeners) {
									button.Click += GetDetails;
								} else {
									button.Click -= GetDetails;
								}
							} else if (idx < 2) {
								SetTrainsNotice (button, time);
							}
							idx++;
						}
						dir++;
					}
					swipeLayout.Refreshing = false;
				}
			});
		}

		/// <summary>
		/// Sets the trains notice.
		/// </summary>
		/// <param name="button">Button.</param>
		/// <param name="time">Time.</param>
		public void SetTrainsNotice (Button button, TextView time)
		{
			button.Text = GetString (Resource.String.error_train_line);
			button.SetBackgroundResource (GetTrainColorDrawable (""));
			time.Text = GetString (Resource.String.error_train_time);
		}

		/// <summary>
		/// Gets the color of the train.
		/// </summary>
		/// <returns>The train color.</returns>
		/// <param name="StopId">Stop identifier.</param>
		public static int GetTrainColor (String StopId)
		{
			int resourceDrawable;

			switch (StopId) {
			case "1":
			case "2":
			case "3":
				resourceDrawable = Resource.Color.red;
				break;
			case "A":
			case "C":
			case "E":
				resourceDrawable = Resource.Color.blue;
				break;
			case "N":
			case "Q":
			case "R":
				resourceDrawable = Resource.Color.yellow;
				break;
			case "4":
			case "5":
			case "6":
				resourceDrawable = Resource.Color.green;
				break;
			case "G": 
				resourceDrawable = Resource.Color.green_alt;
				break;
			case "B":
			case "D":
			case "F":
			case "M":
				resourceDrawable = Resource.Color.orange;
				break;
			case "7":
				resourceDrawable = Resource.Color.purple;
				break;
			case "J":
			case "Z":
				resourceDrawable = Resource.Color.brown;
				break;
			case "L":
			default: 
				resourceDrawable = Resource.Color.gray;
				break;
			}
			return resourceDrawable;
		}

		/// <summary>
		/// Gets the color of the train.
		/// </summary>
		/// <returns>The train color.</returns>
		/// <param name="StopId">Stop identifier.</param>
		public int GetTrainColorDrawable (String StopId)
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
				resourceDrawable = Resource.Drawable.green;
				break;
			case "G": 
				resourceDrawable = Resource.Drawable.green_alt;
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

		/// <summary>
		/// Gets the train models.
		/// </summary>
		/// <param name="locationData">Location data.</param>
		public void GetTrainModels (Location locationData)
		{
			if (locationData != null) {
				Report ("Latitude: " + locationData.Latitude, 0);
				Report ("Longitude: " + locationData.Longitude, 0);

				try {
					if (trainLVM == null) {
						trainLVM = new TrainListViewModel (locationData.Latitude, locationData.Longitude);
						trainLVM.PropertyChanged += delegate {
							Report ("Schedule updated", 0);
							SetNextTrains ("Property changed.");
						};
					}
					// Get trains asynchonously
					Task.Run (() => trainLVM.GetTrainsAsync ());
				} catch (Exception ex) {
					Report ("Exception: " + ex.Message.ToString (), 0);
				} finally {
					if (!this.trainLVM.IsBusy) {
						Report ("Train schedule requested", 0);
					}
				}
			} else {
				Report ("Location missing", 2);
			}
		}

		/// <summary>
		/// Report the specified msg and verbosity.
		/// </summary>
		/// <param name="msg">Message.</param>
		/// <param name="verbosity">Verbosity.</param>
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

		/// <summary>
		/// Shows the alert.
		/// </summary>
		/// <param name="str">String.</param>
		public void ShowAlert (String str)
		{
			var alertString = str == null ? "Unknown Issue" : str;

			if (coordinatorView != null) {
				Snackbar.Make (coordinatorView, alertString, Snackbar.LengthShort).Show ();
			} else {
				ActivityOptions options = ActivityOptions.MakeScaleUpAnimation (mainLayout, 0, 0, 60, 60);
				Intent pendingIntent = new Intent (this, typeof(Alert));
				pendingIntent.PutExtra ("alertBoxText", alertString);
				pendingIntent.PutExtra ("parentWidth", mainLayout.Width);
				StartActivity (pendingIntent, options.ToBundle ());
			}

			return;
		}

		/// <summary>
		/// Raises the location changed event.
		/// </summary>
		/// <param name="NewLocation">New location.</param>
		public void OnLocationChanged (Location NewLocation)
		{
			// Show latest location
			var LocationDescription = DescribeLocation (NewLocation);
			Report ("OnLocationChanged: " + LocationDescription, 0);
			GetTrainModels (NewLocation);
			EndLocationUpdates ();
		}

		/// <summary>
		/// Raises the connected event.
		/// </summary>
		/// <param name="connectionHint">Connection hint.</param>
		public async void OnConnected (Bundle connectionHint)
		{           
			// Get Last known location
			var lastLocation = LocationServices.FusedLocationApi.GetLastLocation (googleApiClient);
			Report (lastLocation == null ? "NULL" : DescribeLocation (lastLocation), 0);

			await RequestLocationUpdates ();
		}

		/// <summary>
		/// Requests the location updates.
		/// </summary>
		/// <returns>The location updates.</returns>
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

		/// <summary>
		/// Checks the location availability.
		/// </summary>
		/// <returns>The location availability.</returns>
		/// <param name="locationRequest">Location request.</param>
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
					Toast.MakeText (this, Resource.String.error_location_unavailable, ToastLength.Long).Show ();

				return false;
			}

			return true;
		}

		/// <summary>
		/// Raises the connection suspended event.
		/// </summary>
		/// <param name="cause">Cause.</param>
		public void OnConnectionSuspended (int cause)
		{
			Report (String.Format ("GooglePlayServices Connection Suspended: {0}", cause), 0);
		}

		/// <summary>
		/// Raises the connection failed event.
		/// </summary>
		/// <param name="result">Result.</param>
		public void OnConnectionFailed (Android.Gms.Common.ConnectionResult result)
		{
			Report (String.Format ("GooglePlayServices Connection Failed: {0}", result), 0);
		}

		/// <Docs>The integer request code originally supplied to
		///  startActivityForResult(), allowing you to identify who this
		///  result came from.</Docs>
		/// <param name="data">An Intent, which can return result data to the caller
		///  (various data can be attached to Intent "extras").</param>
		/// <summary>
		/// Raises the activity result event.
		/// </summary>
		/// <param name="requestCode">Request code.</param>
		/// <param name="resultCode">Result code.</param>
		protected override async void OnActivityResult 
		(int requestCode, Result resultCode, Intent data)
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

		/// <summary>
		/// Describes the location.
		/// </summary>
		/// <returns>The location.</returns>
		/// <param name="location">Location.</param>
		public string DescribeLocation (Location location)
		{
			return string.Format ("{0}: {1}, {2} @ {3}",
				location.Provider,
				location.Latitude,
				location.Longitude,
				new DateTime (1970, 1, 1, 0, 0, 0).AddMilliseconds (location.Time));
		}

	}
}