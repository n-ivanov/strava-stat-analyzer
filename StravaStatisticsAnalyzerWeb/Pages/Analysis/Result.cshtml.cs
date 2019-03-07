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

        public async Task OnGetAsync(string activityFilter, int start, int end)
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
    }
}
