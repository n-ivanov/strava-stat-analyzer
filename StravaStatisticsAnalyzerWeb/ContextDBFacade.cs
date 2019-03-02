using System;
using System.Collections.Generic;
using System.Extensions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExtendedStravaClient;
using StravaStatisticsAnalyzer.Web.Models;

namespace StravaStatisticsAnalyzer.Web
{
    public class ContextDBFacade : IDBFacade
    {
        public RazorPagesActivityContext ActivityContext {get;set;}

        public bool Initialize()
        {
            return true;
        }

        public void Shutdown()
        {

        }

        public bool Insert(List<ExtendedStravaClient.Activity> activities)
        {
            if(ActivityContext != null)
            {
                foreach(var activity in activities)
                {
                    ActivityContext.Activity.Add(ConvertClientActivityToModelActivity(activity));
                }
                ActivityContext.SaveChanges();
                return true;
            }
            return false;
        }

        public bool Insert(List<ExtendedStravaClient.SegmentEffort> segmentEfforts)
        {
            return false;
        }

        public bool Insert(List<ExtendedStravaClient.Segment> segments)
        {
            return false;
        }

        public bool Insert(ExtendedStravaClient.Activity activity)
        {
            if(ActivityContext != null)
            {
                ActivityContext.Activity.Add(ConvertClientActivityToModelActivity(activity));
                ActivityContext.SaveChanges();
                return true;
            }
            return false;
        }

        public bool Insert(ExtendedStravaClient.SegmentEffort segmentEffort)
        {
            return false;
        }

        public bool Insert(ExtendedStravaClient.Segment segment)
        {
            return false;
        }

        public int GetLastUpdate()
        {
            if(ActivityContext == null)
            {
                return -1;
            }
            var lastInsertedActivity = ActivityContext.Activity.OrderByDescending(a => a.DateTime).FirstOrDefault<Models.Activity>();
            if(lastInsertedActivity == null)
            {
                return -1;
            }
            return lastInsertedActivity.DateTime.ToEpoch();
        }

        public Dictionary<string,List<IRideEffort>> GetSegmentEffortsForActivity(string activityName, int? maxInterval)
        {
            return null;
        }

        public List<long> GetSegmentIdsForActivity(string activityName)
        {
            return null;
        }

        public List<IRideEffort> GetActivities(string activityName, int? maxInterval)
        {
            return null;            
        }

        public bool Update(ExtendedStravaClient.Activity activity)
        {
            return false;
        }

        private Models.Activity ConvertClientActivityToModelActivity(ExtendedStravaClient.Activity activity)
        {
            return new Models.Activity()
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
    }
}