using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Text;
using Android.Graphics;
using Android.Locations;
using Android.Net;
using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Android.Gms.Location;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Util;

using Nearest.ViewModels;
using Nearest.Models;

namespace Nearest.Droid
{
	[Activity(
		Label = "Nearest",
		MainLauncher = true,
		Icon = "@drawable/icon",
		ScreenOrientation = ScreenOrientation.Portrait
	)]
	public class MainActivity : Activity,
	View.IOnTouchListener,
	Android.Locations.ILocationListener,
	GoogleApiClient.IConnectionCallbacks,
	GoogleApiClient.IOnConnectionFailedListener,
	Android.Gms.Location.ILocationListener,
	SwipeRefreshLayout.IOnRefreshListener
	{
		public Nearest NearestApp;
		public TrainListViewModel trainLVM;
		public GoogleApiClient googleApiClient;

		public RelativeLayout mainLayout, subLayout;
		public View coordinatorView;
		public SwipeRefreshLayout swipeLayout;
		public ScrollView scrollView;
		public ImageButton swipeButton;

		bool UseGooglePlayLocations;
		bool UseNearestTrainAPI;
		TimeSpan lastUpdated;
		public Location lastKnown;
		LocationManager LocationManager;
		string LocationProvider;

		readonly string[] PermissionsLocation = {
			Manifest.Permission.AccessCoarseLocation,
			Manifest.Permission.AccessFineLocation
		};

		public int startY;
		const int RequestLocationId = 0;

		/// <summary>
		/// Raises the create event.
		/// </summary>
		/// <param name="savedInstanceState">Saved instance state.</param>
		protected override void OnCreate(Bundle savedInstanceState)
		{
			RequestWindowFeature(WindowFeatures.NoTitle);
			base.OnCreate(savedInstanceState);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);
			UseGooglePlayLocations = true;
			UseNearestTrainAPI = false;

			// Set Typeface and Styles
			TypefaceStyle tfs = TypefaceStyle.Normal;
			Typeface HnBd = Typeface.CreateFromAsset(Assets, "fonts/HelveticaNeueLTCom-Bd.ttf");
			Typeface HnLt = Typeface.CreateFromAsset(Assets, "fonts/HelveticaNeueLTCom-Lt.ttf");
			Typeface HnMd = Typeface.CreateFromAsset(Assets, "fonts/HelveticaNeueLTCom-Roman.ttf");

			var metrics = new DisplayMetrics();
			WindowManager.DefaultDisplay.GetMetrics(metrics);
			int viewport = metrics.HeightPixels - GetStatusBarHeight();

			mainLayout = FindViewById<RelativeLayout>(Resource.Id.mainLayout);
			mainLayout.SetMinimumHeight(viewport * 70 / 100);
			subLayout = FindViewById<RelativeLayout>(Resource.Id.subLayout);
			subLayout.SetMinimumHeight(viewport * 70 / 100);

			coordinatorView = FindViewById(Resource.Id.CoordinatorView);

			swipeLayout = FindViewById<SwipeRefreshLayout>(Resource.Id.swipeContainer);
			swipeLayout.SetOnRefreshListener(this);
			swipeLayout.SetColorSchemeResources(Resource.Color.red);

			int childCount = mainLayout.ChildCount;

			// Main app title and tagline
			for (var i = 0; i < childCount; i++)
			{
				switch (i)
				{
					case 0:
						var title = (TextView)mainLayout.GetChildAt(i);
						title.SetTypeface(HnBd, tfs);
						break;
					case 1:
						var tagLine = (TextView)mainLayout.GetChildAt(i);
						tagLine.SetTypeface(HnLt, tfs);
						break;
				}
			}

			scrollView = FindViewById<ScrollView>(Resource.Id.scrollView);
			scrollView.SetOnTouchListener(this);

			var allButtons = GetViewsByTag(scrollView, "button");
			foreach (Button button in allButtons)
			{
				button.SetTypeface(HnLt, tfs);
			}

			swipeButton = FindViewById<ImageButton>(Resource.Id.swipeButton);
			swipeButton.SetMinimumHeight(viewport * 30 / 100);
			swipeButton.SetOnTouchListener(this);

			for (var i = 0; i < 2; i++)
			{
				var direction = (LinearLayout)mainLayout.GetChildAt(i + 2);
				var times = (TextView)direction.FindViewWithTag("time");
				var label = (TextView)direction.FindViewWithTag("label");
				times.SetTypeface(HnMd, tfs);
				label.SetTypeface(HnLt, tfs);

				var button = (Button)direction.FindViewWithTag("button");
				SetTrainsNotice(button, times);
			}
		}

		protected override void OnStop()
		{
			base.OnStop();
			NearestApp?.DestroyDatabase();
			NearestApp = null;
		}

		public int GetStatusBarHeight()
		{
			int result = 0;
			int resourceId = Resources.GetIdentifier("status_bar_height", "dimen", "android");
			if (resourceId > 0)
			{
				result = Resources.GetDimensionPixelSize(resourceId);
			}
			return result;
		}

		/// <summary>
		/// Raises the touch event.
		/// </summary>
		/// <param name="v">View.</param>
		/// <param name="e">MotionEvent.</param>
		public bool OnTouch(View v, MotionEvent e)
		{
			int direction = -15;
			if (v.Equals(scrollView))
			{
				return true;
			}
			if (startY == 0)
			{
				startY = (int)e.RawY;
			}
			else {
				direction = startY - (int)e.RawY;
			}
			switch (e.Action)
			{
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
					if (direction > 15)
					{
						scrollView.SmoothScrollTo(0, subLayout.Bottom);
						var state = new int[] { Android.Resource.Attribute.StateExpanded };
						swipeButton.SetImageState(state, false);
					}
					else if (direction < -15)
					{
						scrollView.SmoothScrollTo(0, mainLayout.Top);
						swipeButton.SetImageState(new int[] { }, false);
					}
					startY = 0;
					break;
			}

			return true;
		}

		/// <summary>
		/// Raises the refresh event.
		/// </summary>
		public void OnRefresh()
		{
			if (swipeLayout.Refreshing)
			{
				HandleConnections();
				SetNextTrains("Refreshing...");
			}
			else
			{
				swipeLayout.Refreshing = false;
			}
		}

		/// <summary>
		/// Raises the resume event.
		/// </summary>
		protected override void OnResume()
		{
			base.OnResume();
			var notUpdated = TimeSpan.Zero == lastUpdated;
			var lastUpdate = (DateTime.Now.TimeOfDay - lastUpdated).TotalSeconds;
			if (notUpdated || lastUpdate.CompareTo(30.0) > 0)
			{
				if (!notUpdated)
				{
					string type = "seconds";
					if (lastUpdate.CompareTo(60.0) > 0)
					{
						type = "minutes";
						lastUpdate /= 60;
					}
					if (lastUpdate.CompareTo(60.0) > 0)
					{
						type = "hours";
						lastUpdate /= 60;
					}
					var final = lastUpdate.ToString("0.0");
					Report(string.Format(GetString(Resource.String.info_last_updated), final, type), 2);
				}
				Report("Waking up, restarting app...", 0);
				HandleConnections();
				StartApplication();
			}
			else
			{
				SetNextTrains("Resuming...");
			}
		}

		/// <Docs>Called as part of the activity lifecycle when an activity is going into
		///  the background, but has not (yet) been killed.</Docs>
		/// <summary>
		/// Raises the pause event.
		/// </summary>
		protected override void OnPause()
		{
			base.OnPause();
			EndLocationUpdates();
		}

		public void StartApplication()
		{
			if (NearestApp == null)
			{
				Task.Run(() =>
				{
					SQLite.Net.Interop.ISQLitePlatform platform;
					platform = new SQLite.Net.Platform.XamarinAndroid.SQLitePlatformAndroid();
					NearestApp = new Nearest(platform, new Utility());
				});
			}
			else
			{
				Report("Using existing NearestApp instance. Setting next trains", 0);
			}
			if (lastKnown != null && NearestApp != null)
			{
				GetTrainModels(lastKnown);
				Report("StartApplication => GetTrainModels", 0);
			}
			SetNextTrains("Start Application...");
		}

		/// <summary>
		/// Handles the connections.
		/// </summary>
		public void HandleConnections()
		{
			if (!IsConnected())
			{
				Report(GetString(Resource.String.error_no_internet), 0);
				Snackbar.Make(coordinatorView,
					Resource.String.error_no_internet,
					Snackbar.LengthLong)
				.SetAction(Resource.String.snackbar_button_try_again, v => HandleConnections())
				.Show();
			}
			try
			{
				Task.Run(() => TryGetLocation());
			}
			catch (Exception ex)
			{
				Report(GetString(Resource.String.error_exception) + ex.Message, 0);
			}
			swipeLayout.Refreshing = false;
			return;
		}

		/// <summary>
		/// Determines whether this instance is connected.
		/// </summary>
		/// <returns><c>true</c> if this instance is connected; otherwise, <c>false</c>.</returns>
		public bool IsConnected()
		{
			var connectivityManager = (ConnectivityManager)GetSystemService(ConnectivityService);
			NetworkInfo activeConnection = connectivityManager.ActiveNetworkInfo;
			bool conn = (activeConnection != null) && activeConnection.IsConnected;
			return conn;
		}

		public void OnProviderDisabled(string provider)
		{
			Report("Disabled: " + provider + ".", 0);
		}

		public void OnProviderEnabled(string provider)
		{
			Report("Enabled:  " + provider + ".", 0);
		}

		public void OnStatusChanged(string provider, Availability status, Bundle extras)
		{
			Report("Changed:  " + provider + ".", 0);
		}

		/// <summary>
		/// Tries to get the location.
		/// </summary>
		async void TryGetLocation()
		{
			Report("Getting location info", 0);

			// No need to request permission prior to api 23
			if ((int)Build.VERSION.SdkInt < 23)
			{
				await GetLocationAsync();
				return;
			}

			await GetLocationPermissionAsync();
		}

		/// <summary>
		/// Gets the location permission async.
		/// </summary>
		/// <returns>The location permission async.</returns>
		async Task GetLocationPermissionAsync()
		{
			//Check to see if any permission in our group is available, if one, then all are
			const string permission = Manifest.Permission.AccessFineLocation;
			if (CheckSelfPermission(permission) == (int)Permission.Granted)
			{
				Report("Location permission granted.", 0);
				await GetLocationAsync();
				return;
			}

			//need to request permission
			if (ShouldShowRequestPermissionRationale(permission))
			{
				Report("Should show reason for permission.", 0);
				//Explain to the user why we need to read the contacts
				Snackbar.Make(coordinatorView,
					Resource.String.error_location_required,
					Snackbar.LengthIndefinite)
				.SetAction(Resource.String.snackbar_button_ok,
					v => RequestPermissions(PermissionsLocation, RequestLocationId))
				.Show();
				return;
			}
			//Finally request permissions with the list of permissions and Id
			RequestPermissions(PermissionsLocation, RequestLocationId);
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
			Report("Request Code:" + requestCode, 0);
			switch (requestCode)
			{
				case RequestLocationId:
					if (grantResults[0] == Permission.Granted)
					{
						//Permission granted
						Report("Permission granted.", 0);
						await GetLocationAsync();
					}
					else
					{
						//Permission Denied :(
						Report(GetString(Resource.String.error_location_permission), 0);
						Snackbar.Make(coordinatorView,
							Resource.String.error_location_permission,
							Snackbar.LengthIndefinite)
						.SetAction(Resource.String.snackbar_button_try_again,
							v => RequestPermissions(PermissionsLocation, RequestLocationId))
							.Show();
					}
					break;
			}
		}

		/// <summary>
		/// Gets the location async.
		/// </summary>
		/// <returns>The location async.</returns>
		public async Task GetLocationAsync()
		{
			try
			{
				if (IsGooglePlayServicesInstalled() && UseGooglePlayLocations)
				{
					Report("Getting gplay services location async...", 0);
					// register for location updates
					await Task.Run(() =>
					{
						googleApiClient = new GoogleApiClient.Builder(this)
							.AddApi(LocationServices.API)
							.AddConnectionCallbacks(this)
							.AddOnConnectionFailedListener(this)
							.Build();
						googleApiClient.Connect();
					});
				}
				else
				{
					Report("Getting provider location async...", 0);
					// try get location via gps directly
					await Task.Run(() =>
					{
						RunOnUiThread(() =>
						{
							InitializeLocationManager();
							LocationManager.RequestLocationUpdates(LocationProvider, 0, 0, this);
						});
					});
				}
			}
			catch (Exception ex)
			{
				Report("Unable to get location\nDBG Exception: " + ex, 2);
				Report(GetString(Resource.String.common_google_play_services_api_unavailable_text), 0);
				Snackbar.Make(coordinatorView,
					Resource.String.error_play_missing,
					Snackbar.LengthIndefinite)
				.SetAction(Resource.String.snackbar_button_ok, v => HandleConnections())
				.Show();

			}
		}

		void InitializeLocationManager()
		{
			LocationManager = (LocationManager)GetSystemService(LocationService);
			var LocationCrtieria = new Criteria
			{
				Accuracy = Accuracy.Fine
			};
			var acceptableLocationProviders = LocationManager.GetProviders(LocationCrtieria, true);

			if (acceptableLocationProviders.Count > 0)
			{
				LocationProvider = acceptableLocationProviders[0];
			}
			else
			{
				LocationProvider = string.Empty;
			}
			Report("Using " + LocationManager + " . " + LocationProvider, 0);
		}

		/// <summary>
		/// Ends the location updates.
		/// </summary>
		public void EndLocationUpdates()
		{
			if (googleApiClient != null && !googleApiClient.IsConnecting)
			{
				LocationServices.FusedLocationApi.RemoveLocationUpdates(googleApiClient, this);
			}
			LocationManager?.RemoveUpdates(this);
			lastUpdated = DateTime.Now.TimeOfDay;
		}

		/// <summary>
		/// Determines whether this instance is google play services installed.
		/// </summary>
		/// <returns><c>true</c> if this instance is google play services installed; otherwise, <c>false</c>.</returns>
		bool IsGooglePlayServicesInstalled()
		{
			int queryResult = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
			if (queryResult == ConnectionResult.Success)
			{
				return true;
			}

			if (GoogleApiAvailability.Instance.IsUserResolvableError(queryResult))
			{
				string errorString = GoogleApiAvailability.Instance.GetErrorString(queryResult);
				Report(string.Format("There is a problem with Google Play Services: {0} - {1}",
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
		public static List<View> GetViewsByTag(ViewGroup root, string tag)
		{
			var views = new List<View>();
			int childCount = root.ChildCount;
			for (int i = 0; i < childCount; i++)
			{
				View child = root.GetChildAt(i);
				if (child is ViewGroup)
				{
					views.AddRange(GetViewsByTag((ViewGroup)child, tag));
				}

				object tagObj = child.Tag;
				if (tagObj?.ToString() == tag)
				{
					views.Add(child);
				}

			}
			return views;
		}

		/// <summary>
		/// Sets the next trains. This only sets trains and never gets them.
		/// </summary>
		public void SetNextTrains(string origin)
		{
			if (trainLVM == null)
			{
				Report(origin + " TrainLVM not setup yet.", 0);
				return;
			}
			if (trainLVM.IsBusy)
			{
				Report(origin + " Still getting trains.", 0);
			}
			else
			{
				Report(origin + " Setting next trains.", 0);
				RunOnUiThread(() =>
				{
					int dir = 0;
					// Loop throuh south and north view groups 
					foreach (var stops in trainLVM.stopList)
					{
						int idx = 0;
						foreach (var stop in stops)
						{
							//no more than 5, including the main
							if (idx > 4)
							{
								break;
							}
							LinearLayout path;
							if (idx < 1)
							{
								path = (LinearLayout)mainLayout.GetChildAt(dir + 2);
							}
							else
							{
								path = (LinearLayout)subLayout.GetChildAt(dir);
								subLayout.Visibility = ViewStates.Visible;
								swipeButton.Visibility = ViewStates.Visible;
							}
							var button = (Button)path.FindViewWithTag("button");
							var buttons = GetViewsByTag(path, "button");
							var time = (TextView)path.FindViewWithTag("time");
							int subIdx = idx - 1;

							if (idx > 0 && buttons.Count > 0 && subIdx < buttons.Count)
							{
								button = (Button)buttons[subIdx];
							}

							var nearestTrain = stop != null ? stop.next_train : null;
							if (nearestTrain != null)
							{
								/*if (nearestTrain.ExpiredUnder(15))
								{
									Report("===>ExpiredUnder", 0);
									// refresh these if the first time is stale
									HandleConnections();
									return;
								}*/

								button.Text = nearestTrain.route_id.Substring(0, 1);
								button.SetBackgroundResource(GetTrainColorDrawable(nearestTrain.route_id));
								button.SetTextColor(Color.White);
								button.Click -= stop.clickHandler;
								// TODO: animate in
								button.Visibility = ViewStates.Visible;
								//Animation anim = AnimationUtils.LoadAnimation (this, Resource.Animation.tada);

								time?.Text = Train.time(nearestTrain.ts);

								if (!button.HasOnClickListeners)
								{
									stop.clickHandler = delegate
									{
										ActivityOptions options = ActivityOptions.MakeScaleUpAnimation(button, 0, 0, 60, 60);
										var pendingIntent = new Intent(this, typeof(Detail));

										Train nearest = nearestTrain;
										List<Train> next = stop.trains;

										var toJsonNearestTrain = Newtonsoft.Json.JsonConvert.SerializeObject(nearest);
										pendingIntent.PutExtra("nearestTrain", toJsonNearestTrain);
										pendingIntent.PutExtra("nearestTrainColor", GetTrainColor(nearestTrain.route_id));

										var toJsonFartherTrains = Newtonsoft.Json.JsonConvert.SerializeObject(next);
										pendingIntent.PutExtra("fartherTrains", toJsonFartherTrains);

										StartActivity(pendingIntent, options.ToBundle());
										button.Click -= stop.clickHandler;
									};

									// event needed to clear click handlers after update
									EventHandler<AfterTextChangedEventArgs> removeHandlers = null;
									removeHandlers = delegate
									{
										if (stop.clickHandler != null)
										{
											button.Click -= stop.clickHandler;
											// TODO: animate out
											button.Visibility = ViewStates.Invisible;
											button.AfterTextChanged -= removeHandlers;
										}
									};
									button.AfterTextChanged += removeHandlers;
									button.Click += stop.clickHandler;
								}
							}
							else
							{
								SetTrainsNotice(button, time);
								Report("NearestTrain was null", 0);
							}
							idx++;
						}
						dir++;
					}
				});
				swipeLayout.Refreshing = false;
			}
		}

		/// <summary>
		/// Sets the trains notice.
		/// </summary>
		/// <param name="button">Button.</param>
		/// <param name="time">Time.</param>
		public void SetTrainsNotice(Button button, TextView time)
		{
			button.Text = GetString(Resource.String.error_train_line);
			button.SetBackgroundResource(GetTrainColorDrawable(""));
			if (time != null)
			{
				time.Text = GetString(Resource.String.error_train_time);
			}
		}

		/// <summary>
		/// Gets the color of the train.
		/// </summary>
		/// <returns>The train color.</returns>
		/// <param name="StopId">Stop identifier.</param>
		public static int GetTrainColor(string StopId)
		{
			int resourceDrawable;

			switch (StopId)
			{
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
				case "5X":
				case "6":
				case "6X":
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
				case "7X":
					resourceDrawable = Resource.Color.purple;
					break;
				case "J":
				case "Z":
					resourceDrawable = Resource.Color.brown;
					break;
				case "L":
					resourceDrawable = Resource.Color.gray;
					break;
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
		public int GetTrainColorDrawable(string StopId)
		{
			int resourceDrawable;

			switch (StopId)
			{
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
				case "5X":
				case "6":
				case "6X":
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
				case "7X":
					resourceDrawable = Resource.Drawable.purple;
					break;
				case "J":
				case "Z":
					resourceDrawable = Resource.Drawable.brown;
					break;
				case "L":
					resourceDrawable = Resource.Drawable.gray;
					break;
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
		public void GetTrainModels(Location locationData)
		{
			if (locationData != null)
			{
				var LocationDescription = DescribeLocation(locationData);
				Report("GetTrainModels: " + LocationDescription, 0);

				if (trainLVM == null)
				{
					trainLVM = new TrainListViewModel(locationData.Latitude, locationData.Longitude);

					// listen to property changes
					trainLVM.PropertyChanged += delegate
					{
						SetNextTrains("Property changed.");
					};
				}
				trainLVM.SetLocation(locationData.Latitude, locationData.Longitude);

				if (IsConnected() && UseNearestTrainAPI)
				{
					// Get trains asynchonously from remote api
					//TODO: get trains from transit api feed
					Report("Getting trains async...", 0);
					Task.Run(() => trainLVM.GetTrainsAsync());
				}
				else
				{
					// get the trains from schedule
					Report("Getting train synchronously...", 0);
					Task.Run(() => trainLVM.GetTrains(NearestApp));
				}
			}
			else {
				Report("Location missing", 2);
			}
		}

		/// <summary>
		/// Report the specified msg and verbosity.
		/// </summary>
		/// <param name="msg">Message.</param>
		/// <param name="verbosity">Verbosity.</param>
		public void Report(string msg, int verbosity)
		{
			RunOnUiThread(() =>
			{
				var appName = GetString(Resource.String.title) ?? "Nearest";
				switch (verbosity)
				{
					case 0:
						Log.Debug(appName, "DBG: " + msg);
						break;
					case 1:
						Toast.MakeText(this, msg, ToastLength.Short).Show();
						break;
					case 2:
						Report(msg, 0);
						Report(msg, 1);
						break;
				}
			});
			return;
		}

		/// <summary>
		/// Shows the alert.
		/// </summary>
		/// <param name="str">string.</param>
		public void ShowAlert(string str)
		{
			var alertString = str ?? "Unknown Issue";

			if (coordinatorView != null)
			{
				Snackbar.Make(coordinatorView, alertString, Snackbar.LengthShort).Show();
			}

			return;
		}

		/// <summary>
		/// Raises the location changed event.
		/// </summary>
		/// <param name="NewLocation">New location.</param>
		public void OnLocationChanged(Location NewLocation)
		{
			// Show latest location
			var LocationDescription = DescribeLocation(NewLocation);
			Report("OnLocationChanged: " + LocationDescription, 0);
			lastKnown = NewLocation;
			GetTrainModels(NewLocation);
			EndLocationUpdates();
		}

		/// <summary>
		/// Raises the connected event.
		/// </summary>
		/// <param name="connectionHint">Connection hint.</param>
		public async void OnConnected(Bundle connectionHint)
		{
			// Get Last known location
			var lastLocation = LocationServices.FusedLocationApi.GetLastLocation(googleApiClient);
			Report(lastLocation == null ? "lastLocation is null" : DescribeLocation(lastLocation), 0);

			await RequestLocationUpdates();
		}

		/// <summary>
		/// Requests the location updates.
		/// </summary>
		/// <returns>The location updates.</returns>
		async Task RequestLocationUpdates()
		{
			// Describe our location request
			var locationRequest = new LocationRequest()
				.SetInterval(10000)
				.SetFastestInterval(1000)
				.SetPriority(LocationRequest.PriorityHighAccuracy);

			// Check to see if we can request updates first
			if (await CheckLocationAvailability(locationRequest))
			{

				// Request updates
				await LocationServices.FusedLocationApi.RequestLocationUpdates(googleApiClient,
					locationRequest,
					this);
			}
		}

		/// <summary>
		/// Checks the location availability.
		/// </summary>
		/// <returns>The location availability.</returns>
		/// <param name="locationRequest">Location request.</param>
		async Task<bool> CheckLocationAvailability(LocationRequest locationRequest)
		{
			// Build a new request with the given location request
			var locationSettingsRequest = new LocationSettingsRequest.Builder()
				.AddLocationRequest(locationRequest)
				.Build();

			// Ask the Settings API if we can fulfill this request
			var locationSettingsResult = await LocationServices.SettingsApi.CheckLocationSettingsAsync(googleApiClient, locationSettingsRequest);


			// If false, we might be able to resolve it by showing the location settings 
			// to the user and allowing them to change the settings
			if (!locationSettingsResult.Status.IsSuccess)
			{

				if (locationSettingsResult.Status.StatusCode == CommonStatusCodes.ResolutionRequired)
					locationSettingsResult.Status.StartResolutionForResult(this, 101);
				else
					Toast.MakeText(this, Resource.String.error_location_unavailable, ToastLength.Long).Show();

				return false;
			}

			return true;
		}

		/// <summary>
		/// Raises the connection suspended event.
		/// </summary>
		/// <param name="cause">Cause.</param>
		public void OnConnectionSuspended(int cause)
		{
			Report(string.Format("GooglePlayServices Connection Suspended: {0}", cause), 0);
		}

		/// <summary>
		/// Raises the connection failed event.
		/// </summary>
		/// <param name="result">Result.</param>
		public void OnConnectionFailed(ConnectionResult result)
		{
			Report(string.Format("GooglePlayServices Connection Failed: {0}", result), 0);
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
			base.OnActivityResult(requestCode, resultCode, data);

			// See if we returned from a location settings dialog 
			// and if succesfully, we can try location updates again
			if (requestCode == 101)
			{
				if (resultCode == Result.Ok)
					await RequestLocationUpdates();
				else
					Report("Failed to resolve Location Settings changes", 1);
			}
		}

		/// <summary>
		/// Describes the location.
		/// </summary>
		/// <returns>The location.</returns>
		/// <param name="location">Location.</param>
		public string DescribeLocation(Location location)
		{
			return string.Format("{0}: {1}, {2} @ {3}",
				location.Provider,
				location.Latitude,
				location.Longitude,
				new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds(location.Time));
		}

	}

}