using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExtremeAspNetCoreResponsiveApp.Model;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using DevExtremeAspNetCoreResponsiveApp.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Extensions;

namespace DevExtremeAspNetCoreResponsiveApp
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
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            
            //Conexion a la Base de Datos
            services.AddDbContext<PayLotsDBContext>(options => options.UseSqlServer(Configuration.GetConnectionString("PayLotsConnection")));

            //services.AddDbContext<IdentityContext>(options => options.UseSqlServer(Configuration.GetConnectionString("IdentityConnection")));

            //Configuracion de Identity
            services.AddIdentity<AppUser, IdentityRole>(options =>
                    {
                        options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
                        options.Password.RequireDigit = true;
                        options.Password.RequiredLength = 8;
                        options.Password.RequireNonAlphanumeric = true;
                        options.Password.RequireUppercase = true;
                        options.Password.RequireLowercase = true;
                        options.User.RequireUniqueEmail = true;
                        options.SignIn.RequireConfirmedEmail = true;
                        options.Lockout.MaxFailedAccessAttempts = 5;
                    })
                .AddEntityFrameworkStores<PayLotsDBContext>().AddDefaultTokenProviders().AddDefaultUI();

            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
            //.AddRazorPagesOptions(options =>
            //{
            //options.AllowAreas = true;
            //options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
            //options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
            //});

            services.ConfigureApplicationCookie(opts =>
            {
                opts.LoginPath = $"/Identity/Account/Login";
                opts.AccessDeniedPath = $"/Identity/Account/Login";
                opts.Events = new CookieAuthenticationEvents()
                {
                    OnRedirectToLogin = redirectContext =>
                    {
                        string redirecturi = redirectContext.RedirectUri;
                        UriHelper.FromAbsolute(
                       redirecturi,
                       out string scheme,
                       out HostString host,
                       out PathString path,
                       out QueryString query,
                       out FragmentString fragment);

                        redirecturi = UriHelper.BuildAbsolute(scheme, host, path);

                        redirectContext.Response.Redirect(redirecturi);

                        return Task.CompletedTask;
                    }
                };


            }
            );


            //Instancia el UserHelper como Servicio
            services.AddScoped<IUserHelper, UserHelper>();
            
            //Instancia el servicio para enviar correo
            services.AddScoped<IEmailSender, MailHelper>();
           
            

            //Configuracion de JSON para evitar conflictos con el nombre de los campos
            services.AddMvc()
                .AddJsonOptions(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver())
                .AddJsonOptions(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore)
                .AddRazorPagesOptions(options => 
                {
                    options.AllowAreas = true;
                    options.Conventions.AuthorizeFolder("/");
                    options.Conventions.AllowAnonymousToPage("/Identity/Account/Login");
                    
                }
                )
                
                ;
            //Requiere que el usuario este autenticado para poder acceder a cualquier pagina
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                options.AddPolicy("AdminPolicy", config => {
                    config.RequireAuthenticatedUser().Build();
             });
            });

           

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            
                
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

   
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseCookiePolicy();

            
            // app.UseMvc(routes =>
            //{
            //    routes.MapRoute(
            //        name: "default",
            //        template: "{controller=Home}/{action=Index}/{id?}")
            //        ;
            //});

            app.UseMvc();
        }
    }
}
