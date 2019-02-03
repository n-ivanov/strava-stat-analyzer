using System;
using System.Linq;
using System.Collections.Generic;

namespace StravaStatisticsAnalyzer
{
    public struct AverageBest<T> where T: struct
    {
        public T Average {get; set;}
        public T Best {get; set;}

        public AverageBest(T average, T best)
        {
            Average = average;
            Best = best;
        }
    }

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

        public Dictionary<int,(AverageBest<double> speed, AverageBest<int> time)> AnalyzeRide(string rideName, int[] intervals)
        {
            int? maxInterval = intervals.Max();
            if(maxInterval == Int32.MaxValue)
            {
                maxInterval = null;
            }
            var activities = dbWriter_.GetActivities(rideName, maxInterval);
            double speedSum = 0;
            double bestSpeed = 0;
            int timeSum = 0;
            int bestTime = Int32.MaxValue;
            int intervalIdx = 0;
            var results = new Dictionary<int,(AverageBest<double> speed, AverageBest<int> time)>();
            for(int i = 0; i < activities.Count; i++)
            {
                var activity = activities[i];
                if(activity.avg_speed > bestSpeed)
                {
                    bestSpeed = activity.avg_speed;
                }
                if(activity.moving_time < bestTime)
                {
                    bestTime = activity.moving_time;
                }
                speedSum += activity.avg_speed;
                timeSum += activity.moving_time;
                if(i == intervals[intervalIdx] - 1)
                {
                    results.Add(intervals[intervalIdx], 
                        (new AverageBest<double>(speedSum/intervals[intervalIdx], bestSpeed), new AverageBest<int>(timeSum/intervals[intervalIdx], bestTime))); 
                    intervalIdx++;
                }
                else if(intervals[intervalIdx] == Int32.MaxValue && i == activities.Count - 1)
                {
                    results.Add(intervals[intervalIdx], 
                        (new AverageBest<double>(speedSum/activities.Count, bestSpeed), new AverageBest<int>(timeSum/activities.Count, bestTime))); 
                }
            }
            return results;
        }
    }
}