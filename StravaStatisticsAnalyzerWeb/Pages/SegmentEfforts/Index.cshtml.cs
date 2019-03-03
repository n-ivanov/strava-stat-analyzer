using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

        public IList<SegmentEffort> SegmentEffort { get;set; }

        public async Task OnGetAsync()
        {
            SegmentEffort = await _context.SegmentEffort.ToListAsync();
        }
    }
}
