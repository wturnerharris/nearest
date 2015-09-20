using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.ComponentModel;

using Nearest.Models;
using System.Net.Http;

namespace Nearest.ViewModels
{
	public class TrainListViewModel : INotifyPropertyChanged
	{
		public ObservableCollection<Train> TrainList { get; set; }

		public TrainListViewModel ()
		{
			TrainList = new ObservableCollection<Train> ();
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

				var json = await client.GetStringAsync("http://turnerharris.com/nearest/" +
					"next.php?action=getTrains&lat=40.8248438&lon=-73.95145959999999&dir=0");

				var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Train>>(json);

				foreach(var item in items)
				{
					TrainList.Add(item);
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

