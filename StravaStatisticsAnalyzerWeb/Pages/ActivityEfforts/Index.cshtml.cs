using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StravaStatisticsAnalyzer.Web.Models;

namespace StravaStatisticsAnalyzer.Web.Pages.ActivityEfforts
{
    public class IndexModel : PageModel
    {
        private readonly StravaStatisticsAnalyzer.Web.Models.RazorPagesActivityEffortContext _context;

        public IndexModel(StravaStatisticsAnalyzer.Web.Models.RazorPagesActivityEffortContext context)
        {
            _context = context;
        }

        public IList<ActivityEffort> ActivityEffort { get;set; }

        public async Task OnGetAsync()
        {
            ActivityEffort = await _context.ActivityEffort.ToListAsync();
        }
    }
}
