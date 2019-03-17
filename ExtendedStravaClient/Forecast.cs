using System;
using System.Collections.Generic;

namespace ExtendedStravaClient.Weather 
{
    public class HourlyForecasts
    {
        public string Summary {get;set;}
        public string Icon {get;set;}
        public List<HourForecast> Data {get;set;}
    }

    public class DayForecast
    {
        public double Latitude {get;set;}
        public double Longitude {get;set;}
        public string TimeZone {get;set;}
        public int Offset {get;set;}

        public HourlyForecasts Hourly {get;set;}
    }
}