
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
using Nearest.ViewModels;

namespace Nearest.Droid
{
	[Activity (Label = "Detail")]			
	public class Detail : Activity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetContentView (Resource.Layout.Detail);

			var trains = Intent.GetStringExtra ("trains");
			System.Console.WriteLine (trains);
		}
	}
}

