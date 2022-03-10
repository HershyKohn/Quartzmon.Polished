using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Quartzmon.AspNetCore
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddQuartzmon();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseQuartzmon(new QuartzmonOptions()
            {
                UseLocalTime = true,
                Scheduler = DemoScheduler.Create().Result,
                VirtualPathRoot = "/test",
            });
        }
    }
}
