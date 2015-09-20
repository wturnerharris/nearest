using System;

namespace Nearest.Models
{
	public class Train
	{
		public Train ()
		{
			
		}

		/**
		 * Train Route ID / Label
		 */
		public string Name { get; set; }

		/**
		 * Arrival Time in Epoch
		 */
		public string Time { get; set; }

		/**
		 * Stop or Station
		 */
		public string Stop { get; set; }

		/**
		 * Arrival time formatted
		 */
		public string Arrival { get; set; }

		/**
		 * Last Stop and/or Headsign
		 */
		public string Destination { get; set; }
	}
}

