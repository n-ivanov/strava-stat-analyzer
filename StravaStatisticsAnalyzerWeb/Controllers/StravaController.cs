using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ExtendedStravaClient;
using StravaStatisticsAnalyzer.Web.Models;

namespace StravaStatisticsAnalyzer.Web
{
    [Route("[controller]/[action]")]
    public class StravaController : ControllerBase
    {
        private IServiceScopeFactory factory_;

        public StravaController(IServiceScopeFactory factory)
        {
            factory_ = factory;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetActivities(string returnUrl = "/")
        {
            string accessToken = await HttpContext.GetTokenAsync("access_token");
            var client = new Client(new ContextDBFacade(){ServiceProvider = HttpContext.RequestServices});
            client.Initialize(accessToken);         

            try
            {
                await client.GetAndSaveNewActivities(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred adding activities to the DB.");
            }

            Task.Run(async () => 
            {
                try 
                {

                    using(var scope = factory_.CreateScope())
                    {
                        var asyncClient = new Client(new ContextDBFacade(){ServiceProvider = scope.ServiceProvider});
                        if(!asyncClient.Initialize(accessToken))
                        {
                            Console.WriteLine("Unable to initialize client!");
                        }        
                        await asyncClient.GetAndSaveDetailedActivityInformation();
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    if(ex.InnerException != null)
                    {
                        Console.WriteLine(ex.InnerException.Message);
                        Console.WriteLine(ex.InnerException.StackTrace);
                    }
                }
            });

            return Redirect("../Activities");
        }
    }
}

