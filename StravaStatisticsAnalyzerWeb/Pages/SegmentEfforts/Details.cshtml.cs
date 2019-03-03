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
    public class DetailsModel : PageModel
    {
        private readonly StravaStatisticsAnalyzer.Web.Models.RazorPagesSegmentEffortContext _context;

        public DetailsModel(StravaStatisticsAnalyzer.Web.Models.RazorPagesSegmentEffortContext context)
        {
            _context = context;
        }

        public SegmentEffort SegmentEffort { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            SegmentEffort = await _context.SegmentEffort.FirstOrDefaultAsync(m => m.ID == id);

            if (SegmentEffort == null)
            {
                return NotFound();
            }
            return Page();
        }
    }
}
