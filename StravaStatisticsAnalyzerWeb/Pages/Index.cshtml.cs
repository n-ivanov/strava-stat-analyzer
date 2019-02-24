using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StravaStatisticsAnalyzer.Web.Pages
{

    public class IndexModel : PageModel
    {
        public string Name {get; set;}
        public string LastName {get; set;}
        public string Title {get; set;}
        public string Avatar {get;set;}

        public void OnGet()
        {
            if(User.Identity.IsAuthenticated)
            {
                Name = User.FindFirst(c => c.Type == ClaimTypes.Name)?.Value;
                LastName = User.FindFirst(c => c.Type == ClaimTypes.Surname)?.Value;
                Title = User.FindFirst(c => c.Type == ClaimTypes.Gender)?.Value == "M" ? "Mr." : "Ms.";
                Avatar = User.FindFirst(c => c.Type == ClaimTypes.Uri)?.Value;
            }
        }
    }
}
