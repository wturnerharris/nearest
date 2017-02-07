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
		string latitude;
		string longitude;
		public string requestString;

		public ObservableCollection<List<Stop>> stopList { get; set; }

		public TrainListViewModel(double lat, double lon)
		{
			SetLocation(lat, lon);
			stopList = new ObservableCollection<List<Stop>>();
			stopList.Add(new List<Stop>()); // down
			stopList.Add(new List<Stop>()); // up
		}

		public void SetLocation(double lat, double lon)
		{
			latitude = lat.ToString();
			longitude = lon.ToString();
		}

		bool busy;

		public string info;

		public bool IsBusy
		{
			get { return busy; }
			set
			{
				if (busy == value)
					return;

				busy = value;
				OnPropertyChanged("IsBusy");
			}
		}

		void Update(int i, List<Stop> stops)
		{
			// empty current list data
			stopList[i].Clear();

			// overwrite stop list
			foreach (var stop in stops)
			{
				stopList[i].Add(stop);
			}
		}

		public void GetTrains(Nearest NearestApp)
		{
			try
			{
				IsBusy = true;

				var i = 0;
				foreach (var directionList in stopList)
				{
					List<Stop> stops = NearestApp.GetNearestStopsAll(
						double.Parse(latitude), double.Parse(longitude), i
					);
					Update(i, stops);
					i++;
				}
			}
			finally
			{
				IsBusy = false;
			}

		}

		public async Task GetTrainsAsync()
		{
			try
			{
				IsBusy = true;

				var client = new HttpClient();
				var distance = 0.6;
				var time = 3;
				var showDupes = (true ? "1" : "0");

				requestString = string.Format(
					"http://{0}?action=getTrains&lat={1}&lon={2}&d={3}&t={4}&x={5}",
					"turnerharris.com/nearest/next.php",
					latitude,
					longitude,
					distance,
					time,
					showDupes
				);

				for (int i = 0; i < 2; i++)
				{
					var json = await client.GetStringAsync(requestString + "&dir=" + i);
					var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Stop>>(json);

					Update(i, items);
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
