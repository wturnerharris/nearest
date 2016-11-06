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

		public string TimeString()
		{
			return TimeInMinutesString(ts);
		}

		public double Time()
		{
			return TimeInMinutes(ts);
		}

		/**
		 * Return formatted time string
		 */
		public static string TimeInMinutesString(long unixTime)
		{
			var minutes = TimeInMinutes(unixTime);
			var min = Math.Floor(minutes);
			var timeStr = (min < 1 ? "< 1 Min" : min + " Mins");
			return timeStr;
		}

		/**
		 * Return time in minutes
		 */
		public static double TimeInMinutes(long unixTime)
		{
			var unixDate = FromUnixTime(unixTime);
			var timeSpan = (unixDate.TimeOfDay - DateTime.UtcNow.TimeOfDay);
			return timeSpan.TotalMinutes;
		}

		/**
		 * Convert from unix time to DateTime
		 */
		public static DateTime FromUnixTime(long unixTime)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return epoch.AddSeconds(unixTime);
		}

		public static int ConvertToUnixTimestamp(DateTime date)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			TimeSpan diff = date.ToUniversalTime() - epoch;
			return (int)Math.Floor(diff.TotalSeconds);
		}
	}
}

