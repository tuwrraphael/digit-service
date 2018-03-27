using System.IdentityModel.Tokens.Jwt;
using System.IO;
using ButlerClient;
using DigitService.Hubs;
using DigitService.Impl;
using DigitService.Impl.EF;
using DigitService.Models;
using DigitService.Service;
using FileStore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace DigitService
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            if (string.IsNullOrWhiteSpace(hostingEnvironment.WebRootPath))
            {
                hostingEnvironment.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment HostingEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var path = Path.Combine(HostingEnvironment.WebRootPath, "App_Data");
            new FileStore.FileStore(null, path).InitializeAsync().Wait();
            var provider = new PhysicalFileProvider(path);
            var access = new FileStore.FileStore(provider, path);
            services.AddSingleton<IFileProvider>(provider);
            services.AddSingleton<IFileStore>(access);
            services.AddTransient<IDeviceRepository, FileDeviceRepository>();
            services.AddTransient<IDigitLogger, DigitLogger>();
            services.AddTransient<IPushService, NotificationHubPushService>();
            services.Configure<NotificationHubConfig>(Configuration);

            services.Configure<DigitServiceOptions>(o =>
            {
                o.CalendarServiceUrl = Configuration["CalendarServiceUrl"];
                o.DigitClientId = Configuration["DigitClientId"];
                o.DigitClientSecret = Configuration["DigitClientSecret"];
                var endpoint = Configuration["CallbackEndpoint"];
                o.ReminderCallbackUri = $"{endpoint}/api/callback/reminder";
                o.ReminderMaintainanceCallbackUri = $"{endpoint}/api/callback/reminder-maintainance";
                o.ServiceIdentityUrl = Configuration["ServiceIdentityUrl"];
            });

            services.Configure<ButlerOptions>(Configuration);
            services.AddTransient<IButler, Butler>();

            services.AddTransient<ICalendarService, CalendarService>();
            services.AddTransient<IDigitAuthTokenService, DigitAuthTokenService>();
            services.AddTransient<IUserService, UserService>();

            services.AddMvc();
            services.AddMemoryCache();
            services.AddSignalR();
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            services.AddDbContext<DigitServiceContext>(options =>
                options.UseSqlite($"Data Source={HostingEnvironment.WebRootPath}\\App_Data\\digitService.db")
            );
            services.AddTransient<IUserRepository, UserRepository>();

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = Configuration["ServiceIdentityUrl"];
                    options.Audience = "digit";
                    options.RequireHttpsMetadata = false;
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("User", builder =>
                {
                    builder.RequireAuthenticatedUser();
                    builder.RequireClaim("scope", "digit.user");
                });
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Service", builder =>
                {
                    builder.RequireClaim("scope", "digit.service");
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();

            app.UseCors("CorsPolicy");
            app.UseSignalR(routes =>
            {
                routes.MapHub<LogHub>("log");
            });
            app.UseMvc();

        }
    }
}
