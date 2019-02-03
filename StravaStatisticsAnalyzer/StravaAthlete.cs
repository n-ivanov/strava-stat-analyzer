using Newtonsoft.Json;

namespace StravaStatisticsAnalyzer
{
    public class StravaAthlete
    {
        [JsonProperty("id")]
        public long ID { get; private set; }
        
        [JsonProperty("firstname")]
        public string FirstName { get; private set; }
        
        [JsonProperty("lastname")]
        public string LastName { get; private set;  }
    }
}