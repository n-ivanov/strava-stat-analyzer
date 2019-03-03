using Microsoft.EntityFrameworkCore;

namespace StravaStatisticsAnalyzer.Web.Models
{
    public class RazorPagesSegmentContext : DbContext
    {
        public RazorPagesSegmentContext (DbContextOptions<RazorPagesSegmentContext> options) : base(options)
        {

        }

        public DbSet<StravaStatisticsAnalyzer.Web.Models.Segment> Segment {get; set;}
    }
}
