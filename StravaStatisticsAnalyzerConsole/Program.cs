using System;
using System.Collections.Generic; 
using System.Extensions;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using ExtendedStravaClient;
using CommandLine;

namespace StravaStatisticsAnalyzerConsole
{
    public class Options
    {
        [Option('u', "update", HelpText = "Check for updates from Strava, which requires authenticating with the Strava API using OAuth.")]
        public bool Update {get; set;}

        [Option('i', "intervals", Separator = ',', 
            HelpText = "Numeric intervals over which rides should be analyzed (e.g. last 5 rides, last 30 rides, etc.)")]
        public IEnumerable<int> Intervals { get; set; }

        [Option('r', "rides", Separator = ',', HelpText = "Names of rides that should be analyzed.")]
        public IEnumerable<string> Rides {get; set;}

        [Option('s', "startDate", HelpText = "Start date in the form yyyy/MM/dd")]
        public string StartDateStr {get; set;}

        public DateTime? StartDate =>  
            DateTime.TryParseExact(StartDateStr, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt) 
                            ? dt 
                            : (DateTime?)null; 

        [Option('e', "endDate", HelpText = "End date in the form yyyy/MM/dd")]
        public string EndDateStr {get; set;}

        public DateTime? EndDate =>  
            DateTime.TryParseExact(EndDateStr, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt) 
                            ? dt 
                            : (DateTime?)null; 
        // [Option('v', "verbose", HelpText = "Verbose logging (Currently unimplemented)")]
        // public bool Verbose {get; set;}
    }

    class Program
    {
        static readonly HttpClient client_ = new HttpClient();
        static readonly MySqlDBFacade dbFacade_ = new MySqlDBFacade();
        static Client stravaClient_ = new Client(dbFacade_);

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
           
            stravaClient_.Initialize(token);
            if(options.Update)
            {
                var task = stravaClient_.GetAndSaveNewActivities();
                task.Wait();     
            }

            if(options.Intervals.Count() != 0)
            {
                Analyze(options.Rides, options.Intervals);
            }
            else
            {
                Analyze(options.Rides, options.StartDate, options.EndDate);
            }
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
                var results = stravaClient_.AnalyzeRide(ride, intervals.ToArray()); 
                presenter.PresentResults(results, intervalsArr);
                Console.WriteLine();
            }
        }

        private static void Analyze(IEnumerable<string> rides, DateTime? start, DateTime? end)
        {
            if(rides == null || rides.Count() == 0)
            {
                Console.WriteLine("No rides specified. Analysis will not be conducted.");
                return;
            }
            var presenter = new ConsoleResultPresenter();
            foreach(var ride in rides)
            {
                var results = stravaClient_.AnalyzeRide(ride, new List<(DateTime? start, DateTime? end)>(){(start,end)}); 
                presenter.PresentResults(results,new (DateTime? start, DateTime? end)[]{(start, end)});
                Console.WriteLine();
            }
        }
    }   
}
