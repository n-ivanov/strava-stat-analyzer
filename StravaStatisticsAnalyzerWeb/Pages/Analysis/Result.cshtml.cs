using System;
using System.Collections.Generic;
using System.Extensions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StravaStatisticsAnalyzer.Web.Models;
using ExtendedStravaClient;

namespace StravaStatisticsAnalyzer.Web.Pages.Analysis
{
    public class ResultModel : PageModel
    {
        public ResultModel()
        {
        }

        public IList<AnalysisResult> AnalysisResults { get;set; }
        public VelocityUnits Units {get;set;}
        public string UnitsStr => UnitsDisplay();

        public async Task OnGetAsync(string activityFilter, int start, int end, VelocityUnits units)
        {
            string accessToken = await HttpContext.GetTokenAsync("access_token");
            var client = new Client(new ContextDBFacade(){ServiceProvider = HttpContext.RequestServices});
            client.Initialize(accessToken);         

            var effortAnalyses = client.AnalyzeRide(activityFilter, 
                new (DateTime? start, DateTime? end)[]{(start.FromEpoch(), end.FromEpoch())});

            AnalysisResults = new List<AnalysisResult>(effortAnalyses.Values.Count());
            foreach(var kvp in effortAnalyses)
            {
                AnalysisResults.Add(new AnalysisResult()
                {
                    Name = kvp.Key,
                    SubResults = kvp.Value.Select(a => ConvertRideEffortAnalysisToSubResult(a)).ToList()
                });
            }
            Units = units;
        }

        public AnalysisSubResult ConvertRideEffortAnalysisToSubResult(IRideEffortAnalysis analysis)
        {
            var result = new AnalysisSubResult();
            result.Time.Average = analysis.Time.Average;
            result.Time.Maximum = analysis.Time.Maximum;
            result.Time.Minimum = analysis.Time.Minimum;
            result.Speed.Average = analysis.Speed.Average;
            result.Speed.Maximum = analysis.Speed.Maximum;
            result.Speed.Minimum = analysis.Speed.Minimum;
            return result;
        }    

        public string ConvertUnitsAndRound(double value)
        {
            double convertedVal; 
            switch(Units)
            {
                case VelocityUnits.KM_H:
                    convertedVal = value * 3600 / 1000;
                    break;
                case VelocityUnits.Mi_H:
                    convertedVal = value * 2.23694;
                    break;
                case VelocityUnits.M_S:
                default:
                    convertedVal = value;
                    break;
            }
            return convertedVal.ToString("#.###");
        }

        public string UnitsDisplay()
        {
            switch(Units)
            {
                case VelocityUnits.KM_H:
                    return "kph";
                case VelocityUnits.Mi_H:
                    return "mph";
                case VelocityUnits.M_S:
                    return "m/s";
                default:
                    return "Unit Error";
            }
        }
    }
}
