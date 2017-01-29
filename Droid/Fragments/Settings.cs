using Android.OS;
using Android.Content;
using Android.Views;
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

			Settings.VersionNumber = pinfo.VersionCode.ToString();
			Settings.BuildNumber = pinfo.VersionName;
			FindPreference("VersionNumberString").Summary = $"{Settings.VersionNumber} ({Settings.BuildNumber})";
		}


		public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
		{
			base.OnCreateOptionsMenu(menu, inflater);

			//menu.RemoveItem(Resource.Id.action_search);
			//menu.RemoveItem(Resource.Id.action_radar);
			//menu.RemoveItem(Resource.Id.action_settings);
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
			var listPreference = e.Preference as ListPreference;

			int val = 0;

			if (listPreference != null && int.TryParse(e.NewValue.ToString(), out val))
			{
				listPreference.Summary = listPreference.GetEntries()[val];
			}
			else
			{
				e.Preference.Summary = e.NewValue.ToString();
			}
		}
	}
}