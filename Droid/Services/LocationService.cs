using Android.App;
using Android.Util;
using Android.Content;
using Android.OS;
using Android.Locations;

namespace Nearest.Droid
{
	[Service(Name = "com.turnerharris.Nearest.LocationService", Label = "Location Services")]
	public class LocationService : Service, ILocationListener, IGetLocation
	{
		static readonly string TAG = typeof(LocationService).FullName;

		string LocationProvider;
		LocationManager LocationManager;
		string provider;
		double longitude;
		double latitude;

		public IBinder Binder { get; private set; }

		public override void OnCreate()
		{
			// This method is optional to implement
			base.OnCreate();
			Log.Debug(TAG, "OnCreate");
			InitializeLocationManager(this);
			LocationManager.RequestLocationUpdates(LocationManager.NetworkProvider, 4000, 0, this);
			LocationManager.RequestLocationUpdates(LocationManager.GpsProvider, 4000, 0, this);
		}

		public override IBinder OnBind(Intent intent)
		{
			// This method must always be implemented
			Log.Debug(TAG, "OnBind");
			Binder = new LocationBinder(this);
			InitializeLocationManager(this);
			LocationManager.RequestLocationUpdates(LocationManager.NetworkProvider, 4000, 0, this);
			LocationManager.RequestLocationUpdates(LocationManager.GpsProvider, 4000, 0, this);
			return Binder;
		}

		public override bool OnUnbind(Intent intent)
		{
			// This method is optional to implement
			Log.Debug(TAG, "OnUnbind");
			return base.OnUnbind(intent);
		}

		public override void OnDestroy()
		{
			// This method is optional to implement
			Log.Debug(TAG, "OnDestroy");
			Binder = null;
			EndLocationUpdates();
			base.OnDestroy();
		}

		void InitializeLocationManager(Context context)
		{
			LocationManager = (LocationManager)context.GetSystemService(LocationService);
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
		}

		/// <summary>
		/// Ends the location updates.
		/// </summary>
		public void EndLocationUpdates()
		{
			LocationManager.RemoveUpdates(this);
		}

		public void OnProviderDisabled(string provider)
		{
		}

		public void OnProviderEnabled(string provider)
		{
		}

		public void OnStatusChanged(string provider, Availability status, Bundle extras)
		{
		}

		public void OnLocationChanged(Location NewLocation)
		{
			// Show latest location
			provider = NewLocation.Provider;
			latitude = NewLocation.Latitude;
			longitude = NewLocation.Longitude;
			Log.Debug(TAG, $"OnLocationChanged {provider}/{LocationProvider}, {latitude}, {longitude}");

			EndLocationUpdates();
		}

		public double[] GetLocationArray()
		{
			double[] value = { latitude, longitude };
			return value;
		}
	}
}