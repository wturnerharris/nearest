
using Android.App;
using Android.OS;
using Android.Preferences;

namespace Nearest.Droid
{
	[Activity(Label = "Popup", Theme = "@style/InfoTheme")]
	public class Popup : Activity
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Display the fragment as the main content.
			SetContentView(Resource.Layout.Popup);
			FragmentTransaction FragTx = FragmentManager.BeginTransaction();
			FragTx.Add(Resource.Id.content_container, new SettingsFragment()).Commit();

		}
	}
}
