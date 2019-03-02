using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExtendedStravaClient
{
    public class Analyzer
    {
        Fetcher fetcher_;
        IDBFacade dbFacade_;

        public Analyzer(IDBFacade facade)
        {
            fetcher_ = new Fetcher();
            dbFacade_ = facade;
        }

        public void Initialize(string accessToken)
        {
            fetcher_.Initialize(accessToken);
            dbFacade_.Initialize();
        }

        public async Task GetAndSaveNewActivities()
        {
            var lastUpdate = dbFacade_.GetLastUpdate();
            if(lastUpdate == -1)
            {
                lastUpdate = 1534982400;
            }
            var activities = await fetcher_.GetAllActivities(null, lastUpdate);
            dbFacade_.Insert(activities);
            foreach(var activity in activities)
            {
                var detailedActivity = await fetcher_.GetDetailedActivity(activity.Id);
                // dbFacade_.Update(activity);
                if(detailedActivity == null)
                {
                    Console.WriteLine("Unable to fetch detailed activity. Aborting insertions...");
                    break;
                }
                dbFacade_.Insert(detailedActivity.Segment_Efforts);
                dbFacade_.Insert(detailedActivity.Segment_Efforts.Select(e => e.Segment).ToList());
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
            var activities = dbFacade_.GetActivities(rideName, maxInterval);
            var results = new Dictionary<string,List<IRideEffortAnalysis>>();
            
            results[rideName] = new List<IRideEffortAnalysis>(); 
            foreach(var interval in normalizedIntervals)
            {
                var activitySubset =  activities.GetRange(0, Math.Min(interval, activities.Count));
                results[rideName].Add(new RideEffortAnalysis(rideName,activitySubset));
            }
            var segmentEfforts = dbFacade_.GetSegmentEffortsForActivity(rideName, maxInterval);

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