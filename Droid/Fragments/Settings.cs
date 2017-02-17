using Android.OS;
using Android.Content;
using Android.Preferences;

using SettingsStudio;

namespace Nearest.Droid
{
	public class SettingsFragment : PreferenceFragment
	{

		static readonly string[] UomListPreferences = {
			SettingsKeys.UomTime,
			SettingsKeys.UomDistance
		};

		static readonly string[] UomSeekBarPreferences = {
			SettingsKeys.UomTimeThreshold,
			SettingsKeys.UomDistanceThreshold
		};

		public override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			SetHasOptionsMenu(true);
			AddPreferencesFromResource(Resource.Xml.Preferences);

			Context context = Context;
			var pinfo = context.PackageManager.GetPackageInfo(context.PackageName, 0);

			Settings.VersionNumber = pinfo.VersionName;
			Settings.BuildNumber = pinfo.VersionCode.ToString();
			FindPreference(SettingsKeys.VersionNumber).Summary = $"{Settings.VersionNumber} ({Settings.BuildNumber})";

		}

		public override void OnResume()
		{
			base.OnResume();

			//Analytics.TrackPageViewStart(this, Pages.Settings);
			foreach (var item in UomListPreferences)
			{
				var preference = (ListPreference)FindPreference(item);
				preference.PreferenceChange += handlePreferenceChange;
				preference.Summary = preference.Entry ?? " ";
			}
			foreach (var item in UomSeekBarPreferences)
			{
				var preference = FindPreference(item);
				string timeFormat;
				switch (preference.Key)
				{
					case SettingsKeys.UomDistanceThreshold:
						timeFormat = "Showing stations within {0} miles.";
						float floatVal = Settings.UomDistanceThreshold > 0 ? ((int)Settings.UomDistanceThreshold / 8f) * 2f : 0;
						preference.Title = string.Format(timeFormat, floatVal);
						break;
					case SettingsKeys.UomTimeThreshold:
						timeFormat = "Showing trains arriving {0}.";
						var stringVal = Settings.UomTimeThreshold == 0 ? "now" : string.Format("after {0} mins", Settings.UomTimeThreshold);
						preference.Title = string.Format(timeFormat, stringVal); //"after 3 minutes" or "now"
						break;
					default:
						preference.Title = preference.Title;
						break;
				}
				preference.PreferenceChange += handlePreferenceChange;
			}
		}


		public override void OnPause()
		{
			//Analytics.TrackPageViewEnd(this);

			base.OnPause();

			foreach (var item in UomListPreferences)
			{
				var preference = (ListPreference)FindPreference(item);
				preference.PreferenceChange -= handlePreferenceChange;
			}
			foreach (var item in UomSeekBarPreferences)
			{
				var preference = FindPreference(item);
				preference.PreferenceChange -= handlePreferenceChange;
			}
		}


		void handlePreferenceChange(object sender, Preference.PreferenceChangeEventArgs e)
		{
			int val = 0;
			var listPreference = e.Preference as ListPreference;
			var newValue = e.NewValue.ToString();
			string timeFormat;

			if (listPreference != null && int.TryParse(newValue, out val))
			{
				listPreference.Summary = listPreference.GetEntries()[val];
			}
			else
			{
				switch (e.Preference.Key)
				{
					case SettingsKeys.UomDistanceThreshold:
						timeFormat = "Showing stations within {0} miles.";
						var floatVal = int.Parse(newValue) > 0 ? (int.Parse(newValue) / 8f) * 2f : 0;
						e.Preference.Title = string.Format(timeFormat, floatVal);
						break;
					case SettingsKeys.UomTimeThreshold:
						timeFormat = "Showing trains arriving {0}.";
						var stringVal = newValue == "0" ? "now" : string.Format("after {0} mins", newValue);
						e.Preference.Title = string.Format(timeFormat, stringVal); //"after 3 minutes" or "now"
						break;
					default:
						e.Preference.Title = e.Preference.Title + ": " + e.NewValue;
						break;
				}
			}
		}
	}
}