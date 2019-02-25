using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json; 
using Newtonsoft.Json.Linq; 
using StravaStatisticsAnalyzer.Web.Models;


namespace StravaStatisticsAnalyzer.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddAuthentication(options => 
                {
                   options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme; 
                   options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                   options.DefaultChallengeScheme = "Strava";
                })
                .AddCookie()
                .AddOAuth("Strava", options =>
                {
                    var config = Configuration.GetSection("Strava").Get<StravaConfig>();
                    
                    options.ClientId = config.ClientId;
                    options.ClientSecret = config.ClientSecret;
                    options.CallbackPath = new PathString("/signin-strava");

                    options.AuthorizationEndpoint = "https://www.strava.com/oauth/authorize";
                    options.TokenEndpoint = "https://www.strava.com/oauth/token";
                    options.UserInformationEndpoint = "https://www.strava.com/api/v3/athlete";

                    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "athlete");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "firstname");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "lastname");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Gender, "sex");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Uri, "profile_medium");

                    options.SaveTokens = true;

                    options.Events = new OAuthEvents
                    {
                        OnCreatingTicket = async context =>
                        {
                            var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                            var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, 
                                context.HttpContext.RequestAborted);
                            response.EnsureSuccessStatusCode();

                            var user = JObject.Parse(await response.Content.ReadAsStringAsync());
                            
                            context.RunClaimActions(user);
                        }
                    };
                });

            services.AddDbContext<RazorPagesActivityContext>(options => 
                options.UseSqlite(Configuration.GetConnectionString("ActivityContext")));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseAuthentication();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc();
        }
    }
}
