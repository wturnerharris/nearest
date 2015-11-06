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

			var nearestTrain = Intent.GetStringExtra ("nearestTrain");
			var dir = Intent.GetStringExtra ("direction");
			if (nearestTrain != null) {
				var train = Newtonsoft.Json.JsonConvert.DeserializeObject<Train> (nearestTrain);
				RouteView.Text = String.Format (
					"Direction: {0}\nStation: {1}\nTime: {2}\nFarther Trains:\n", 
					dir, 
					train.stop_name,
					train.GetTimeInMinutes ()
				);
			} else {
				RouteView.Text = "Json error!!";
			}
			var fartherTrains = Intent.GetStringExtra ("fartherTrains");
			if (fartherTrains != null) {
				System.Console.WriteLine ("has Farther Trains");
				var trains = Newtonsoft.Json.JsonConvert.DeserializeObject <List<Train>> (fartherTrains);
				foreach (var fartherTrain in trains) {
					RouteView.Text += String.Format ("{0}\n", fartherTrain.GetTimeInMinutes ());
				}
			} else {
				System.Console.WriteLine ("has not Farther Trains");

			}
		}
	}
}

