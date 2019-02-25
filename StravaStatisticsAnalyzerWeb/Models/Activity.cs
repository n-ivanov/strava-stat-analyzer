using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Newtonsoft.Json;

namespace StravaStatisticsAnalyzer.Web.Models
{
    public class Activity
    {
        public long ID {get; set;}
        
        [Display(Name = "Activity Name")]
        public string Name {get; set;} 

        [Display(Name = "Distance (m)")]
        public double Distance {get; set;}

        [Display(Name = "Moving Time (s)")]
        public int MovingTime {get; set;}

        [Display(Name = "Elapsed Time (s)")]
        public int ElapsedTime {get; set;}

        [Display(Name = "Average Speed (m/s)")]
        public double AvgSpeed {get;set;}

        [Display(Name = "Max Speed (m/s)")]
        public double MaxSpeed {get;set;}

        public DateTime DateTime {get; set;}

        public long AthleteID {get; set;}

        public double TotalElevationGain {get;set;}

        public double ElevationHigh {get; set;}
        
        public double ElevationLow {get;set;}

        public double StartLatitude {get;set;}

        public double StartLongitude {get;set;}

        public double EndLatitude {get;set;}

        public double EndLongitude {get;set;}

        public string Description {get;set;}

        public bool Commute {get;set;}

    }
}