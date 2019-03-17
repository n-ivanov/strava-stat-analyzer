using System;
using System.Collections.Generic;
using System.Extensions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Extensions;

namespace ExtendedStravaClient.Weather
{
    public class WeatherClient
    {
        private string accessKey_;
        private RestClient restClient_;
        private static Dictionary<double,string> bearingsToCardinalDirections_ = new Dictionary<double, string>()
        {
            {0,"N"},
            {22.5,"NNE"},
            {45,"NE"},
            {67.5,"ENE"},
            {90,"E"},
            {112.5,"ESE"},
            {135,"SE"},
            {157.5,"SSE"},
            {180,"S"},
            {202.5,"SSW"},
            {225,"SW"},
            {247.5,"WSW"},
            {270,"W"},
            {292.5,"WNW"},
            {315,"NW"},
            {337.5,"NNW"},
        };

        public WeatherClient()
        {
            restClient_ = new RestClient($"https://api.darksky.net/");
        }

        public void Initialize(string accessKey)
        {
            accessKey_ = accessKey;
        }

        public async Task<DayForecast> GetHistoricWeatherForecast(double latitude, double longitude, DateTime dateTime, string units = "si")
        {
            return await GetHistoricWeatherForecast(latitude, longitude, dateTime.ToEpoch(), units);
        }

        public async Task<DayForecast> GetHistoricWeatherForecast(double latitude, double longitude, int timeSinceEpoch, string units = "si")
        {
            var request = new RestRequest(Method.GET);
            request.Resource = $"/forecast/{accessKey_}/{latitude},{longitude},{timeSinceEpoch}";
            request.AddParameter("units", units);
            request.AddParameter("exclude", "currently,daily,flags");

            var cancellationTokenSource = new CancellationTokenSource();
            var response = await restClient_.ExecuteGetTaskAsync<DayForecast>(request, cancellationTokenSource.Token);

            if(!response.IsSuccessful)
            {
                Console.WriteLine($"An error occurred during your request: {response.ErrorMessage} - {response.ErrorException}");
                Console.WriteLine($"({response.StatusCode}) - {response.StatusDescription}");
                return null;
            }
            return response.Data;
        }

        public async Task<string> GetWeatherDescription(double latitude, double longitude, int timeSinceEpoch, string units = "si")
        {
            var forecast = await GetHistoricWeatherForecast(latitude, longitude, timeSinceEpoch, units);
            int i = 0;
            HourForecast startHour = null;
            HourForecast endHour = null;
            long difference = 0; 
            while(i < forecast.Hourly.Data.Count)
            {
                difference = timeSinceEpoch - forecast.Hourly.Data[i].Time;
                if(difference < 3600)
                {
                    startHour = forecast.Hourly.Data[i];
                    endHour = forecast.Hourly.Data[i+1];
                    break;
                }
                i++;
            }
            double startHourWeight = (double)(3600 - difference) / 3600;
            double endHourWeight = 1 - startHourWeight;

            var temperature = startHour.Temperature * startHourWeight + endHour.Temperature * endHourWeight;
            var windSpeed = startHour.WindSpeed * startHourWeight + endHour.WindSpeed * endHourWeight;
            var windGust = startHour.WindGust * startHourWeight + endHour.WindGust * endHourWeight;
            var windBearing = startHour.WindBearing * startHourWeight + endHour.WindBearing * endHourWeight;
            
            var precipitationIntensity = startHour.PrecipIntensity * startHourWeight + endHour.PrecipIntensity * endHourWeight;
            var precipitationChance = startHour.PrecipProbability * startHourWeight + endHour.PrecipProbability * endHourWeight;
            var summary = startHourWeight > 0.5 ? startHour.Summary : endHour.Summary;

            var windSpeedUnits = GetWindSpeedUnits(units);
            return $"{summary} ({temperature:0.00}{GetTemperatureUnits(units)}){Environment.NewLine}Wind {windSpeed:0.00}-{windGust:0.00} {windSpeedUnits:0.00} {GetCardinalDirection(windBearing)} ({windBearing:0.00}°){Environment.NewLine}Rain {precipitationChance}%";      
        }

        private string GetTemperatureUnits(string units)
        {
            switch(units)
            {
                case "ca":
                case "si":
                case "uk2":
                    return "°C";
                case "us":
                default:
                    return "°F";
            }
        }

        private string GetWindSpeedUnits(string units)
        {
            switch(units)
            {
                case "ca":
                    return "kph";
                case "si":
                    return "m/s";
                case "us":
                case "uk2":
                default:
                    return "mph";
            }
        }

        private string GetCardinalDirection(double windBearing)
        {
            var roundedWindBearing = Math.Round(windBearing / 22.5) * 22.5;
            if(roundedWindBearing > 360)
            {
                roundedWindBearing %= 360;
            }
            if(!bearingsToCardinalDirections_.TryGetValue(roundedWindBearing, out var dir))
            {
                return "N/A";
            }
            return dir;
        }
    }
}