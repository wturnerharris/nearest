using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Text;
using Android.Graphics;

using Nearest.ViewModels;

namespace Nearest.Droid
{
	[Activity (Label = "Nearest.Droid", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		int count = 1;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			RequestWindowFeature (WindowFeatures.NoTitle);
			Window.SetFlags (WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			//TrainListViewModel train = new TrainListViewModel ();

			// Set Typeface and Styles
			Typeface HnBd = Typeface.CreateFromAsset(Assets,"fonts/HelveticaNeueLTCom-Bd.ttf");
			Typeface HnLt = Typeface.CreateFromAsset(Assets,"fonts/HelveticaNeueLTCom-Lt.ttf");
			Typeface HnMd = Typeface.CreateFromAsset(Assets,"fonts/HelveticaNeueLTCom-Md.ttf");
			TypefaceStyle tfs = TypefaceStyle.Normal;

			// Main app title and tagline
			TextView title = FindViewById<TextView> (Resource.Id.TextViewTitle);
			TextView tag = FindViewById<TextView> (Resource.Id.TextViewTagline);

			title.SetTypeface(HnBd, tfs);
			tag.SetTypeface(HnLt, tfs);

			// Get our button from the layout resource,
			Button buttonNorth = FindViewById<Button> (Resource.Id.ButtonNorth);

			// and attach an event to it
			buttonNorth.Click += delegate {
				buttonNorth.Text = string.Format ("{0}", count++);
			};

			Button buttonSouth = FindViewById<Button> (Resource.Id.ButtonSouth);
			buttonSouth.Click += delegate {
				buttonSouth.SetBackgroundResource(
					count%2==0?Resource.Drawable.green:Resource.Drawable.yellow
				);
			};

		}

		/** Called when the user touches a button */
		public void sendMessage(View view) {
			// Do something in response to button click
			System.Console.WriteLine("sendingMessage");
		}
	}
}


