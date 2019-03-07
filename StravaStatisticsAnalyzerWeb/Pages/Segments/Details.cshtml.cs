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
    public class DetailsModel : PageModel
    {
        private readonly StravaStatisticsAnalyzer.Web.Models.RazorPagesSegmentContext _context;

        public DetailsModel(StravaStatisticsAnalyzer.Web.Models.RazorPagesSegmentContext context)
        {
            _context = context;
        }

        public Segment Segment { get; set; }
        public IList<SegmentEffort> SegmentEfforts {get;set;}
        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Segment = await _context.Segment.FirstOrDefaultAsync(m => m.ID == id);
            
            var segmentEffortContext = 
                (RazorPagesSegmentEffortContext)HttpContext.RequestServices
                    .GetService(typeof(RazorPagesSegmentEffortContext));
            SegmentEfforts = await segmentEffortContext.SegmentEffort.Where(e => e.SegmentID == id).ToListAsync();
            
            if (Segment == null)
            {
                return NotFound();
            }
            return Page();
        }
    }
}
