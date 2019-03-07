using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Newtonsoft.Json;

namespace StravaStatisticsAnalyzer.Web.Models
{
    public class StatisticalResult<T> where T : IConvertible
    {
        public T Minimum {get;set;}
        public T Maximum {get;set;}
        public double Average {get;set;}
    }

    public class AnalysisSubResult
    {
        public StatisticalResult<int> Time {get;set;} = new StatisticalResult<int>();
        public StatisticalResult<double> Speed {get;set;} = new StatisticalResult<double>();
    }

    public class AnalysisResult
    {
        public string Name {get; set;}
        public List<AnalysisSubResult> SubResults {get; set;}
    }
}