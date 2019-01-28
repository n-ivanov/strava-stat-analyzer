using System;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic; 

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
                $"https://www.strava.com/oauth/authorize?client_id={Config.CLIENT_ID}&redirect_uri=http://localhost:{Config.LOCAL_SERVER_PORT}&response_type=code&approval_prompt=auto&scope=activity:write,read";

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
        } 
    }
}
