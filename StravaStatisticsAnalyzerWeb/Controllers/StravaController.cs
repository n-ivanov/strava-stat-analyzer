using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StravaStatisticsAnalyzer.Web.Models;

namespace StravaStatisticsAnalyzer.Web
{
    [Route("[controller]/[action]")]
    public class StravaController : ControllerBase
    {
        private RazorPagesActivityContext context_;
        public StravaController(RazorPagesActivityContext context)
        {
            context_ = context;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetActivities(string returnUrl = "/")
        {
            string accessToken = await HttpContext.GetTokenAsync("access_token");
            
            var client = new StravaClient(accessToken);         

            try
            {
                foreach(var activity in await client.GetAllActivities(null,1534982400))
                {
                    context_.Activity.Add(activity);
                }
                context_.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred adding activities to the DB.");
            }

            return Redirect("../Activities");
        }
    }
}

