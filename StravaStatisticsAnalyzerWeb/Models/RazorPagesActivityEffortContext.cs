using Microsoft.EntityFrameworkCore;

namespace StravaStatisticsAnalyzer.Web.Models
{
    public class RazorPagesActivityEffortContext : DbContext
    {
        public RazorPagesActivityEffortContext (DbContextOptions<RazorPagesActivityEffortContext> options) : base(options)
        {

        }

        public DbSet<StravaStatisticsAnalyzer.Web.Models.ActivityEffort> ActivityEffort {get; set;}
    }
}
