using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StravaStatisticsAnalyzer.Web;
using StravaStatisticsAnalyzer.Web.Models;

namespace StravaStatisticsAnalyzerWeb.Pages.SegmentEfforts
{
    public class IndexModel : PageModel
    {
        private readonly StravaStatisticsAnalyzer.Web.Models.RazorPagesSegmentEffortContext _context;

        public IndexModel(StravaStatisticsAnalyzer.Web.Models.RazorPagesSegmentEffortContext context)
        {
            _context = context;
        }

        public PaginatedList<SegmentEffort> SegmentEffort { get;set; }

        public async Task OnGetAsync(int? pageIndex)
        {
            IQueryable<SegmentEffort> segmentEffortQuery = from s in _context.SegmentEffort select s; 

            SegmentEffort = await PaginatedList<SegmentEffort>.CreateAsync(segmentEffortQuery.AsNoTracking(), pageIndex ?? 1);
        }
    }
}
