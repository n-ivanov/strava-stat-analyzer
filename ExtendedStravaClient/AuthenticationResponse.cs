using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ExtendedStravaClient
{
    public class AuthenticationResponse
    {
        [JsonProperty("refresh_token")]
        public string RefreshToken {get; private set;}

        [JsonProperty("access_token")]
        public string AccessToken {get; private set;}

        [JsonProperty("athlete")]
        public StravaAthlete Athlete {get; private set;}


        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        public static AuthenticationResponse Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<AuthenticationResponse>(json);
        }
    }

}