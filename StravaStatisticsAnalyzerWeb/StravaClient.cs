using System;
using System.Collections.Generic;
using System.Extensions;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Extensions;
using StravaStatisticsAnalyzer.Web.Models;

namespace StravaStatisticsAnalyzer.Web
{
    public class StravaClient
    {
        private string authenticationToken_;
        private RestClient restClient_;


        public StravaClient(string authenticationToken)
        {
            restClient_ = new RestClient("https://www.strava.com/api/v3");
            authenticationToken_ = authenticationToken;
        }

        public async Task<List<Activity>> GetActivities(int? before = null, int? after = null, int? page = null, int? pageSize = null)
        {
            var request = new RestRequest(Method.GET);
            request.Resource = "/athlete/activities";
            request.AddHeader("Authorization", $"Bearer {authenticationToken_}");
            request.AddNullableParameter("before", before);
            request.AddNullableParameter("after", after);
            request.AddNullableParameter("page", page);
            request.AddNullableParameter("per_page", pageSize);

            var cancellationTokenSource = new CancellationTokenSource();
            var response = await restClient_.ExecuteGetTaskAsync<List<Activity>>(request, cancellationTokenSource.Token);

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

        public async Task<List<Activity>> GetAllActivities(int? before, int? after)
        {
            Console.WriteLine($"Fetching all activities {(before.HasValue? $"before {before.Value.FromEpoch()} " : "")}{(after.HasValue ? $"after {after.Value.FromEpoch()}" : "")}."); 
            List<Activity> activities = new List<Activity>();
            int page = 1;
            int perList = 50;
            List<Activity> partialActivities;
            while((partialActivities = await GetActivities(before, after, page++, perList)) != null && partialActivities.Count != 0)
            {
                activities.AddRange(partialActivities);   
                Console.WriteLine($"Fetched {partialActivities.Count} activities from page {page}"); 
            }
            Console.WriteLine($"Obtained {activities.Count} activities total");
            return activities;
        }
    }
}
