using System;
using System.Collections.Generic;

using Nearest.Models;
using SQLite.Net;
using ISQLitePlatform = SQLite.Net.Interop.ISQLitePlatform;

//using ProtoBuf;
//using transit_realtime;
//using System.Globalization;
using System.Linq;

namespace Nearest
{
	public class Nearest
	{
		public const string DB_NAME = "nearest.db";

		public string service_id;
		public float latitude, longitude;
		public SQLiteConnection db;
		public ISQLitePlatform platform;
		public string dbPath;
		IUtility utility;

		public Nearest(ISQLitePlatform Platform, IUtility Utility)
		{
			platform = Platform;
			utility = Utility;
			try
			{
				dbPath = utility.CopyDatabaseFromAssets(DB_NAME);
				//222d8a42e097fa050a3509a95aab41d1d69a9297
				utility.WriteLine("DBPATH: "+dbPath);

				//File does not seem to exist
				if (!utility.FileExists(dbPath))
				{
					//Copy zip from assets
					//utility.CopyFromAssets(dbPath);
					//if (!utility.FileExists(dbPath))
					//{
						utility.WriteLine("File copy failed.");
					//}
				}
				db = new SQLiteConnection(platform, dbPath);
				db.CreateTable<Metro.calendar>();
				db.CreateTable<Metro.stops>();
				db.CreateTable<Metro.stop_times>();
				db.CreateTable<Metro.trips>();

				//final sanity checks
				if (!TablesExist() || null == GetServiceId())
				{
					utility.WriteLine("Tables do not exist or service_id is null.");
				}
			}
			catch (SQLiteException Ex)
			{
				utility.WriteLine(Ex.Message);
			}
			/*
			.NET
			Install-Package GtfsRealtimeBindings

			WebRequest req = HttpWebRequest.Create("URL OF YOUR GTFS-REALTIME SOURCE GOES HERE");
			FeedMessage feed = Serializer.Deserialize<FeedMessage>(req.GetResponse().GetResponseStream());
			foreach (FeedEntity entity in feed.entity) {}
			*/

		}

		bool DatabaseConnected()
		{
			return dbPath != null && db != null;
		}

		bool TablesExist()
		{
			if (!DatabaseConnected())
			{
				return false;
			}
			string[] AppTables = { "calendar", "stops", "stop_times", "trips" };

			// Get table info to verify existence of tables
			for (var i = 0; i < AppTables.Length; i++)
			{
				var TableInfo = db.GetTableInfo(AppTables[i]);
				if (TableInfo.Count < 1)
				{
					return false;
				}
			}
			return true;
		}

		public string GetServiceId()
		{
			if (service_id != null)
			{
				return service_id;
			}
			try
			{
				// This is the default query for checking
				var sql = GetServiceCalendarQuery();

				// Run the query to obtain the service_id
				var calendar = db.Query<Metro.calendar>(sql);
				if (calendar.Count > 0)
				{
					service_id = calendar[0].service_id;
					return service_id;
				}

				// Alternate way to query to obtain the service_id
				var command = db.CreateCommand(sql);
				command.CommandText = sql;
				var dbQuery = command.ExecuteQuery<Metro.calendar>();
				if (dbQuery.Count > 0)
				{
					service_id = calendar[0].service_id;
					return service_id;
				}
			}
			catch (Exception e)
			{
				utility.WriteLine(e.Message);
			}

			return service_id;
		}

		string GetServiceCalendarQuery()
		{
			var WeekDay = DateTime.Today.DayOfWeek;
			return string.Format("SELECT service_id FROM calendar WHERE {0} = 1", WeekDay);
		}

		public List<Stop> GetNearestStopsAll(double lat, double lon, int dir = 0)
		{
			var sql = "SELECT stop_id, stop_lat, stop_lon FROM stops WHERE location_type = 1;";
			var results = db.Query<stop_data>(sql);
			var locations = new List<stop_data>();

			foreach (stop_data stop in results)
			{
				locations.Add(new stop_data
				{
					stop_id = stop.stop_id,
					stop_lat = stop.stop_lat,
					stop_lon = stop.stop_lon
				});
			}

			return GetNearestTrains(locations, lat, lon, dir);
		}

		List<Stop> GetNearestTrains(List<stop_data> locations, double lat, double lon, int dir = 0)
		{
			var direction = dir > 0 ? "N" : "S";
			var threshold = 0.6; // hardcoded (in miles)
			var distances = new Dictionary<string, double>();
			foreach (var location in locations)
			{
				var distance = GetDistance(lat, lon, location.stop_lat, location.stop_lon);
				if (distance.CompareTo(threshold) <= 0)
				{
					distances.Add(location.stop_id, distance);
				}
			}

			// compare the distances in the array
			var distancesList = distances.ToList();
			distancesList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

			var trains = new List<Stop>();
			foreach (var distance in distancesList)
			{
				var times = GetTrainsByStopId(distance.Key, direction);
				Train next_train = times.GetRange(0, 1)[0];
				times.RemoveAt(0);
				trains.Add(new Stop
				{
					stop_id = distance.Key,
					distance = distance.Value.ToString(),
					trains = times,
					next_train = next_train,
					direction = direction
				});
			}

			return trains;
		}

		List<Train> GetTrainsByStopId(string stop_id, string cardinality = "N", int limit = 8)
		{
			stop_id += cardinality;
			var time_compare = "TIME('now', 'localtime', '+{0} minutes')";

			var sql = string.Format(@"
				SELECT stop_times.arrival_time, trips.route_id, trips.trip_headsign, stops.stop_name,
				ifnull(
					strftime('%s', strftime('%Y-%m-%d', 'now')||' '||stop_times.arrival_time, 'utc'),
					strftime('%s', strftime('%Y-%m-%d', 'now')||' '||
						substr('0000'||(substr(stop_times.arrival_time, 0, 3)-24), -2, 2)||
						substr(stop_times.arrival_time,3,6), 'utc')
				) AS ts
				FROM stop_times, trips, stops WHERE TIME(ts, 'unixepoch', 'localtime') >= {0}
				AND TIME(ts, 'unixepoch', 'localtime') <= {1}
				AND stop_times.stop_id = '{2}'
				AND trips.service_id IN ( {3} )
				AND stop_times.stop_id = stops.stop_id
				AND stop_times.trip_id = trips.trip_id 
				ORDER BY ts ASC LIMIT {4};",
				string.Format(time_compare, 3),
				string.Format(time_compare, 25),
				stop_id,
				GetServiceCalendarQuery(),
				limit
			);
			var results = db.Query<Train>(sql);
			return results;
		}

		public double GetDistance(double lat1, double lon1, double lat2, double lon2, string unit = "M")
		{
			var theta = lon1 - lon2;
			var dist = Math.Sin(Deg2Rad(lat1)) * Math.Sin(Deg2Rad(lat2));
			dist += (Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) * Math.Cos(Deg2Rad(theta)));
			dist = Math.Acos(dist);
			dist = Rad2Deg(dist);
			var factor = 1.0;
			var miles = dist * 60 * 1.1515;

			switch (unit.ToUpper())
			{
				case "K":
					factor = 1.609344;
					break;
				case "N":
					factor = 0.8684;
					break;
				case "M":
					factor = 1.0;
					break;
			}
			return miles * factor;
		}

		public double Deg2Rad(double degrees)
		{
			return (Math.PI / 180) * degrees;
		}

		public double Rad2Deg(double radians)
		{
			return (180 / Math.PI) * radians;
		}

		public class stop_data
		{
			public string stop_id { get; set; }

			public double stop_lat { get; set; }

			public double stop_lon { get; set; }

			public string distance { get; set; }
		}
	}
}

