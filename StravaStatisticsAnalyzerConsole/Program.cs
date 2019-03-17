using System;
using System.Collections.Generic; 
using System.Extensions;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ExtendedStravaClient;
using CommandLine;

namespace StravaStatisticsAnalyzerConsole
{
    public class DateIntervalOptions
    {
        [Option('s', "startDate", HelpText = "Start date for the interval in the form yyyy/MM/dd")]
        public string StartDateStr {get; set;}

        public DateTime? StartDate =>  ParseToDateTime(StartDateStr);

        [Option('e', "endDate", HelpText = "End date for the interval in the form yyyy/MM/dd")]
        public string EndDateStr {get; set;}

        public DateTime? EndDate => ParseToDateTime(EndDateStr);

        private DateTime? ParseToDateTime(string str)
        {
            return DateTime.TryParseExact(str, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt) 
                    ? dt 
                    : (DateTime?)null; 
        }
    }

    [Verb("analyze", HelpText = "Analyze your loaded Strava rides and ride segments.")]
    public class AnalyzeOptions : DateIntervalOptions
    {
        [Option('i', "intervals", Separator = ',', 
            HelpText = "Numeric intervals over which rides should be analyzed (e.g. last 5 rides, last 30 rides, etc.)")]
        public IEnumerable<int> Intervals { get; set; }

        [Option('r', "rides", Separator = ',', HelpText = "Names of rides that should be analyzed.")]
        public IEnumerable<string> Rides {get; set;}
    }


    [Verb("load", HelpText = "Load rides from Strava")]
    public class LoadOptions : DateIntervalOptions
    {          
        [Option("reload", HelpText = "Reload the rides from Strava")]
        public bool Reload {get;set;}

        [Option('i', "id", HelpText = "Strava ID for the ride that should be reloaded. Cannot be used in conjunction with date intervals or ride names.")]
        public long? Id {get;set;}

        [Option('w', "weather", HelpText = "Add weather information during the load or reload.")]
        public bool Weather {get;set;}
    }

    [Verb("modify", HelpText = "Modify loaded Strava rides")]
    public class ModifyOptions : DateIntervalOptions
    {
        [Option('r', "rides", Separator = ',', HelpText = "Names of rides that should be modified.")]
        public IEnumerable<string> Rides {get; set;}

        [Option('i', "id", HelpText = "Strava ID for the ride that should be modified. Cannot be used in conjunction with date intervals or ride names.")]
        public long? Id {get;set;}

        [Option('c', "commute", HelpText = "Set the commute flag for the selected ride(s) to the indicated value ('true' or 'false').")]
        public string Commute {get;set;}

        [Option('n', "name", HelpText = "New name for the selected ride(s)")]
        public string Name {get;set;}

        [Option('d', "description", HelpText = "New description for the selected ride(s)")]
        public string Description {get;set;}

        [Option('w', "weather", HelpText = "Add weather information to the description for the selected ride(s) if it is not already present.")]
        public bool Weather {get;set;}
    }

    class Program
    {
        static readonly HttpClient client_ = new HttpClient();
        static readonly MySqlDBFacade dbFacade_ = new MySqlDBFacade();
        static Client stravaClient_ = new Client(dbFacade_);

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<LoadOptions,AnalyzeOptions,ModifyOptions>(args)
                .MapResult(
                    (LoadOptions loadOpts) => RunLoadAndReturnExitCode(loadOpts),
                    (AnalyzeOptions analyzeOpts) => RunAnalyzeAndReturnExitCode(analyzeOpts),
                    (ModifyOptions modifyOpts) => RunModifyAndReturnExitCode(modifyOpts),
                    (errs) => 1
                );
        } 

        private static int RunLoadAndReturnExitCode(LoadOptions opts)
        {
            InitalizeClient();
            Task task;
            if(!opts.Reload)
            {
                if(opts.StartDate != null || opts.EndDate != null)
                {
                    task = stravaClient_.GetAndSaveActivities(opts.StartDate, opts.EndDate);
                }
                else
                {
                    task = stravaClient_.GetAndSaveNewActivities();
                }
            }
            else
            {
                if(opts.Id.HasValue)
                {
                    task = stravaClient_.ReloadActivity(opts.Id.Value, opts.Weather);
                }
                else
                {
                    task = stravaClient_.ReloadActivities(opts.Weather);
                }
            }
            task.Wait();  
            return 0;
        }

        private static void InitalizeClient(bool connectToStrava = true, bool connectToWeather = true)
        {
            string token = null;
            token = Authenticate();
            stravaClient_.Initialize(token, Configuration.DarkSky.CLIENT_SECRET);
        }

        private static string Authenticate()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{Configuration.Strava.LOCAL_SERVER_PORT}/");
            listener.Start();

            var targetAuthUrl =  
                $"https://www.strava.com/oauth/authorize?client_id={Configuration.Strava.CLIENT_ID}&redirect_uri=http://localhost:{Configuration.Strava.LOCAL_SERVER_PORT}&response_type=code&approval_prompt=auto&scope=activity:write,activity:read_all";

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

        private static int RunAnalyzeAndReturnExitCode(AnalyzeOptions opts)
        {
            InitalizeClient(false,false);
            if(opts.Intervals.Count() != 0)
            {
                Analyze(opts.Rides, opts.Intervals);
            }
            else
            {
                Analyze(opts.Rides, opts.StartDate, opts.EndDate);
            }
            return 0;
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
                var results = stravaClient_.AnalyzeRide(ride, new (DateTime? start, DateTime? end)[]{(start,end)}); 
                presenter.PresentResults(results,new (DateTime? start, DateTime? end)[]{(start, end)});
                Console.WriteLine();
            }
        }

        private static int RunModifyAndReturnExitCode(ModifyOptions opts)
        {
            InitalizeClient();
            if(opts.Id.HasValue)
            {
                bool? commute = null;
                if(String.Compare(opts.Commute, "true", CultureInfo.InvariantCulture, CompareOptions.IgnoreCase) == 0)
                {
                    commute = true;
                }
                else if(String.Compare(opts.Commute, "false", CultureInfo.InvariantCulture, CompareOptions.IgnoreCase) == 0)
                {
                    commute = false;
                }
                var task = stravaClient_.ModifyActivity(opts.Id.Value, commute, opts.Description, opts.Name);
                task.Wait();
                return 0;
            }
            return 1;
        }
    }   
}
