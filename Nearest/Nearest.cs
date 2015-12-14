using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Nearest.Models;
using Newtonsoft.Json.Schema;

using System.IO;
using SQLite;
using SQLite.Net;

//using ProtoBuf;
//using transit_realtime;
using System.Globalization;

namespace Nearest
{
	public class Nearest
	{
		public string ServiceCalendar;
		public float latitude, longitude;
		public SQLiteConnection db;
		public SQLiteCommand command;
		public string info;

		public Nearest (SQLite.Net.Interop.ISQLitePlatform platform, string path)
		{
			ServiceCalendar = GetServiceCalendar ();

			string dbPath = path;
			try {
				using (var conn = new SQLiteConnection (platform, dbPath)) {
					// Do stuff here...
					var sql = "select * from calendar";
					var query = conn.Query<MetroData.calendar> (sql);
					var data = query.ToArray ();
					//command = new SQLiteCommand (db);
					//command.CommandText = sql;
					//var r = command.ExecuteQuery<MetroData.calendar> ();
					info = data.ToString ();
				}
				//db = new SQLiteConnection (dbPath);
			} catch (SQLiteException Ex) {
				info = Ex.Message;
			}
			//check if tables exist
			//#if tables not exist
			//download zip
			//http://web.mta.info/developers/data/nyct/subway/google_transit.zip
			//decompress zip
			//insert specified csv tables to sqlite db
			//#if
			/*
			date_default_timezone_set('America/New_York');
			$this->mysql_tz = $mysqli->query("SET time_zone = 'America/New_York'");

			.NET
			Install-Package GtfsRealtimeBindings

			WebRequest req = HttpWebRequest.Create("URL OF YOUR GTFS-REALTIME SOURCE GOES HERE");
			FeedMessage feed = Serializer.Deserialize<FeedMessage>(req.GetResponse().GetResponseStream());
			foreach (FeedEntity entity in feed.entity) {}
			*/

		}

		private List<Stop> GetNearestStops (int dir = 0, int limit = 10, string unit = "M")
		{
			var lat = latitude;
			var lon = longitude;
			var _dir = dir > 0 ? "N" : "S";
			var distance_threshold = 0.6;
			double factor;

			switch (unit.ToUpper ()) {
			case "K":
				factor = 1.609344;
				break;
			case "N":
				factor = 0.8684;
				break;
			default:
				factor = 1;
				break;
			}
			var sql = String.Format (@"
				SELECT stop_id, stop_lat, stop_lon, 
				(
					DEGREES(
						ACOS(
							( SIN( {0} ) * SIN( RADIANS(stop_lat) ) ) + (
								COS( {0} ) * COS( RADIANS(stop_lat) ) * cos( RADIANS({1} - stop_lon) )
							)
						)
					) * 60 * 1.1515 * {2} )
				) AS distance FROM stops 
				WHERE location_type = 1 
				ORDER BY distance ASC LIMIT 5;", lat, lon, factor);

			var results = db.Query<MetroData.StopData> (sql);
			var stops = new List<Stop> ();
			foreach (MetroData.StopData stop in results) {
				if (double.Parse (stop.distance) > distance_threshold)
					continue;
				var stop_id = stop.stop_id;
				var times = GetTrainsByStopId (stop_id, _dir);
				var i = 0;
				foreach (Train time in times) {
					DateTime arrival = Convert.ToDateTime (time.arrival_time);
					DateTime origin = new DateTime (1970, 1, 1, 0, 0, 0, 0);
					TimeSpan diff = arrival.ToUniversalTime () - origin;
					times [i].ts = (int)diff.TotalSeconds;
					i++;
				}

				Train next_train = times.GetRange (0, 1) [0];
				times.RemoveAt (0);
				stops.Add (new Stop {
					stop_id = (string)stop_id,
					distance = stop.distance,
					next_train = next_train,
					direction = _dir,
					trains = times
				});
			}
			return stops;
		}

		private List<Train> GetTrainsByStopId (string stop_id, string cardinality = "N", int limit = 7)
		{
			stop_id += cardinality;
			var time_compare = "CURTIME() + INTERVAL {0} MINUTE";

			var sql = String.Format (@"
				SELECT stop_times.arrival_time, trips.route_id, trips.trip_headsign, stops.stop_name
				FROM stop_times, trips, stops WHERE arrival_time >= {0}
				AND arrival_time <= {1}
				AND stop_times.stop_id = '{2}'
				AND trips.service_id = ANY ( {3} )
				AND stop_times.stop_id = stops.stop_id
				AND stop_times.trip_id = trips.trip_id 
				ORDER BY arrival_time ASC LIMIT {4};", 
				          String.Format (time_compare, 3), 
				          String.Format (time_compare, 25),
				          stop_id, 
				          ServiceCalendar, 
				          limit
			          );
			var results = db.Query<Train> (sql);
			return results;
		}

		private string GetServiceCalendar ()
		{
			var DayOfWeek = DateTime.Today.DayOfWeek;
			return String.Format ("SELECT service_id FROM calendar WHERE {0} = 1", 
				DayOfWeek);
		}

		public double ConvertToRadians (double angle)
		{
			return (Math.PI / 180) * angle;
		}
	}
}

