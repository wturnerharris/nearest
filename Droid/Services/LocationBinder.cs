using Android.OS;

namespace Nearest.Droid
{
	public class LocationBinder : Binder, IGetLocation
	{
		public LocationBinder(LocationService service)
		{
			Service = service;
		}

		public LocationService Service { get; private set; }

		public double[] GetLocationArray()
		{
			return Service?.GetLocationArray();
		}
	}
}
