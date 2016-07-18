using System;

namespace Nearest.Models
{
	public class Train
	{

		/**
		 * Train Route ID / Label
		 */
		public string route_id { get; set; }

		/**
		 * Arrival Time
		 */
		public string arrival_time { get; set; }

		/**
		 * Stop or Station
		 */
		public string stop_name { get; set; }

		/**
		 * Last Stop and/or Headsign
		 */
		public string trip_headsign { get; set; }

		/**
		 * Arrival Time Epoch
		 */
		public int ts { get; set; }

		public bool ExpiredUnder(int seconds)
		{
			TimeSpan TrainTimeSpan = FromUnixTime(ts).TimeOfDay;
			TimeSpan NowTimeSpan = DateTime.UtcNow.TimeOfDay;
			double TotalSeconds = (TrainTimeSpan - NowTimeSpan).TotalSeconds;
			return (TotalSeconds < seconds);
		}

		public string GetTimeInMinutes()
		{
			return time(ts);
		}

		/**
		 * Return formatted time string
		 */
		public static string time(long unixTime)
		{
			var unixDate = FromUnixTime(unixTime);
			var newDate = (unixDate.TimeOfDay - DateTime.UtcNow.TimeOfDay).TotalMinutes;
			var min = Math.Floor(newDate);
			var timeStr = (min < 1 ? "< 1 Min" : min + " Mins");
			return timeStr;
		}

		/**
		 * Convert from unix time to DateTime
		 */
		public static DateTime FromUnixTime(long unixTime)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return epoch.AddSeconds(unixTime);
		}
	}
}

