using Microsoft.EntityFrameworkCore;

namespace StravaStatisticsAnalyzer.Web.Models
{
    public class RazorPagesActivityContext : DbContext
    {
        public RazorPagesActivityContext (DbContextOptions<RazorPagesActivityContext> options) : base(options)
        {

        }

        public DbSet<StravaStatisticsAnalyzer.Web.Models.Activity> Activity {get; set;}
    }
}
