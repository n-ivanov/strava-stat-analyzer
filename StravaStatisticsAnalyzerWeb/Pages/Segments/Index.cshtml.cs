using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

        public IList<Segment> Segment { get;set; }

        public async Task OnGetAsync()
        {
            Segment = await _context.Segment.ToListAsync();
        }
    }
}
