using System;
using System.Collections.Generic;

namespace Nearest.Models
{
	public class Stop
	{

		public bool stale = false;

		int TRASH_BEYOND = 3;

		public string stop_id { get; set; }

		public string distance { get; set; }

		public Train next_train { get; set; }

		public List<Train> trains { get; set; }

		public string direction { get; set; }

		public EventHandler clickHandler { get; set; }

		public void shift()
		{
			if (trains.Count < TRASH_BEYOND)
			{
				stale = true;
			}
			else
			{
			}
			next_train = trains.GetRange(0, 1)[0];
			trains.RemoveAt(0);
		}
	}
}

