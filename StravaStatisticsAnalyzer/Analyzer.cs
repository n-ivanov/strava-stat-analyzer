using System;
using System.Linq;
using System.Collections.Generic;

namespace StravaStatisticsAnalyzer
{
    public class Analyzer
    {
        Fetcher fetcher_;
        DBWriter dbWriter_;

        public Analyzer()
        {
            fetcher_ = new Fetcher();
            dbWriter_ = new DBWriter();
        }

        public void Initialize(string accessToken)
        {
            fetcher_.Initialize(accessToken);
            dbWriter_.Initialize();
        }

        public void GetAndSaveNewActivities()
        {
            var lastUpdate = dbWriter_.GetLastUpdate();
            if(lastUpdate == -1)
            {
                lastUpdate = 1534982400;
            }
            var activities = fetcher_.GetAllActivities(null, lastUpdate);
            dbWriter_.Insert(activities);
            foreach(var activity in activities)
            {
                var detailedActivity = fetcher_.GetDetailedActivity(activity.Id);
                // dbWriter_.Update(activity);
                if(detailedActivity == null)
                {
                    Console.WriteLine("Unable to fetch detailed activity. Aborting insertions...");
                    break;
                }
                dbWriter_.Insert(detailedActivity.Segment_Efforts);
                dbWriter_.Insert(detailedActivity.Segment_Efforts.Select(e => e.Segment).ToList());
            }
        }

        public Dictionary<string,List<IRideEffortAnalysis>> AnalyzeRide(string rideName, int[] intervals)
        {
            int? maxInterval;
            int[] normalizedIntervals; 
            if(intervals.Count() == 0)
            {
                maxInterval = null;
                normalizedIntervals = new int[] {Int32.MaxValue};
            }
            else
            {
                maxInterval = intervals.Max();
                if(maxInterval == Int32.MaxValue)
                {
                    maxInterval = null;
                }
                normalizedIntervals = intervals;
            }
            var activities = dbWriter_.GetActivities(rideName, maxInterval);
            var results = new Dictionary<string,List<IRideEffortAnalysis>>();
            
            results[rideName] = new List<IRideEffortAnalysis>(); 
            foreach(var interval in normalizedIntervals)
            {
                results[rideName].Add(new RideEffortAnalysis(rideName, activities.GetRange(0, Math.Min(interval, activities.Count))));
            }
            var segmentEfforts = dbWriter_.GetSegmentEffortsForActivity(rideName, maxInterval);

            foreach(var kvp in segmentEfforts)
            {
                results[kvp.Key] = new List<IRideEffortAnalysis>(); 
                foreach(var interval in normalizedIntervals)
                {
                    results[kvp.Key].Add(new RideEffortAnalysis(kvp.Key, kvp.Value.GetRange(0, Math.Min(interval, kvp.Value.Count))));
                }
            }
            return results;
        }
    }
}