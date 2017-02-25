using Android.Util;
using Android.OS;
using Android.Content;

namespace Nearest.Droid
{
	public class LocationServiceConnection : Java.Lang.Object, IServiceConnection, IGetLocation
	{
		static readonly string TAG = typeof(LocationServiceConnection).FullName;

		MainActivity mainActivity;
		public LocationServiceConnection(MainActivity activity)
		{
			IsConnected = false;
			Binder = null;
			mainActivity = activity;
		}

		public bool IsConnected { get; private set; }
		public LocationBinder Binder { get; private set; }

		public void OnServiceConnected(ComponentName name, IBinder service)
		{
			Binder = service as LocationBinder;
			IsConnected = Binder != null;
			Log.Debug(TAG, $"OnServiceConnected {name.ClassName}");

			if (IsConnected)
			{
				//mainActivity.timestampMessageTextView.SetText(Resource.String.service_started);
			}
			else
			{
				//mainActivity.timestampMessageTextView.SetText(Resource.String.service_not_connected);
			}

		}

		public void OnServiceDisconnected(ComponentName name)
		{
			Log.Debug(TAG, $"OnServiceDisconnected {name.ClassName}");
			IsConnected = false;
			Binder = null;
			//mainActivity.timestampMessageTextView.SetText(Resource.String.service_not_connected);
		}

		public double[] GetLocationArray()
		{
			if (!IsConnected)
			{
				return null;
			}

			return Binder?.GetLocationArray();
		}
	}

}
