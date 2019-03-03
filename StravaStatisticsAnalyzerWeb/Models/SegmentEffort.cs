using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Newtonsoft.Json;

namespace StravaStatisticsAnalyzer.Web.Models
{
    public class SegmentEffort
    {
        public long ID {get; set;}
        
        [Display(Name = "Segment Name")]
        public string Name {get; set;} 

        [Display(Name = "Distance (m)")]
        public double Distance {get; set;}

        [Display(Name = "Moving Time (s)")]
        public int MovingTime {get; set;}

        [Display(Name = "Elapsed Time (s)")]
        public int ElapsedTime {get; set;}

        [Display(Name = "Average Speed (m/s)")]
        public double AvgSpeed {get;set;}

        public DateTime DateTime {get; set;}

        public long ActivityID {get;set;}
        public long AthleteID {get; set;}
        public long SegmentID {get; set;}
    }
}