using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Text;
using Android.Graphics;
using Android.Views.Animations;

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
			Typeface HnMd = Typeface.CreateFromAsset(Assets,"fonts/HelveticaNeueLTCom-Roman.ttf");
			TypefaceStyle tfs = TypefaceStyle.Normal;

			// Main app title and tagline
			TextView title = FindViewById<TextView> (Resource.Id.TextViewTitle);
			TextView tagLn = FindViewById<TextView> (Resource.Id.TextViewTagline);
			TextView timeS = FindViewById<TextView> (Resource.Id.TextTimeSouth);
			TextView destS = FindViewById<TextView> (Resource.Id.TextDestSouth);
			TextView timeN = FindViewById<TextView> (Resource.Id.TextTimeNorth);
			TextView destN = FindViewById<TextView> (Resource.Id.TextDestNorth);

			Animation hyperspaceJump = AnimationUtils.LoadAnimation(
				this, 
				Resource.Animation.tada
			);

			title.SetTypeface(HnBd, tfs);
			tagLn.SetTypeface(HnLt, tfs);
			timeS.SetTypeface(HnMd, tfs);
			destS.SetTypeface(HnLt, tfs);
			timeN.SetTypeface(HnMd, tfs);
			destN.SetTypeface(HnLt, tfs);

			// Get our button from the layout resource,
			Button buttonNorth = FindViewById<Button> (Resource.Id.ButtonNorth);

			// and attach an event to it
			buttonNorth.Click += delegate {
				buttonNorth.Text = string.Format ("{0}", count++);
				buttonNorth.SetBackgroundResource(
					count%2==0?Resource.Drawable.orange:Resource.Drawable.purple
				);
			};

			Button buttonSouth = FindViewById<Button> (Resource.Id.ButtonSouth);
			buttonSouth.Click += delegate {
				title.StartAnimation(hyperspaceJump);
				buttonSouth.SetBackgroundResource(
					count%2==0?Resource.Drawable.red:Resource.Drawable.gray
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


