using System;
using System.Collections.Generic;

namespace Nearest.Models
{
	public class Stop
	{

		public string stop_id { get; set; }

		public string distance { get; set; }

		public Train next_train { get; set; }

		public List<Train> trains { get; set; }

		public string direction { get; set; }

		public EventHandler clickHandler { get; set; }
	}
}

