using System;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Extensions;
using System.Collections.Generic; 
using ExtendedStravaClient;
using CommandLine;

namespace StravaStatisticsAnalyzerConsole
{
    public class Options
    {
        [Option('u', "update", HelpText = "Check for updates from Strava, which requires authenticating with the Strava API using OAuth.")]
        public bool Update {get; set;}

        [Option('i', "intervals", Separator = ',', HelpText = "Intervals over which rides should be analyzed.")]
        public IEnumerable<int> Intervals { get; set; }

        [Option('r', "rides", Separator = ',', HelpText = "Names of rides that should be analyzed.")]
        public IEnumerable<string> Rides {get; set;}

        // [Option('v', "verbose", HelpText = "Verbose logging (Currently unimplemented)")]
        // public bool Verbose {get; set;}
    }

    class Program
    {
        static readonly HttpClient client_ = new HttpClient();
        static readonly MySqlDBFacade dbFacade_ = new MySqlDBFacade();
        static Analyzer analyzer_ = new Analyzer(dbFacade_);

        static void Main(string[] args)
        {
            Console.WriteLine(" o__  ");
            Console.WriteLine(",>/_ ");
            Console.WriteLine("(*)`(*)");

            Options options = null;
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       options = o;
                   });

            string token = null;
            if(options.Update)
            {
                token = Authenticate();
            }
           
            analyzer_.Initialize(token);
            if(options.Update)
            {
                var task = analyzer_.GetAndSaveNewActivities();
                task.Wait();     
            }

            Analyze(options.Rides, options.Intervals);
        } 

        private static string Authenticate()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{Configuration.Strava.LOCAL_SERVER_PORT}/");
            listener.Start();

            var targetAuthUrl =  
                $"https://www.strava.com/oauth/authorize?client_id={Configuration.Strava.CLIENT_ID}&redirect_uri=http://localhost:{Configuration.Strava.LOCAL_SERVER_PORT}&response_type=code&approval_prompt=auto&scope=activity:read";

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
                return null;
            }

            var requestVals = new Dictionary<string,string>
            {
                {"client_id", Configuration.Strava.CLIENT_ID.ToString()},
                {"client_secret", Configuration.Strava.CLIENT_SECRET},
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
            return deserializedResponse.AccessToken;
        }

        private static void Analyze(IEnumerable<string> rides, IEnumerable<int> intervals)
        {
            if(rides == null || rides.Count() == 0)
            {
                Console.WriteLine("No rides specified. Analysis will not be conducted.");
                return;
            }
            var presenter = new ConsoleResultPresenter();
            var intervalsArr = intervals.ToArray();
            foreach(var ride in rides)
            {
                var results = analyzer_.AnalyzeRide(ride, intervals.ToArray()); 
                presenter.PresentResults(results, intervalsArr);
                Console.WriteLine();
            }
        }
    }   
}
