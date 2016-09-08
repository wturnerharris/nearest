using Android.App;
using Android.Preferences;
using Android.OS;
using Android.Content;
using Android.Content.PM;

namespace Nearest.Droid
{
	/// <summary>
	/// Parent setting activity, all it does is load up the headers
	/// </summary>
	[Activity(
		Label = "@string/menu_settings",
		Icon = "@drawable/ic_launcher",
		Theme = "@style/ThemeActionBar",
		ScreenOrientation = ScreenOrientation.Portrait
	)]
	public class SettingsFragment : PreferenceFragment
	{
		public override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			AddPreferencesFromResource(Resource.Xml.Preferences);
			//var intent = new Intent(Activity, typeof(MainActivity));
			//AddPreferencesFromIntent(intent);
		}

		public void OnSharedPreferenceChanged(ISharedPreferences sharedPreferences, string key)
		{

		}
	}
}