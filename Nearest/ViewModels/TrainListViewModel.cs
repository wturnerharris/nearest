using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Net.Http;

using Nearest.Models;

namespace Nearest.ViewModels
{
	public class TrainListViewModel : INotifyPropertyChanged
	{
		String latitude;
		String longitude;
		public String requestString;

		public ObservableCollection<List<Stop>> stopList { get; set; }

		public TrainListViewModel (Double lat, Double lon)
		{
			latitude = lat.ToString ();
			longitude = lon.ToString ();
			stopList = new ObservableCollection<List<Stop>> ();
			stopList.Add (new List<Stop> ()); // down
			stopList.Add (new List<Stop> ()); // up
		}


		private bool busy = false;

		public bool IsBusy {
			get { return busy; }
			set {
				if (busy == value)
					return;

				busy = value;
				OnPropertyChanged ("IsBusy");
			}
		}

		public async Task GetTrainsAsync ()
		{
			try {
				IsBusy = true;

				var client = new HttpClient ();

				requestString = "http://turnerharris.com/nearest/" +
				"next.php?action=getTrains&lat=" + latitude + "&lon=" + longitude;

				for (int i = 0; i < 2; i++) {
					var json = await client.GetStringAsync (requestString + "&dir=" + i.ToString ());
					var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Stop>> (json);

					stopList [i].Clear (); // empty current list data
					foreach (var item in items) {
						stopList [i].Add (item);
					}
				}
			} finally {
				IsBusy = false;
			}
		}


		#region INotifyPropertyChanged implementation

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion


		public void OnPropertyChanged (string name)
		{
			var changed = PropertyChanged;
			if (changed == null)
				return;

			changed (this, new PropertyChangedEventArgs (name));


		}
	}
}

