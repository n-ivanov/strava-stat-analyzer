using System;
using System.Collections.Generic;
using System.Extensions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ExtendedStravaClient;
using ExtendedStravaClient.Extensions;
using StravaStatisticsAnalyzer.Web.Models;

namespace StravaStatisticsAnalyzer.Web
{
    public class ContextDBFacade : IDBFacade
    {
        private HashSet<long> existingSegments = new HashSet<long>();
        public RazorPagesActivityContext ActivityContext {get;set;}
        public RazorPagesSegmentEffortContext SegmentEffortContext {get;set;}

        public RazorPagesSegmentContext SegmentContext {get;set;}
        public IServiceProvider ServiceProvider {get;set;}

        public bool Initialize()
        {
            ActivityContext = (RazorPagesActivityContext)ServiceProvider.GetService(typeof(RazorPagesActivityContext));
            SegmentEffortContext = (RazorPagesSegmentEffortContext)ServiceProvider.GetService(typeof(RazorPagesSegmentEffortContext));
            SegmentContext = (RazorPagesSegmentContext)ServiceProvider.GetService(typeof(RazorPagesSegmentContext));
            
            foreach(var segment in SegmentContext.Segment)
            {
                existingSegments.Add(segment.ID);
            }
            return ActivityContext != null && SegmentContext != null && SegmentEffortContext != null;
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
                    ActivityContext.Activity.Add(activity.ToModel());
                }
                ActivityContext.SaveChanges();
                return true;
            }
            return false;
        }

        public bool Insert(List<ExtendedStravaClient.SegmentEffort> segmentEfforts)
        {
            if(SegmentEffortContext != null)
            {
                foreach(var segmentEffort in segmentEfforts)
                {
                    SegmentEffortContext.SegmentEffort.Add(segmentEffort.ToModel());
                }
                SegmentEffortContext.SaveChanges();
                return true;
            }
            return false;
        }

        public bool Insert(List<ExtendedStravaClient.Segment> segments)
        {
            if(SegmentContext != null)
            {
                foreach(var segment in segments)
                {
                    if(!existingSegments.Contains(segment.Id))
                    {
                        SegmentContext.Segment.Add(segment.ToModel());
                        existingSegments.Add(segment.Id);
                    }
                }
                SegmentContext.SaveChanges();
                return true;
            }
            return false;
        }

        public bool Insert(ExtendedStravaClient.Activity activity)
        {
            if(ActivityContext != null)
            {
                ActivityContext.Activity.Add(activity.ToModel());
                ActivityContext.SaveChanges();
                return true;
            }
            return false;
        }

        public bool Insert(ExtendedStravaClient.SegmentEffort segmentEffort)
        {
            if(SegmentEffortContext != null)
            {
                SegmentEffortContext.SegmentEffort.Add(segmentEffort.ToModel());
                SegmentEffortContext.SaveChanges();
                return true;
            }
            return false;
        }

        public bool Insert(ExtendedStravaClient.Segment segment)
        {
            if(SegmentContext != null)
            {
                SegmentContext.Segment.Add(segment.ToModel());
                SegmentContext.SaveChanges();
                return true;
            }
            return false;
        }

        public int GetLastUpdate()
        {
            if(ActivityContext == null)
            {
                return -1;
            }
            var lastInsertedActivity = ActivityContext.Activity?.OrderByDescending(a => a.DateTime).FirstOrDefault<Models.Activity>();
            if(lastInsertedActivity == null)
            {
                return -1;
            }
            return lastInsertedActivity.DateTime.ToEpoch();
        }

        public Dictionary<string,List<IRideEffort>> GetSegmentEffortsForActivity(string activityName, int? maxInterval)
        {
            var ids = GetSegmentIdsForActivity(activityName);
            if(ids == null || ids.Count == 0 || SegmentContext == null)
            {
                return null;
            }

            var segmentNamesById = SegmentContext.Segment.Where(s => ids.Contains(s.ID)).ToDictionary(s => s.ID, s=> s.Name);
            var res = new Dictionary<string,List<IRideEffort>>();
            foreach(var id in ids)
            {
                var segmentEfforts = GetSegmentEffortsForSegment(id, maxInterval);
                if(segmentEfforts.Count > 0)
                {
                    res.Add(segmentNamesById[id], segmentEfforts);
                }
                else
                {
                    Console.WriteLine($"Failed to get segment efforts for {segmentNamesById[id]}");
                }
            }
            return res;
        }

        public Dictionary<string,List<IRideEffort>> GetSegmentEffortsForActivity(string activityName, DateTime? start, DateTime? end)
        {
            var ids = GetSegmentIdsForActivity(activityName);
            if(ids == null || ids.Count == 0 || SegmentContext == null)
            {
                return null;
            }

            var segmentNamesById = SegmentContext.Segment.Where(s => ids.Contains(s.ID)).ToDictionary(s => s.ID, s=> s.Name);
            var res = new Dictionary<string,List<IRideEffort>>();
            foreach(var id in ids)
            {
                var segmentEfforts = GetSegmentEffortsForSegment(id, start, end);
                if(segmentEfforts.Count > 0)
                {
                    res.Add(segmentNamesById[id], segmentEfforts);
                }
                else
                {
                    Console.WriteLine($"Failed to get segment efforts for {segmentNamesById[id]}");
                }
            }
            return res;
        }

        public List<long> GetSegmentIdsForActivity(string activityName)
        {
            var activityId = ActivityContext.Activity.Where(a => a.Name == activityName).FirstOrDefault()?.ID;
            var segmentsWithCount = SegmentEffortContext.SegmentEffort.Where(s => s.ActivityID == activityId)
                .GroupBy(s => s.SegmentID).OrderBy(g => g.Count())
                .Select(g => new Tuple<long,long>(g.Key, g.Count())).ToList();
            var averageCount = segmentsWithCount.Select(i => i.Item2).Average();
            return segmentsWithCount.Where(i => i.Item2 >= averageCount).Select(i => i.Item1).ToList();
        }

        public List<IRideEffort> GetActivities(string activityName, int? maxInterval)
        {
            if(ActivityContext != null)
            {
                var activities = ActivityContext.Activity.Where(a => a.Name == activityName).OrderByDescending(a => a.DateTime).ToList();
                if(maxInterval.HasValue && maxInterval < activities.Count)
                {
                    activities = activities.GetRange(0, maxInterval.Value);
                }
                return activities.Select(a => (IRideEffort)new RideEffort(a.ID, a.AvgSpeed, a.MovingTime, a.DateTime)).ToList();
            }
            return new List<IRideEffort>();
        }

        public List<IRideEffort> GetActivities(string activityName, DateTime? start, DateTime? end)
        {
            if(ActivityContext != null)
            {
                var activities = ActivityContext.Activity.Where(a => a.Name == activityName && a.DateTime >= start && a.DateTime <= end);
                return activities.Select(a => (IRideEffort)new RideEffort(a.ID, a.AvgSpeed, a.MovingTime, a.DateTime)).ToList();
            }
            return new List<IRideEffort>();
        }

        public bool Update(ExtendedStravaClient.Activity activity)
        {
            return false;
        }

        private List<IRideEffort> GetSegmentEffortsForSegment(long segmentId, int? maxInterval)
        {
            if(SegmentEffortContext != null)
            {
                var segmentEfforts = SegmentEffortContext.SegmentEffort.Where(a => a.ID == segmentId).OrderByDescending(a => a.DateTime).ToList();
                if(maxInterval.HasValue && maxInterval < segmentEfforts.Count)
                {
                    segmentEfforts = segmentEfforts.GetRange(0, maxInterval.Value);
                }
                return segmentEfforts.Select(a => (IRideEffort)new RideEffort(a.ID, a.AvgSpeed, a.MovingTime, a.DateTime)).ToList();
            }
            return new List<IRideEffort>();
        }

        private List<IRideEffort> GetSegmentEffortsForSegment(long segmentId, DateTime? start, DateTime? end)
        {
            if(SegmentEffortContext != null)
            {
                var segmentEfforts = SegmentEffortContext.SegmentEffort.Where(a => a.ID == segmentId && a.DateTime >= start && a.DateTime <= end);
                return segmentEfforts.Select(a => (IRideEffort)new RideEffort(a.ID, a.AvgSpeed, a.MovingTime, a.DateTime)).ToList();
            }
            return new List<IRideEffort>();
        }
    }   
}