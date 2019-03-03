using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExtendedStravaClient
{
    public class Client
    {
        Fetcher fetcher_;
        IDBFacade dbFacade_;

        public Client(IDBFacade facade)
        {
            fetcher_ = new Fetcher();
            dbFacade_ = facade;
        }

        public void Initialize(string accessToken)
        {
            fetcher_.Initialize(accessToken);
            dbFacade_.Initialize();
        }

        public async Task GetAndSaveNewActivities(bool getDetailedActivityInformation = true)
        {
            var lastUpdate = dbFacade_.GetLastUpdate();
            if(lastUpdate == -1)
            {
                // lastUpdate = 1534982400;
                lastUpdate = 1551398400;
            }
            var activities = await fetcher_.GetAllActivities(null, lastUpdate);
            dbFacade_.Insert(activities);
            if(getDetailedActivityInformation)
            {
                await GetAndSaveDetailedActivityInformation(activities.Select(s => s.Id).ToList());
            }
        }

        public async Task GetAndSaveDetailedActivityInformation(List<long> activityIds)
        {
            foreach(var activityId in activityIds)
            {
                var detailedActivity = await fetcher_.GetDetailedActivity(activityId);
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

        public Dictionary<string,List<IRideEffortAnalysis>> AnalyzeRide(string rideName, (DateTime? start, DateTime? end)[] intervals)
        {
            var result = new Dictionary<string,List<IRideEffortAnalysis>>();

            //TODO - find a way to make this smarter and avoid fetching duplicates
            foreach(var interval in intervals)
            {
                AddAnalyzedIntervalToResults(rideName, interval.start, interval.end, ref result);
            }

            return result;
        }

        private void AddAnalyzedIntervalToResults(string rideName, DateTime? start, DateTime? end, 
            ref Dictionary<string,List<IRideEffortAnalysis>> results)
        {
            var activities = dbFacade_.GetActivities(rideName, start, end);
            if(!results.ContainsKey(rideName))
            {
                results[rideName] = new List<IRideEffortAnalysis>();                
            }
            results[rideName].Add(new RideEffortAnalysis(rideName, activities));

            var segmentEfforts = dbFacade_.GetSegmentEffortsForActivity(rideName, start, end);
            foreach(var kvp in segmentEfforts)
            {
                if(!results.ContainsKey(kvp.Key))
                {
                    results[kvp.Key] = new List<IRideEffortAnalysis>();
                }
                results[kvp.Key].Add(new RideEffortAnalysis(kvp.Key, kvp.Value));
            }
        }
    }
}