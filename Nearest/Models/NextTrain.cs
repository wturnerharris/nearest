using System;

namespace Nearest.Models
{
	public class NextTrain
	{
		public NextTrain ()
		{
			
		}

		/**
		 * Stop/Station ID
		 */
		public string StopID { get; set; }

		/**
		 * Cardinal direction
		 */
		public string Direction { get; set; }

		/**
		 * Distance from location
		 */
		public string Distance { get; set; }

		/**
		 * Next train
		 */
		public string NextTrains { get; set; }

		/**
		 * Subsequent trains
		 */
		public string FollowingTrains { get; set; }
	}
}

