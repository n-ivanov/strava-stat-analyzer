using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Newtonsoft.Json;

namespace StravaStatisticsAnalyzer.Web.Models
{
    public class Segment
    {
        public long ID {get; set;}
        
        [Display(Name = "Segment Name")]
        public string Name {get; set;} 

        [Display(Name = "Distance (m)")]
        public double Distance {get; set;}

        [Display(Name = "Average Grade")]
        public double AvgGrade {get;set;}

        [Display(Name = "Max Grade")]
        public double MaxGrade {get;set;}

        [Display(Name = "Elevation High")]
        public double ElevationHigh {get; set;}
        
        [Display(Name = "Elevation Low")]
        public double ElevationLow {get;set;}

        [Display(Name = "Start Latitude")]
        public double StartLatitude {get;set;}

        [Display(Name = "Start Longitude")]
        public double StartLongitude {get;set;}

        [Display(Name = "End Latitude")]
        public double EndLatitude {get;set;}

        [Display(Name = "End Longitude")]
        public double EndLongitude {get;set;}

        public bool Starred {get;set;}
    }
}