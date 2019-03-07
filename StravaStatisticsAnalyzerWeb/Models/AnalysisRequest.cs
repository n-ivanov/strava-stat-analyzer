using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Newtonsoft.Json;

namespace StravaStatisticsAnalyzer.Web.Models
{
    public class AnalysisRequest
    {
        public string ActivityFilter {get; set;}
        public DateTime StartInterval {get; set;}
        public DateTime EndInterval {get;set;}
    }
}