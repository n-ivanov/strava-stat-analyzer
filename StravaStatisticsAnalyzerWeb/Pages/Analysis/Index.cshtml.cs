using System;
using System.Collections.Generic;
using System.Extensions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StravaStatisticsAnalyzer.Web.Models;

namespace StravaStatisticsAnalyzer.Web.Pages.Analysis
{
    public enum VelocityUnits
    {
        M_S,
        KM_H,
        Mi_H
    }

    public class IndexModel : PageModel
    {
        public IndexModel()
        {
        }

        public int NumAnalyses {get; set;} = 1;

        [BindProperty]
        public IList<AnalysisRequest> AnalysisRequests { get;set; }

        [BindProperty]
        public VelocityUnits Units {get;set;}

        public async Task OnGetAsync()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine("On post triggered.");
            foreach(var request in AnalysisRequests)
            {
                Console.WriteLine(
                    $"ActivityFilter: {request.ActivityFilter} StartInterval: {request.StartInterval} EndInterval: {request.EndInterval}");
                return RedirectToPage("./Result", new 
                    { 
                        activityFilter=request.ActivityFilter, 
                        start=request.StartInterval.ToEpoch(), 
                        end=request.EndInterval.ToEpoch(),
                        units=Units
                    });
            }
            return NotFound();
        }
    }
}
