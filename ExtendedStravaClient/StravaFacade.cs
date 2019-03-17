using System;
using System.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Extensions;

namespace ExtendedStravaClient
{
    public class StravaFacade
    {  private string accessToken_;
        private RestClient restClient_;
        public StravaFacade()
        {
            restClient_ = new RestClient("https://www.strava.com/api/v3");
        }

        public void Initialize(string accessToken)
        {
            accessToken_ = accessToken;
        }

        public async Task<Activity> GetDetailedActivity(long id, bool includeAllEfforts = true)
        {
            var request = new RestRequest(Method.GET);
            request.Resource = $"/activities/{id}";
            request.AddHeader("Authorization", $"Bearer {accessToken_}");
            request.AddParameter("include_all_efforts",includeAllEfforts);

            var cancellationTokenSource = new CancellationTokenSource();
            var response = await restClient_.ExecuteGetTaskAsync<Activity>(request, cancellationTokenSource.Token);

            if(!response.IsSuccessful)
            {
                Console.WriteLine($"An error occurred during your request: {response.ErrorMessage} - {response.ErrorException}");
                Console.WriteLine($"({response.StatusCode}) - {response.StatusDescription}");
                return null;
            }
            return response.Data;
        }

      
        public async Task<List<Activity>> GetActivities(int? before = null, int? after = null, int? page = null, int? pageSize = null)
        {
            var request = new RestRequest(Method.GET);
            request.Resource = "/athlete/activities";
            request.AddHeader("Authorization", $"Bearer {accessToken_}");
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
            Console.WriteLine($"Fetching all activities{(before.HasValue? $" before {before.Value.FromEpoch()} " : "")}{(after.HasValue ? $" after {after.Value.FromEpoch()}" : "")}."); 
            List<Activity> activities = new List<Activity>();
            int page = 1;
            int perList = 100;
            List<Activity> partialActivities;
            while((partialActivities = await GetActivities(before, after, page++, perList)) != null && partialActivities.Count != 0)
            {
                activities.AddRange(partialActivities);   
                Console.WriteLine($"Added {partialActivities.Count} activities from page {page}"); 
            }
            Console.WriteLine($"Obtained {activities.Count} activities");
            return activities;
        }

        public async Task<Activity> ModifyActivity(long id, bool? commute = null, string description = null, string name = null, long? gearId = null)
        {
            if(!commute.HasValue && description == null && name == null && !gearId.HasValue)
            {
                Console.WriteLine($"Ignoring NOP update request for {id}.");
                return null;
            }
            var request = new RestRequest(Method.PUT);
            request.Resource = $"/activities/{id}";
            request.AddHeader("Authorization", $"Bearer {accessToken_}");
            request.AddNullableParameter("commute", commute);
            request.AddNullableParameter("gear_id", gearId);
            request.AddNullableParameter("name", name);
            request.AddNullableParameter("description", description);

            var cancellationTokenSource = new CancellationTokenSource();
            var response = await restClient_.ExecuteTaskAsync<Activity>(request, cancellationTokenSource.Token);

            if(!response.IsSuccessful)
            {
                Console.WriteLine($"An error occurred during your request: {response.ErrorMessage} - {response.ErrorException}");
                Console.WriteLine($"({response.StatusCode}) - {response.StatusDescription}");
                return null;
            }
            return response.Data;
        }
    }
}
