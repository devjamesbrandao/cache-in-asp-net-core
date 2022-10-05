using System.Reflection;
using Cache.Core.Interfaces;
using Cache.Infrastructure.Data;
using Cache.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace Caching.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
               options.UseSqlServer(
                   Configuration.GetConnectionString("DefaultConnection"),
                   b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
                )
                .LogTo(Console.WriteLine, LogLevel.Information)
            );

            services.AddScoped<ICustomerRepository, CustomerRepository>();

            services.AddControllers();

            services.AddSwaggerGen(x => 
            {
                x.SwaggerDoc("v1", new OpenApiInfo { Title = "Cache InMemoryCache and Redis", Version = "v1" });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                x.IncludeXmlComments(xmlPath);
            });

            //For In-Memory Caching
            services.AddMemoryCache();

            //For Redis Caching
            services.AddStackExchangeRedisCache(options =>
            {
                options.InstanceName = "My Redis Instance";
                options.Configuration = Configuration.GetConnectionString("RedisConnection");
            });
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            using var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();

            var context = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();

            context.Database.EnsureCreated();

            app.UseSwagger();

            app.UseSwaggerUI(c => 
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
                c.RoutePrefix = string.Empty;
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>{ endpoints.MapControllers(); });
        }
    }
}
