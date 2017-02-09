/* This file was generated by Settings Studio
 *
 * Copyright © 2015 Colby Williams. All Rights Reserved.
 */

using Nearest;

namespace SettingsStudio
{

	public static partial class Settings
	{

		#region Visible Settings


		public static string VersionNumber
		{
			get { return StringForKey(SettingsKeys.VersionNumber); }
			set { SetSetting(SettingsKeys.VersionNumber, value); }
		}


		public static string BuildNumber
		{
			get { return StringForKey(SettingsKeys.BuildNumber); }
			set { SetSetting(SettingsKeys.BuildNumber, value); }
		}


		public static string GitHash => StringForKey(SettingsKeys.GitCommitHash);


		public static bool UseInternetServices
		{
			get { return BoolForKey(SettingsKeys.UseInternetServices); }
			set { SetSetting(SettingsKeys.UseInternetServices, value); }
		}


		public static bool ShowAllStations
		{
			get { return BoolForKey(SettingsKeys.ShowAllStations); }
			set { SetSetting(SettingsKeys.ShowAllStations, value); }
		}


		public static TimeUnits UomTime
		{
			get { return (TimeUnits)Int32ForKey(SettingsKeys.UomTime, true); }
			set { SetSetting(SettingsKeys.UomTime, (int)value, true); }
		}


		public static TimeThresholdUnits UomTimeThreshold
		{
			get { return (TimeThresholdUnits)IntForKey(SettingsKeys.UomTimeThreshold, 3); }
			set { SetSetting(SettingsKeys.UomTimeThreshold, (int)value, true); }
		}


		public static DistanceUnits UomDistance
		{
			get { return (DistanceUnits)Int32ForKey(SettingsKeys.UomDistance, true); }
			set { SetSetting(SettingsKeys.UomDistance, (int)value, true); }
		}


		public static DistanceThresholdUnits UomDistanceThreshold
		{
			get { return (DistanceThresholdUnits)IntForKey(SettingsKeys.UomDistanceThreshold, 4); }
			set { SetSetting(SettingsKeys.UomDistanceThreshold, (int)value, true); }
		}


		#endregion


		#region Hidden Settings


		public static bool NotApplicable
		{
			get { return BoolForKey(SettingsKeys.NotApplicable); }
			set { SetSetting(SettingsKeys.NotApplicable, value); }
		}


		#endregion
	}
}