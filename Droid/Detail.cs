using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Nearest.ViewModels;

using Nearest.Models;

namespace Nearest.Droid
{
	[Activity (Label = "Detail")]			
	public class Detail : Activity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetContentView (Resource.Layout.Detail);
			TextView RouteView = FindViewById<TextView> (Resource.Id.textView1);

			var json = Intent.GetStringExtra ("trainLVM");
			var dir = Intent.GetStringExtra ("direction");
			if (json != null) {
				var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<List<Stop>>> (json);
				RouteView.Text = String.Format (
					"Total Payload: {0}\nDirection: {1}\nData: {2}", 
					items.Count, 
					dir, 
					json
				);
			} else {
				RouteView.Text = "Json error!!";
			}
		}
	}
}

