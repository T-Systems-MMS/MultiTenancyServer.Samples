using System;
using System.Reflection;
using IdentityServerWithAspIdAndEF.Data;
using IdentityServerWithAspIdAndEF.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerWithAspIdAndEF
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IHostingEnvironment Environment { get; }

        public Startup(IConfiguration configuration, IHostingEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            var migrationsAssembly = typeof(ApplicationDbContext).GetTypeInfo().Assembly.GetName().Name;

            services.AddDbContext<ApplicationDbContext>(options => options
                .UseSqlite(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly))
                //.EnableSensitiveDataLogging()
                );

            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/AccessDenied";
                options.Cookie.Name = "login.idp.de"; 
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.LoginPath = "/Login";
                options.LogoutPath = "/Logout";
                options.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
                options.SlidingExpiration = true;
            });

            services.AddMultiTenancy<ApplicationTenant, string>()
                .AddDomainParser()
                .AddEntityFrameworkStore<ApplicationDbContext, ApplicationTenant, string>()
                                .WithPerTenantOptions<CookiePolicyOptions>((options, tenant) =>
                {
                    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                    options.CheckConsentNeeded = context => true;
                    options.ConsentCookie.Name = tenant.Id + "-consent";
                    options.MinimumSameSitePolicy = SameSiteMode.None;
                })
                .WithPerTenantOptions<CookieAuthenticationOptions>((options, tenantInfo) =>
                {
                    //Since we are using the route strategy configure each tenant
                    // to have a different cookie name and adjust the paths.
                    options.Cookie.Name = $"{tenantInfo.Id}_{options.Cookie.Name}";
                    //options.LoginPath = $"/{tenantInfo.DisplayName}/Home/Login";
                    //options.LogoutPath = $"/{tenantInfo.DisplayName}";
                    options.Cookie.Path = "";//$"/{tenantInfo.DisplayName}";
                });

            // workaround for https://github.com/aspnet/AspNetCore/issues/5828
            services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
            {
                // This will result in a path of /_tenant_/Identity/Account/Login
                options.LoginPath = $"{options.Cookie.Path}{options.LoginPath}";
            });

            //IdentityModelEventSource.ShowPII = true;
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddMvc(options =>
                {
                    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddAntiforgery();

            services.Configure<IISOptions>(iis =>
                {
                    iis.AuthenticationDisplayName = "Windows";
                    iis.AutomaticAuthentication = false;
                });

            var builder = services.AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                })
                .AddAspNetIdentity<ApplicationUser>()
                // this adds the config data from DB (clients, resources)
                .AddConfigurationStore<ApplicationDbContext>(options =>
                {
                    options.ConfigureDbContext = b =>
                        b.UseSqlite(connectionString,
                            sql => sql.MigrationsAssembly(migrationsAssembly));
                })
                // this adds the operational data from DB (codes, tokens, consents)
                .AddOperationalStore<ApplicationDbContext>(options =>
                {
                    options.ConfigureDbContext = b =>
                        b.UseSqlite(connectionString,
                            sql => sql.MigrationsAssembly(migrationsAssembly));

                    // this enables automatic token cleanup. this is optional.
                    options.EnableTokenCleanup = true;
                    // options.TokenCleanupInterval = 15; // frequency in seconds to cleanup stale grants. 15 is useful during debugging
                });

            if (Environment.IsDevelopment())
            {
                builder.AddDeveloperSigningCredential();
            }
            else
            {
                throw new Exception("need to configure key material");
            }

            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    // register your IdentityServer with Google at https://console.developers.google.com
                    // enable the Google+ API
                    // set the redirect URI to http://localhost:port/signin-google
                    options.ClientId = "copy client ID from Google here";
                    options.ClientSecret = "copy client secret from Google here";
                //})
                //.AddOpenIdConnect("oidc", "OpenID Connect", options =>
                //{
                //    options.Authority = "https://demo.identityserver.io/";
                //    options.ClientId = "implicit";
                //    options.SaveTokens = true;

                //    options.TokenValidationParameters = new TokenValidationParameters
                //    {
                //        NameClaimType = "name",
                //        RoleClaimType = "role"
                //    };
                });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseMultiTenancy<ApplicationTenant, string>();

            app.UseIdentityServer();

            app.UseAuthentication();

            app.UseMvcWithDefaultRoute();
        }
    }
}
