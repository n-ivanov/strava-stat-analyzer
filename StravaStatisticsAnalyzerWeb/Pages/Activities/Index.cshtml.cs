using System;
using System.Collections.Generic;
using System.Extensions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StravaStatisticsAnalyzer.Web.Models;

namespace StravaStatisticsAnalyzer.Web.Pages.Activities
{
    public class IndexModel : PageModel
    {
        private readonly StravaStatisticsAnalyzer.Web.Models.RazorPagesActivityContext _context;

        public IndexModel(StravaStatisticsAnalyzer.Web.Models.RazorPagesActivityContext context)
        {
            _context = context;
        }

        public PaginatedList<Activity> Activity { get;set; }

        public async Task OnGetAsync(int? pageIndex)
        {
            IQueryable<Activity> activityQuery = from a in _context.Activity select a; 

            Activity = await PaginatedList<Activity>.CreateAsync(activityQuery.AsNoTracking(), pageIndex ?? 1);
        }
    }
}
