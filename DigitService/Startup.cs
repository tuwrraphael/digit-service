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
using CalendarService.Client;
using System;
using OAuthApiClient;
using Microsoft.AspNetCore.Mvc;
using TravelService.Client;
using DigitService.Controllers;
using DigitPushService.Client;
using Digit.Focus.Service;
using Digit.Abstractions.Service;
using Microsoft.AspNetCore.SignalR;
using Digit.DeviceSynchronization;
using System.Reflection;
using System.Threading.Tasks;
using Digit.DeviceSynchronization.Impl;

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
            var connectionString = $"Data Source={HostingEnvironment.WebRootPath}\\App_Data\\digitService.db";
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            new FileStore.FileStore(null, path).InitializeAsync().Wait();
            var provider = new PhysicalFileProvider(path);
            var access = new FileStore.FileStore(provider, path);
            services.AddSingleton<IFileProvider>(provider);
            services.AddSingleton<IFileStore>(access);
            services.AddTransient<ILogBackend, FileLogBackend>();
            services.AddTransient<IDigitLogger, DigitLogger>();
            services.AddTransient<IDeviceService, DeviceService>();
            services.AddTransient<IDeviceRepository, DeviceRepository>();

            services.Configure<DigitServiceOptions>(o =>
            {
                o.DigitClientId = Configuration["DigitClientId"];
                o.DigitClientSecret = Configuration["DigitClientSecret"];
                var endpoint = Configuration["CallbackEndpoint"];
                o.ReminderCallbackUri = $"{endpoint}/api/callback/reminder";
                o.NotifyUserCallbackUri = $"{endpoint}/api/callback/notify-user";
                o.ReminderMaintainanceCallbackUri = $"{endpoint}/api/callback/reminder-maintainance";
                o.ServiceIdentityUrl = Configuration["ServiceIdentityUrl"];
            });

            var authenticationProviderBuilder = services.AddBearerTokenAuthenticationProvider("digitServiceToken")
                .UseMemoryCacheTokenStore()
                .UseClientCredentialsTokenStrategy(new ClientCredentialsConfig()
                {
                    ClientId = Configuration["DigitClientId"],
                    ClientSecret = Configuration["DigitClientSecret"],
                    Scopes = "calendar.service travel.service push.service",
                    ServiceIdentityBaseUrl = new Uri(Configuration["ServiceIdentityUrl"])
                });
            services.AddCalendarServiceClient(new Uri(Configuration["CalendarServiceUrl"]), authenticationProviderBuilder);
            services.AddTravelServiceClient(new Uri(Configuration["TravelServiceUrl"]), authenticationProviderBuilder);
            services.AddDigitPushServiceClient(new Uri(Configuration["PushServiceUrl"]), authenticationProviderBuilder);

            services.Configure<ButlerOptions>(Configuration);
            services.AddTransient<IButler, Butler>();

            services.AddTransient<IUserService, UserService>();
            services.AddTransient<ILocationStore, LocationStore>();
            services.AddTransient<IFocusService, FocusService>();
            services.AddTransient<IFocusUpdateService, FocusUpdateService>();
            services.AddTransient<IFocusSubscriber, SignalRFocusSubscriber>();
            services.AddTransient<IFocusSubscriber, DeviceSyncFocusSubscriber>();
            services.AddTransient<IFocusStore, FocusStore>();
            services.AddTransient<IFocusCalendarSyncService, FocusCalendarSyncService>();
            services.AddTransient<ILocationService, LocationService>();
            services.AddTransient<IPlannerService, PlannerService>();
            services.AddDeviceSynchronization(builder => builder.UseSqlite(connectionString,
                                sql => sql.MigrationsAssembly(migrationsAssembly)));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2).AddJsonOptions(v =>
            {
                v.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
            });
            services.AddMemoryCache();
            services.AddSignalR();
            services.AddSingleton<IUserIdProvider, SubUserIdProvider>();
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder
                    .WithOrigins("http://localhost:4200", "https://digit.kesal.at")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            services.AddDbContext<DigitServiceContext>(options =>
                options.UseSqlite(connectionString)
            );
            services.AddTransient<IUserRepository, UserRepository>();

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = Configuration["ServiceIdentityUrl"];
                    options.Audience = "digit";
                    options.RequireHttpsMetadata = false;

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var requestPath = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) &&
                                (requestPath.StartsWithSegments("/hubs")))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("User", builder =>
                {
                    builder.RequireAuthenticatedUser();
                    builder.RequireClaim("scope", "digit.user");
                });
                options.AddPolicy("UserDevice", builder =>
                {
                    builder.RequireAuthenticatedUser();
                    // TODO create digit.userdevice scope
                    builder.RequireClaim("scope", "digit.user");
                });
                options.AddPolicy("Service", builder =>
                {
                    builder.RequireClaim("scope", "digit.service");
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetService<DigitServiceContext>())
                {
                    context.Database.Migrate();
                }
                using (var context = serviceScope.ServiceProvider.GetService<DeviceSynchronizationContext>())
                {
                    context.Database.Migrate();
                }
            }
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();

            app.UseCors("CorsPolicy");
            app.UseSignalR(routes =>
            {
                routes.MapHub<LogHub>("/hubs/log");
                routes.MapHub<FocusHub>("/hubs/focus");
            });
            app.UseMvc();

        }
    }
}
