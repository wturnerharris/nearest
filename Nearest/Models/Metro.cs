using SQLite.Net.Attributes;

namespace Nearest.Models
{
	public class Metro
	{

		/*
		CREATE TABLE `calendar` (
			`service_id` varchar(255) DEFAULT NULL,
			`monday` int(11) DEFAULT NULL,
			`tuesday` int(11) DEFAULT NULL,
			`wednesday` int(11) DEFAULT NULL,
			`thursday` int(11) DEFAULT NULL,
			`friday` int(11) DEFAULT NULL,
			`saturday` int(11) DEFAULT NULL,
			`sunday` int(11) DEFAULT NULL,
			`start_date` date DEFAULT NULL,
			`end_date` date DEFAULT NULL,
			KEY `service_id` (`service_id`)
		) ENGINE=InnoDB DEFAULT CHARSET=utf8;
		*/
		public class calendar
		{
			[PrimaryKey]
			public string service_id { get; set; }

			[MaxLength(11)]
			public int monday { get; set; }

			[MaxLength(11)]
			public int tuesday { get; set; }

			[MaxLength(11)]
			public int wednesday { get; set; }

			[MaxLength(11)]
			public int thursday { get; set; }

			[MaxLength(11)]
			public int friday { get; set; }

			[MaxLength(11)]
			public int saturday { get; set; }

			[MaxLength(11)]
			public int sunday { get; set; }

			public string start_date { get; set; }

			public string end_date { get; set; }
		}

		/*
		CREATE TABLE `stop_times` (
			`trip_id` varchar(255) DEFAULT NULL,
			`arrival_time` time DEFAULT NULL,
			`departure_time` time DEFAULT NULL,
			`stop_id` varchar(255) DEFAULT NULL,
			`stop_sequence` int(11) DEFAULT NULL,
			`stop_headsign` varchar(255) DEFAULT NULL,
			`pickup_type` int(11) DEFAULT NULL,
			`drop_off_type` int(11) DEFAULT NULL,
			`shape_dist_traveled` varchar(255) DEFAULT NULL,
		KEY `stop_time_index` (`trip_id`,`arrival_time`,`departure_time`,`stop_id`)
		) ENGINE=InnoDB DEFAULT CHARSET=utf8;
		*/
		public class stop_times
		{
			public string trip_id { get; set; }

			public string arrival_time { get; set; }

			public string departure_time { get; set; }

			public string stop_id { get; set; }

			public int stop_sequence { get; set; }
		}

		/*
		CREATE TABLE `stops` (
			`stop_id` varchar(255) DEFAULT NULL,
			`stop_code` varchar(255) DEFAULT NULL,
			`stop_name` varchar(255) DEFAULT NULL,
			`stop_desc` varchar(255) DEFAULT NULL,
			`stop_lat` float(10,6) DEFAULT NULL,
			`stop_lon` float(10,6) DEFAULT NULL,
			`zone_id` varchar(255) DEFAULT NULL,
			`stop_url` varchar(255) DEFAULT NULL,
			`location_type` int(11) DEFAULT NULL,
			`parent_station` varchar(255) DEFAULT NULL,
		KEY `stop_id` (`stop_id`,`stop_name`,`stop_lat`,`stop_lon`)
		) ENGINE=InnoDB DEFAULT CHARSET=utf8;
		*/
		public class stops
		{
			public string stop_id { get; set; }

			public string stop_name { get; set; }

			public double stop_lat { get; set; }

			public double stop_lon { get; set; }

			public int? location_type { get; set; }
		}

		/*
		CREATE TABLE `trips` (
			`route_id` varchar(11) DEFAULT NULL,
			`service_id` varchar(255) DEFAULT NULL,
			`trip_id` varchar(255) DEFAULT NULL,
			`trip_headsign` varchar(255) DEFAULT NULL,
			`direction_id` int(11) DEFAULT NULL,
			`block_id` varchar(255) DEFAULT NULL,
			`shape_id` varchar(255) DEFAULT NULL,
		KEY `route_id` (`route_id`,`service_id`,`trip_id`)
		) ENGINE=InnoDB DEFAULT CHARSET=utf8;
		*/
		public class trips
		{
			public string route_id { get; set; }

			public string service_id { get; set; }

			public string trip_id { get; set; }

			public string trip_headsign { get; set; }

			public int? direction_id { get; set; }
		}

	}
}

