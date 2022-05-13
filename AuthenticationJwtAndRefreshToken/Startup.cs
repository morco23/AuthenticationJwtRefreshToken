
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MorCohen.Data;
using MorCohen.Interfaces;
using MorCohen.Middleware;
using MorCohen.Middlewares;
using MorCohen.Services;
using MorCohen.Models;
using MorCohen.Models.AuthenticateModels;

namespace MorCohen
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Register the given context as a service (scope service)
            services.AddDbContext<ApplicationDbContext>(option => option.UseSqlite(Configuration.GetConnectionString("ApplicationDbContext")));
            services.AddLogging();

            services.AddMvcCore();
            services.AddControllers();

            // Add all requried identity services. (UserManger, RoleManger etc) and configure some setting for register validation.
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            }).AddEntityFrameworkStores<ApplicationDbContext>();

            // Take the app setting section from the config file and map it to AppSetting service.
            var appSettings = Configuration.GetSection("AppSettings").Get<AppSettings>();
            services.AddSingleton(appSettings);

            // Add a service responsible to all things related to accounts - register, login, refresh token. The app only has to use this service for those purpose.
            services.AddScoped<IApplicationAccountManager, ApplicationJwtAccountManager>();
            services.AddScoped<IUsersRepository, UsersRepository>();
            services.AddScoped<IRefreshTokensRepository, RefreshTokenRepository>();
            services.AddScoped<ApplicationRepositories>();

            // 
            services.AddAuthentication(configure =>
            {
                // set the default scheme to use, if not set, we must specity by using attribute on endpoints.
                configure.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                configure.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => options.TokenValidationParameters = ApplicationJwtAccountManager.GetTokenValidationParams(appSettings.Secret));

            services.AddScoped<DbSeeder>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, DbSeeder seeder)
        {
            seeder.Seed().Wait();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<ErrorHandlerMiddleware>();
            app.UseMiddleware<RepositorySaveMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });
        }
    }
}
