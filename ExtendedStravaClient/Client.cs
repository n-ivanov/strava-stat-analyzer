using System;
using System.Extensions;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExtendedStravaClient.Weather;

namespace ExtendedStravaClient
{
    public class Client
    {
        StravaFacade stravaFacade_;
        WeatherClient weatherClient_;
        IDBFacade dbFacade_;
        private static List<long> insertedIds_;

        public Client(IDBFacade facade)
        {
            stravaFacade_ = new StravaFacade();
            dbFacade_ = facade;
            weatherClient_ = new WeatherClient();
        }

        public bool Initialize(string stravaAccessToken, string weatherAccessKey = null)
        {
            stravaFacade_.Initialize(stravaAccessToken);
            weatherClient_.Initialize(weatherAccessKey);
            return dbFacade_.Initialize();
        }

        public async Task GetAndSaveNewActivities(bool getDetailedActivityInformation = true, bool addWeatherInformation = true)
        {
            var lastUpdate = dbFacade_.GetLastUpdate();
            if(lastUpdate == -1)
            {
                lastUpdate = 1534982400;
            }
            var activities = await stravaFacade_.GetAllActivities(null, lastUpdate);
            await InsertActivities(activities, getDetailedActivityInformation, addWeatherInformation);
        }

        public async Task GetAndSaveActivities(DateTime? startInterval, DateTime? endInterval, bool getDetailedActivityInformation = true)
        {
            var activities = await stravaFacade_.GetAllActivities(startInterval.ToEpoch(), endInterval.ToEpoch());
            await InsertActivities(activities, getDetailedActivityInformation);
        }

        public async Task GetAndSaveDetailedActivityInformation(List<long> activityIds = null, bool addWeatherInformation = true)
        {
            if(activityIds == null)
            {
                if(insertedIds_ == null)
                {
                    return;
                }
                Console.WriteLine($" Retrieved saved {insertedIds_.Count} activities for which detailed information has not been fetched.");
                activityIds = insertedIds_;
            }
            foreach(var activityId in activityIds)
            {
                var detailedActivity = await stravaFacade_.GetDetailedActivity(activityId);
                if(detailedActivity == null)
                {
                    Console.WriteLine("Unable to fetch detailed activity. Aborting insertions...");
                    break;
                }
                if(addWeatherInformation)
                {
                    await AddWeatherInformation(detailedActivity);
                }
                Console.WriteLine($"Saving detailed information for activity {detailedActivity.Id}...");
                try
                {
                    dbFacade_.Insert(detailedActivity.Segment_Efforts);
                    dbFacade_.Insert(detailedActivity.Segment_Efforts.Select(e => e.Segment).ToList());
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Unable to save information for activity {detailedActivity.Id}.");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    if(ex.InnerException != null)
                    {
                        Console.WriteLine(ex.InnerException.Message);
                        Console.WriteLine(ex.InnerException.StackTrace);
                    }
                }
            }
        }

        public async Task ReloadActivities(bool addWeather = false)
        {
            var activitIds = dbFacade_.ActivityIds;
            foreach(var activityId in activitIds)
            {
                await ReloadActivity(activityId, addWeather);
            }
            Console.WriteLine($"Successfully reloaded {activitIds.Count} activities.");
        }

        public async Task ReloadActivity(long activityId, bool addWeather = false)
        {
            var detailedActivity = await stravaFacade_.GetDetailedActivity(activityId, false);
            if(detailedActivity == null)
            {
                Console.WriteLine("Unable to fetch detailed activity. Aborting reload...");
                return;
            }
            if(addWeather)
            {
                await AddWeatherInformation(detailedActivity);
            }
            if(!dbFacade_.Update(detailedActivity))
            {
                Console.WriteLine($"Failed to reload and update {detailedActivity.Name} on {detailedActivity.DateTime}.");
                return;
            }
            Console.WriteLine($"Successfully reloaded and updated {detailedActivity.Name} ({detailedActivity.Id}) on {detailedActivity.DateTime}.");
        }

        public async Task ModifyActivity(long activityId, bool? commute, string description, string name)
        {
            var modifiedActivity = await stravaFacade_.ModifyActivity(activityId, commute, description, name);
            if(modifiedActivity == null)
            {
                Console.WriteLine("Unable to fetch modified activity. Aborting update...");
                return;
            }
            dbFacade_.Update(modifiedActivity);
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

        private async Task InsertActivities(List<Activity> activities, bool getDetailedActivityInformation = true, bool addWeatherInformation = true)
        {
            dbFacade_.Insert(activities);
            if(getDetailedActivityInformation)
            {
                await GetAndSaveDetailedActivityInformation(activities.Select(s => s.Id).ToList(), addWeatherInformation);
            }
            else
            {
                insertedIds_ = activities.Select(s => s.Id).ToList();
                Console.WriteLine($"Saved {insertedIds_.Count} activities for which detailed information has not been fetched.");
            }
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


        private async Task AddWeatherInformation(Activity detailedActivity)
        {
            var weatherDescription =  await weatherClient_.GetWeatherDescription(detailedActivity.Start_Latitude, 
                detailedActivity.Start_Longitude, detailedActivity.DateTime.ToEpoch() + detailedActivity.Elapsed_Time / 2, "ca");
            var existingDescription  = detailedActivity.Description;
            var idx = existingDescription?.IndexOf($"--") ?? -1;
            detailedActivity.Description = $"{(idx == -1 ? existingDescription : existingDescription.Substring(0,idx-1))}{Environment.NewLine}--{Environment.NewLine}{weatherDescription}";

            var updatedActivity = await stravaFacade_.ModifyActivity(detailedActivity.Id, null, detailedActivity.Description);
            if(updatedActivity == null)
            {
                Console.WriteLine($"Unable to update {detailedActivity.Name} ({detailedActivity.Id}) in Strava.");
                return;
            }
            if(!dbFacade_.Update(detailedActivity))
            {
                Console.WriteLine($"Unable to update {detailedActivity.Name} ({detailedActivity.Id}) in the DB.");
                return;
            }
        }

        
    }
}