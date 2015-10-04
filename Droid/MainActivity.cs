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
//using Android.Database.Sqlite;
using Android.Net;

using Nearest.ViewModels;
using Nearest.Models;

namespace Nearest.Droid
{
	[Activity (Label = "Nearest", 
		MainLauncher = true, 
		Icon = "@drawable/icon", 
		ScreenOrientation = ScreenOrientation.Portrait
	)]
	public class MainActivity : Activity, ILocationListener
	{
		int count = 1;
		LocationManager locationMgr;
		public TrainListViewModel trainLVM;

		/**
		 * Public UI Elements
		 * 
		 */
		public LinearLayout uptown;
		public LinearLayout downtown;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			RequestWindowFeature (WindowFeatures.NoTitle);
			Window.SetFlags (WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			/**
			 * Detect internet access
			 * 
			 */
			ConnectivityManager connectivityManager = (ConnectivityManager) GetSystemService(ConnectivityService);
			NetworkInfo activeConnection = connectivityManager.ActiveNetworkInfo;
			bool isConnected = (activeConnection != null) && activeConnection.IsConnected;
			var locationProvider = checkLocation ();
			if(locationProvider != null) {
				//locationMgr.RequestLocationUpdates (locationProvider, 2000, 1, this);
				if (isConnected) {
					Location location = locationMgr.GetLastKnownLocation (locationProvider);
					System.Console.WriteLine(
						"DBG: Lat: " + location.Latitude + 
						"\nDBG: Long: " + location.Longitude
					);
					trainLVM = new TrainListViewModel (location.Latitude, location.Longitude);
					getTrainPaths();
				} else {
					// Create ui-based handler for no internet
					System.Console.WriteLine ("DBG: No internet is available");
				}
			} else {
				// Create ui-based handler for unknown location
				System.Console.WriteLine ("DBG: No location providers available");
			}


			// Set Typeface and Styles
			Typeface HnBd = Typeface.CreateFromAsset(Assets,"fonts/HelveticaNeueLTCom-Bd.ttf");
			Typeface HnLt = Typeface.CreateFromAsset(Assets,"fonts/HelveticaNeueLTCom-Lt.ttf");
			Typeface HnMd = Typeface.CreateFromAsset(Assets,"fonts/HelveticaNeueLTCom-Roman.ttf");
			TypefaceStyle tfs = TypefaceStyle.Normal;

			// Main app title and tagline
			TextView title = FindViewById<TextView> (Resource.Id.TextViewTitle);
			TextView tagLn = FindViewById<TextView> (Resource.Id.TextViewTagline);
			TextView timeS = FindViewById<TextView> (Resource.Id.TextTimeSouth);
			TextView destS = FindViewById<TextView> (Resource.Id.TextDestSouth);
			TextView timeN = FindViewById<TextView> (Resource.Id.TextTimeNorth);
			TextView destN = FindViewById<TextView> (Resource.Id.TextDestNorth);

			downtown = FindViewById<LinearLayout> (Resource.Id.LayoutDowntown);
			uptown = FindViewById<LinearLayout> (Resource.Id.LayoutUptown);


			/*Animation hyperspaceJump = AnimationUtils.LoadAnimation(
				this, 
				Resource.Animation.tada
			);*/

			title.SetTypeface(HnBd, tfs);
			tagLn.SetTypeface(HnLt, tfs);
			timeS.SetTypeface(HnMd, tfs);
			destS.SetTypeface(HnLt, tfs);
			timeN.SetTypeface(HnMd, tfs);
			destN.SetTypeface(HnLt, tfs);

			/**
			 * Define UI actions
			 * 
			 */
			Button buttonNorth = uptown.FindViewById<Button> (Resource.Id.ButtonNorth);
			buttonNorth.Click += delegate {
				//buttonNorth.Text = string.Format ("{0}", count++);
				buttonNorth.SetBackgroundResource(
					count%2==0?Resource.Drawable.orange:Resource.Drawable.purple
				);
			};

			Button buttonSouth = downtown.FindViewById<Button> (Resource.Id.ButtonSouth);
			buttonSouth.Click += delegate {
				//title.StartAnimation(hyperspaceJump);
				buttonSouth.SetBackgroundResource(
					count%2==0?Resource.Drawable.red:Resource.Drawable.gray
				);
			};

		}

		/**
		 * Resume Activity
		 * 
		 */
		protected override void OnResume () {
			base.OnResume();
			checkLocation();
		}

		/**
		 * Detect Location
		 * 
		 */
		public string checkLocation(){
			locationMgr = GetSystemService (Context.LocationService) as LocationManager;
			Criteria locationCriteria = new Criteria();
			locationCriteria.Accuracy = Accuracy.Fine;
			locationCriteria.PowerRequirement = Power.NoRequirement;
			String locationProviderName = locationMgr.GetBestProvider (locationCriteria, true);
			String Provider = LocationManager.GpsProvider;

			if(locationMgr.IsProviderEnabled(locationProviderName))
			{
				locationMgr.RequestLocationUpdates(locationProviderName, 30000, 1, this);
			}
			else
			{
				System.Console.WriteLine("DBG: "+ 
					locationProviderName + " " + Provider + 
					" is not available. Does the device have location services enabled?"
				);
			}
			return locationProviderName;
		}

		public void setNextTrains() {
			if ( trainLVM.IsBusy ) {
				System.Console.WriteLine("DBG: Async busy");
			} else {
				//TODO: refactor to a loop 
				var btnDown = downtown.FindViewById<Button> (Resource.Id.ButtonSouth);
				var timeDown = downtown.FindViewById<TextView> (Resource.Id.TextTimeSouth);
				if ( trainLVM.stopListDowntown.Count > 0 ) {
					var nearestDown = trainLVM.stopListDowntown[0].next_train;
					btnDown.Text = nearestDown.route_id;
					btnDown.SetBackgroundResource( getTrainColor(nearestDown.route_id) );
					timeDown.Text = Train.time (nearestDown.ts);
					//btnDown.SetTextColor();
				}
				var btnUp = uptown.FindViewById<Button> (Resource.Id.ButtonNorth);
				var timeUp = uptown.FindViewById<TextView> (Resource.Id.TextTimeNorth);
				if ( trainLVM.stopListUptown.Count > 0 ) {
					var nearestUp = trainLVM.stopListUptown[0].next_train;
					btnUp.Text = nearestUp.route_id;
					btnUp.SetBackgroundResource( getTrainColor(nearestUp.route_id) );
					timeUp.Text = Train.time (nearestUp.ts);
					//btnUp.SetTextColor();
				}
			}
		}

		/**
		 * Get Train Color
		 * 
		 */
		public int getTrainColor( String StopId ) {
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
		async void getTrainPaths() {
			try 
			{
				await trainLVM.GetTrainsAsync();
			}
			catch ( Exception ex )
			{
				System.Console.WriteLine("DBG: Error: No Trains Found");
				System.Console.WriteLine("DBG: " + ex.Message.ToString());
			}
			finally {
				if (!this.trainLVM.IsBusy) {
					System.Console.WriteLine("DBG: Async Complete");
					setNextTrains();
				}
			}
		}

		/**
		 * Called when the user touches a button 
		 * 
		 */
		public void sendMessage(View view) {
			// Do something in response to button click
			System.Console.WriteLine("DBG: sendingMessage");
		}

		/**
		 * Location Updates
		 * 
		 */
		public void OnProviderEnabled (string provider)
		{
			
		}

		public void OnProviderDisabled (string provider)
		{
			
		}

		public void OnStatusChanged (string provider, Availability status, Bundle extras)
		{
			
		}

		public void OnLocationChanged (Android.Locations.Location location)
		{
			
		}
	}
}


