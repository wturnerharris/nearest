using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Java.Lang;

namespace Nearest.Droid
{
	[Activity (Label = "Alert")]			
	public class Alert : Activity //DialogFragment
	{
		static int EXIT_DELAY = 3500;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Create your application here
			RequestWindowFeature (WindowFeatures.NoTitle);
			SetContentView (Resource.Layout.Alert);
			TextView details = (TextView)FindViewById (Resource.Id.AlertContent);
			this.Window.SetBackgroundDrawableResource (Android.Resource.Color.Transparent);
			string strNotificationToShow = "Oops.. Something went wrong.";
			int myParentWidth = this.Window.Attributes.Width;
			try {
				Bundle extras = this.Intent.Extras;
				if (extras != null) {	
					strNotificationToShow = extras.GetString ("alertBoxText");
					myParentWidth = extras.GetInt ("parentWidth");
				} else {
					strNotificationToShow = "Oops.. Something went wrong.";
					myParentWidth = this.Window.Attributes.Width;
				}
			} catch (System.Exception e) {
				System.Console.WriteLine (e.ToString ());
				strNotificationToShow = "Oops.. Something went wrong.";
				myParentWidth = this.Window.Attributes.Width;
			} finally {
				details.Text = strNotificationToShow;
			}
			WindowManagerLayoutParams Params = this.Window.Attributes;
			Params.Width = myParentWidth;
			Params.Gravity = GravityFlags.Bottom;

			this.Window.Attributes = (Params);

			Window window = this.Window;
			window.Attributes = ((WindowManagerLayoutParams)Params);
			window.ClearFlags (WindowManagerFlags.DimBehind);
			window.SetFlags (WindowManagerFlags.NotTouchModal, WindowManagerFlags.NotTouchModal);

			new Handler ().PostDelayed (Exit (), EXIT_DELAY);

		}

		private Runnable Exit ()
		{
			return new Runnable (() => Finish ());
		}
	}
}
