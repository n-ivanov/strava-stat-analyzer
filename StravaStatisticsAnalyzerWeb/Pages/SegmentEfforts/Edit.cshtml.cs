using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StravaStatisticsAnalyzer.Web.Models;

namespace StravaStatisticsAnalyzerWeb.Pages.SegmentEfforts
{
    public class EditModel : PageModel
    {
        private readonly StravaStatisticsAnalyzer.Web.Models.RazorPagesSegmentEffortContext _context;

        public EditModel(StravaStatisticsAnalyzer.Web.Models.RazorPagesSegmentEffortContext context)
        {
            _context = context;
        }

        [BindProperty]
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

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(SegmentEffort).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SegmentEffortExists(SegmentEffort.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool SegmentEffortExists(long id)
        {
            return _context.SegmentEffort.Any(e => e.ID == id);
        }
    }
}
