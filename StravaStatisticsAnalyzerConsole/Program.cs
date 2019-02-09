using System;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Extensions;
using System.Collections.Generic; 
using StravaStatisticsAnalyzer;


namespace StravaStatisticsAnalyzerConsole
{
    class Program
    {
        static readonly HttpClient client_ = new HttpClient();
        static void Main(string[] args)
        {
            Console.WriteLine(" o__  ");
            Console.WriteLine(",>/_ ");
            Console.WriteLine("(*)`(*)");

            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{Config.LOCAL_SERVER_PORT}/");
            listener.Start();

            var targetAuthUrl =  
                $"https://www.strava.com/oauth/authorize?client_id={Config.CLIENT_ID}&redirect_uri=http://localhost:{Config.LOCAL_SERVER_PORT}&response_type=code&approval_prompt=auto&scope=activity:read";

            Console.WriteLine($"Attempting to access {targetAuthUrl}");

            var proc = System.Diagnostics.Process.Start("xdg-open", targetAuthUrl);
            Console.WriteLine("Waiting for Strava OAuth to complete...");

            var context = listener.GetContext();
            var request = context.Request;
            var url = request.Url.ToString();

            var returnedArgs = url.Substring(url.IndexOf("?") + 1).Split("&").Select(x => x.Split("=")).ToDictionary(x => x[0], x => x[1]);

            foreach(var kvp in returnedArgs)
            {
                Console.WriteLine($"{kvp.Key}:{kvp.Value}");
            }

            if(returnedArgs.TryGetValue("error", out var error))
            {
                Console.WriteLine($"An error occurred. '{error}'");
                Console.WriteLine("Aborting program execution.");
                return;
            }

            var requestVals = new Dictionary<string,string>
            {
                {"client_id", Config.CLIENT_ID.ToString()},
                {"client_secret", Config.CLIENT_SECRET},
                {"code",returnedArgs["code"]},
                {"grant_type","authorization_code"}
            };

            var requestContent = new FormUrlEncodedContent(requestVals);
            var responseTask = client_.PostAsync("https://www.strava.com/oauth/token", requestContent);
            responseTask.Wait();
            var response = responseTask.Result;

            var responseReadTask = response.Content.ReadAsStringAsync();
            responseReadTask.Wait();
            var responseText = responseReadTask.Result;

            Console.WriteLine($"{responseText}");
            var deserializedResponse = AuthenticationResponse.Deserialize(responseText);
            Console.WriteLine($"Welcome {deserializedResponse.Athlete?.FirstName}! Your token is {deserializedResponse.AccessToken}.");

            var analyzer = new Analyzer();
            analyzer.Initialize(deserializedResponse.AccessToken);
            analyzer.GetAndSaveNewActivities();     
            var intervals = new [] {5,30, Int32.MaxValue};
            var rides = new [] {"HFW","WFH"};
            foreach(var ride in rides)
            {
                Console.WriteLine($"========= Analysis of {ride} =========");
                var results = analyzer.AnalyzeRide(ride, intervals);    
                foreach(var result in results)
                {
                    Console.WriteLine($"In {result.IntervalLength} rides for '{ride}', your best ride was {result.Time.Minimum.ToTime()} @ {result.Speed.Maximum *3.6} km/h.");
                    Console.WriteLine($"The average ride in this interval was {Convert.ToInt32(result.Time.Average).ToTime()} @ {result.Speed.Average *3.6} km/h.");
                    Console.WriteLine($"The worst ride in this interval was {Convert.ToInt32(result.Time.Maximum).ToTime()} @ {result.Speed.Minimum *3.6} km/h.");
                    Console.WriteLine("----------------------------------------------------------");
                }
            }
        } 
    }
}
