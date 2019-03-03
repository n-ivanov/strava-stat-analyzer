using Microsoft.EntityFrameworkCore;

namespace StravaStatisticsAnalyzer.Web.Models
{
    public class RazorPagesSegmentEffortContext : DbContext
    {
        public RazorPagesSegmentEffortContext (DbContextOptions<RazorPagesSegmentEffortContext> options) : base(options)
        {

        }

        public DbSet<StravaStatisticsAnalyzer.Web.Models.SegmentEffort> SegmentEffort {get; set;}
    }
}
