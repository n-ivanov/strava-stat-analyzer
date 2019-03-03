using ExtendedStravaClient;
using StravaStatisticsAnalyzer.Web.Models;

namespace ExtendedStravaClient.Extensions
{
    public static class ExtendedStravaClientExtensions
    {
        public static StravaStatisticsAnalyzer.Web.Models.Activity ToModel(this ExtendedStravaClient.Activity activity)
        {
            return new StravaStatisticsAnalyzer.Web.Models.Activity()
            {
                ID = activity.Id,
                Name = activity.Name,
                Distance = activity.Distance,
                MovingTime = activity.Moving_Time,
                ElapsedTime = activity.Elapsed_Time,
                AvgSpeed = activity.Average_Speed,
                MaxSpeed = activity.Max_Speed,
                DateTime = activity.DateTime,
                AthleteID = activity.Athlete.Id,
                TotalElevationGain = activity.Total_Elevation_Gain,
                ElevationHigh = activity.Elev_High,
                ElevationLow = activity.Elev_Low, 
                StartLatitude = activity.Start_Latitude,
                StartLongitude = activity.Start_Longitude,
                EndLatitude = activity.End_Latitude,
                EndLongitude = activity.End_Longitude, 
                Description = activity.Description,
                Commute = activity.Commute                
            };
        }

        public static StravaStatisticsAnalyzer.Web.Models.SegmentEffort ToModel(this ExtendedStravaClient.SegmentEffort segmentEffort)
        {
            return new StravaStatisticsAnalyzer.Web.Models.SegmentEffort()
            {
                ID = segmentEffort.Id,
                Name = segmentEffort.Name,
                Distance = segmentEffort.Distance,
                MovingTime = segmentEffort.Moving_Time,
                ElapsedTime = segmentEffort.Elapsed_Time,
                AvgSpeed = segmentEffort.Distance / segmentEffort.Moving_Time,
                DateTime = segmentEffort.DateTime,
                AthleteID = segmentEffort.Athlete.Id,
                SegmentID = segmentEffort.Segment.Id,
                ActivityID = segmentEffort.Activity.Id,
            };
        }

         public static StravaStatisticsAnalyzer.Web.Models.Segment ToModel(this ExtendedStravaClient.Segment segment)
        {
            return new StravaStatisticsAnalyzer.Web.Models.Segment()
            {
                ID = segment.Id,
                Name = segment.Name,
                Distance = segment.Distance,
                AvgGrade = segment.Average_Grade,
                MaxGrade = segment.Maximum_Grade,
                ElevationHigh = segment.Elevation_High,
                ElevationLow = segment.Elevation_Low, 
                StartLatitude = segment.Start_Latitude,
                StartLongitude = segment.Start_Longitude,
                EndLatitude = segment.End_Latitude,
                EndLongitude = segment.End_Longitude, 
                Starred = segment.Starred                
            };
        }
    }
}