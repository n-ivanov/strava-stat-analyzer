using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StravaStatisticsAnalyzer.Web.Models;

namespace StravaStatisticsAnalyzer.Web.Pages.Activities
{
    public class DetailsModel : PageModel
    {
        private readonly StravaStatisticsAnalyzer.Web.Models.RazorPagesActivityContext _context;

        public DetailsModel(StravaStatisticsAnalyzer.Web.Models.RazorPagesActivityContext context)
        {
            _context = context;
        }

        public Activity Activity { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Activity = await _context.Activity.FirstOrDefaultAsync(m => m.ID == id);

            if (Activity == null)
            {
                return NotFound();
            }
            return Page();
        }
    }
}
