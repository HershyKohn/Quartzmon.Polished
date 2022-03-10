using Microsoft.Owin;
using Owin;
using Quartzmon.AspNet;

[assembly: OwinStartup(typeof(Startup))]

namespace Quartzmon.AspNet
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseQuartzmon(new QuartzmonOptions()
            {
                Scheduler = DemoScheduler.Create().Result,

                DefaultDateFormat = "dd.MM.yyyy",
                VirtualPathRoot = "/test",
            });
        }
    }
}