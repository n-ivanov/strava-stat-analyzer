using System;
using System.Collections.Generic; 
using System.Extensions;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ExtendedStravaClient;
using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;

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

        [Usage(ApplicationAlias= "dotnet run --project StravaStatisticsAnalyzerConsole.csproj --")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Analyze last n rides with a given name", new AnalyzeOptions()
                {
                    Intervals = new List<int>(){5},
                    Rides = new List<string>(){"HFW"}
                });
                yield return new Example("Analyze last rides over several n intervals with given names", new AnalyzeOptions()
                {
                    Intervals = new List<int>(){5,30},
                    Rides = new List<string>(){"HFW","WFH"}
                });
                yield return new Example("Analyze rides between two dates", new AnalyzeOptions()
                {
                    StartDateStr = "2019/05/05",
                    EndDateStr = "2019/05/11",
                    Rides = new List<string>(){"HFW","WFH"}
                });
            }
        }
    }


    [Verb("load", HelpText = "Load rides from Strava")]
    public class LoadOptions : DateIntervalOptions
    {          
        [Option("reload", HelpText = "Reload the rides from Strava")]
        public bool Reload {get;set;}

        [Option("reloadSegments", HelpText = "Reload the segments for each ride from Strava")]
        public bool ReloadSegments {get;set;}

        [Option('i', "id", HelpText = "Strava ID for the ride that should be reloaded. Cannot be used in conjunction with date intervals or ride names.")]
        public long? Id {get;set;}

        [Option('w', "weather", HelpText = "Add weather information during the load or reload.")]
        public bool Weather {get;set;}

        [Usage(ApplicationAlias= "dotnet run --project StravaStatisticsAnalyzerConsole.csproj --")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Load new rides since last update", new LoadOptions());
                yield return new Example("Reload rides in given interval", new LoadOptions(){
                    StartDateStr = "2019/05/05", 
                    EndDateStr = "2019/05/11",
                    Reload = true,
                });
                yield return new Example("Reload rides and segments in given interval with added weather information", new LoadOptions(){
                    StartDateStr = "2019/05/05", 
                    EndDateStr = "2019/05/11",
                    Reload = true,
                    Weather = true,
                    ReloadSegments = true
                });
                yield return new Example("Reload ride with specific ID in given interval", new LoadOptions(){
                    StartDateStr = "2019/05/05", 
                    EndDateStr = "2019/05/11",
                    Reload = true,
                    Id = 2296595173
                });
            }
        }
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

        [Usage(ApplicationAlias= "dotnet run --project StravaStatisticsAnalyzerConsole.csproj --")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Modify selected rides to be tagged as a Commute", new ModifyOptions()
                {
                    Commute = "true",
                    Rides = new List<string>(){"HFW"}
                });
                yield return new Example("Rename selected rides", new ModifyOptions()
                {
                    Name = "Home From Work",
                    Rides = new List<string>(){"HFW"}
                });
                yield return new Example("Add weather information to selected ride", new ModifyOptions()
                {
                    Weather = true,
                    Id = 2296595173
                });
                yield return new Example("Change description for selected ride", new ModifyOptions()
                {
                    Description = "Some new description",
                    Id = 2296595173
                });
            }
        }
    }

    class Program
    {
        static readonly HttpClient client_ = new HttpClient();
        static readonly DBFacade dbFacade_ = new DBFacade();
        static Client stravaClient_ = new Client(dbFacade_);
        public static IDictionary<string, IDictionary<string,string>> Configuration {get;set;}

        static void Main(string[] args)
        {
            Configuration = GenerateConfigs();
            Parser.Default.ParseArguments<LoadOptions,AnalyzeOptions,ModifyOptions>(args)
                .MapResult(
                    (LoadOptions loadOpts) => RunLoadAndReturnExitCode(loadOpts),
                    (AnalyzeOptions analyzeOpts) => RunAnalyzeAndReturnExitCode(analyzeOpts),
                    (ModifyOptions modifyOpts) => RunModifyAndReturnExitCode(modifyOpts),
                    (errs) => 1
                );
        }

        private static IDictionary<string, IDictionary<string, string>> GenerateConfigs()
        {
            string path = "config.json";
            if(!File.Exists(path))
            {
                Console.WriteLine($"Config file '{path}' could not be found.");
            }

            string configText = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<IDictionary<string, IDictionary<string, string>>>(configText);
        }

        private static int RunLoadAndReturnExitCode(LoadOptions opts)
        {
            InitializeClient();
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
                    task = stravaClient_.ReloadActivities(null, opts.StartDate, opts.EndDate, opts.Weather, opts.ReloadSegments);
                }
            }
            task.Wait();  
            return 0;
        }

        private static void InitializeClient(bool connectToStrava = true, bool connectToWeather = true)
        {
            string token = null;
            token = Authenticate();
            stravaClient_.Initialize(token, Configuration["darkSky"]["clientSecret"]);
        }

        private static string Authenticate()
        {
            var listener = new HttpListener();
            var localServerPort = Configuration["strava"]["localServerPort"];
            var clientId = Configuration["strava"]["clientId"];
            var clientSecret = Configuration["strava"]["clientSecret"];
            listener.Prefixes.Add($"http://localhost:{localServerPort}/");
            listener.Start();

            var targetAuthUrl =  
                $"https://www.strava.com/oauth/authorize?client_id={clientId}&redirect_uri=http://localhost:{localServerPort}&response_type=code&approval_prompt=auto&scope=activity:write,activity:read_all";

            Console.WriteLine($"Attempting to access {targetAuthUrl}");

            var proc = System.Diagnostics.Process.Start("xdg-open", targetAuthUrl);
            Console.WriteLine("Waiting for Strava OAuth to complete...");

            var context = listener.GetContext();
            var request = context.Request;
            var url = request.Url.ToString();

            var returnedArgs = url.Substring(url.IndexOf("?") + 1).Split("&").Select(x => x.Split("=")).ToDictionary(x => x[0], x => x[1]);

            if(returnedArgs.TryGetValue("error", out var error))
            {
                Console.WriteLine($"An error occurred. '{error}'");
                Console.WriteLine("Aborting program execution.");
                return null;
            }

            var requestVals = new Dictionary<string,string>
            {
                {"client_id", clientId},
                {"client_secret", clientSecret},
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

            var deserializedResponse = AuthenticationResponse.Deserialize(responseText);
            Console.WriteLine($"Welcome {deserializedResponse.Athlete?.FirstName}! Your token is {deserializedResponse.AccessToken}.");
            return deserializedResponse.AccessToken;
        }

        private static int RunAnalyzeAndReturnExitCode(AnalyzeOptions opts)
        {
            InitializeClient(false,false);
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
            InitializeClient();
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
