using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StravaStatisticsAnalyzer.Web;
using StravaStatisticsAnalyzer.Web.Models;

namespace StravaStatisticsAnalyzerWeb.Pages.Segments
{
    public class IndexModel : PageModel
    {
        private readonly StravaStatisticsAnalyzer.Web.Models.RazorPagesSegmentContext _context;

        public IndexModel(StravaStatisticsAnalyzer.Web.Models.RazorPagesSegmentContext context)
        {
            _context = context;
        }

        public PaginatedList<Segment> Segment { get;set; }

        public async Task OnGetAsync(int? pageIndex)
        {
            IQueryable<Segment> segmentQuery = from s in _context.Segment select s; 

            Segment = await PaginatedList<Segment>.CreateAsync(segmentQuery.AsNoTracking(), pageIndex ?? 1);
        }
    }
}
