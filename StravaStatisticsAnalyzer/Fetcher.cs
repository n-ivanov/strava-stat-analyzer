using System;
using System.Collections.Generic;
using RestSharp;
using RestSharp.Extensions;

namespace StravaStatisticsAnalyzer
{
    public class Fetcher
    {
        private string accessToken_;
        private RestClient restClient_;
        public Fetcher()
        {
            restClient_ = new RestClient("https://www.strava.com/api/v3");
        }

        public void Initialize(string accessToken)
        {
            accessToken_ = accessToken;
        }

        public List<Activity> GetActivities(int? before = null, int? after = null, int? page = null, int? pageSize = null)
        {
            var request = new RestRequest(Method.GET);
            request.Resource = "/athlete/activities";
            request.AddParameter("access_token", accessToken_);
            request.AddNullableParameter("before", before);
            request.AddNullableParameter("after", after);
            request.AddNullableParameter("page", page);
            request.AddNullableParameter("per_page", pageSize);

            var response = restClient_.Execute<List<Activity>>(request);

            if(!response.IsSuccessful)
            {
                Console.WriteLine($"An error occurred during your request: {response.ErrorMessage} - {response.ErrorException}");
                Console.WriteLine($"({response.StatusCode}) - {response.StatusDescription}");
                return null;
            }
            else 
            {
                Console.WriteLine($"Response status code - {response.StatusCode}");
            }
            return response.Data;
        }

        public List<Activity> GetAllActivities(int? before, int? after)
        {
            List<Activity> activities = new List<Activity>();
            int page = 1;
            int perList = 50;
            List<Activity> partialActivities;
            while((partialActivities = GetActivities(before, after, page++, perList)) != null && partialActivities.Count != 0)
            {
                activities.AddRange(partialActivities);   
                Console.WriteLine($"Added {partialActivities.Count} activities from page {page}"); 
            }
            Console.WriteLine($"Obtained {activities.Count} activities");
            return activities;
        }

    }
}
