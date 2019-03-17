using System;

namespace ExtendedStravaClient.Weather 
{
    public class HourForecast
    {
        public long Time {get;set;}
        public string Summary {get;set;}
        public string Icon {get;set;}
        public double PrecipIntensity {get;set;}
        public double PrecipProbability {get;set;}
        public double Temperature {get;set;}
        public double ApparentTemperature {get;set;}
        public double DewPoint {get;set;}
        public double Humidity {get;set;}
        public double Pressure {get;set;}
        public double WindSpeed {get;set;}
        public double WindGust {get;set;}
        public int WindBearing {get;set;}
        public double CloudCover {get;set;}
        public double UVIndex {get;set;}
        public double Visibility {get;set;}
        public double OZone {get;set;}
    }
}