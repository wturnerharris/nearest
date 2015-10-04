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
		public ObservableCollection<Stop> stopListUptown { get; set; }
		public ObservableCollection<Stop> stopListDowntown { get; set; }

		public TrainListViewModel (Double lat, Double lon)
		{
			latitude = lat.ToString();
			longitude = lon.ToString();
			stopListUptown = new ObservableCollection<Stop> ();
			stopListDowntown = new ObservableCollection<Stop> ();
		}


		private bool busy = false;

		public bool IsBusy
		{
			get { return busy; }
			set {
				if (busy == value)
					return;

				busy = value;
				OnPropertyChanged ("IsBusy");
			}
		}

		public async Task GetTrainsAsync(){
			try
			{
				IsBusy = true;

				var client = new HttpClient();

				requestString = "http://turnerharris.com/nearest/" +
					"next.php?action=getTrains&lat=" + latitude + "&lon=" + longitude;

				for(int i = 0; i < 2; i++)
				{
					var json = await client.GetStringAsync(requestString + "&dir="+ i.ToString());
					var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Stop>>(json);

					foreach( var item in items){
						if (i==0) {
							stopListDowntown.Add(item);
						} else {
							stopListUptown.Add(item);
						}
					}
				}
			}
			finally 
			{
				IsBusy = false;
			}
		}


		#region INotifyPropertyChanged implementation

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion


		public void OnPropertyChanged(string name)
		{
			var changed = PropertyChanged;
			if (changed == null)
				return;

			changed(this, new PropertyChangedEventArgs(name));


		}
	}
}

