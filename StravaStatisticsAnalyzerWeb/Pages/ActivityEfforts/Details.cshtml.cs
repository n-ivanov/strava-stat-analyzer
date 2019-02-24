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
    public class DetailsModel : PageModel
    {
        private readonly StravaStatisticsAnalyzer.Web.Models.RazorPagesActivityEffortContext _context;

        public DetailsModel(StravaStatisticsAnalyzer.Web.Models.RazorPagesActivityEffortContext context)
        {
            _context = context;
        }

        public ActivityEffort ActivityEffort { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            ActivityEffort = await _context.ActivityEffort.FirstOrDefaultAsync(m => m.ID == id);

            if (ActivityEffort == null)
            {
                return NotFound();
            }
            return Page();
        }
    }
}
