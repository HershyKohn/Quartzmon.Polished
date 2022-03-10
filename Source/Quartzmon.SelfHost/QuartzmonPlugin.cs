using Quartz;
using Quartz.Spi;
using System;
using System.Threading;
using System.Threading.Tasks;

#if (NETSTANDARD || NETCOREAPP)
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
#endif

#if NETCOREAPP
using Microsoft.AspNetCore.Builder;
#endif

namespace Quartzmon.SelfHost
{
    public class QuartzmonPlugin : ISchedulerPlugin
    {
        public string Url { get; set; }

        public string DefaultDateFormat { get; set; }
        public string DefaultTimeFormat { get; set; }

        public bool UseLocalTime { get; set; }

        public string Logo { get; set; }
        public string ProductName { get; set; }

        private IScheduler _scheduler;
        private IDisposable _webApp;

        public Task Initialize(string pluginName, IScheduler scheduler, CancellationToken cancellationToken = default(CancellationToken))
        {
            _scheduler = scheduler;
            return Task.FromResult(0);
        }

#if NETSTANDARD
        public Task Start(CancellationToken cancellationToken = default(CancellationToken))
        {
            var host = Microsoft.AspNetCore.WebHost.CreateDefaultBuilder().Configure(app => {
                app.UseQuartzmon(CreateQuartzmonOptions());
            }).ConfigureServices(services => {
                services.AddQuartzmon();
            })
            .ConfigureLogging(logging => {
                logging.ClearProviders();
            })
            .UseUrls(Url)
            .Build();

            _webApp = host;

            return host.StartAsync();
        }
#endif

#if NETCOREAPP
        public Task Start(CancellationToken cancellationToken = default(CancellationToken))
        {
            var host = Microsoft.AspNetCore.WebHost.CreateDefaultBuilder().Configure(app => {
                app.UseStaticFiles();
                app.UseRouting();
                app.UseQuartzmon(CreateQuartzmonOptions());
            }).ConfigureServices(services => {
                services.AddQuartzmon();
            })
            .ConfigureLogging(logging => {
                logging.ClearProviders();
            })
            .UseUrls(Url)
            .Build();

            _webApp = host;

            return host.StartAsync();
        }
#endif

#if NETFRAMEWORK
        public Task Start(CancellationToken cancellationToken = default(CancellationToken))
        {
            _webApp = Microsoft.Owin.Hosting.WebApp.Start(Url, app => {
                app.UseQuartzmon(CreateQuartzmonOptions());
            });
            return Task.FromResult(0);
        }
#endif
        public Task Shutdown(CancellationToken cancellationToken = default(CancellationToken))
        {
            _webApp.Dispose();
            return Task.FromResult(0);
        }

        private QuartzmonOptions CreateQuartzmonOptions()
        {
            var options = new QuartzmonOptions()
            {
                Scheduler = _scheduler,
            };

            if (!string.IsNullOrEmpty(DefaultDateFormat))
                options.DefaultDateFormat = DefaultDateFormat;
            if (!string.IsNullOrEmpty(DefaultTimeFormat))
                options.DefaultTimeFormat = DefaultTimeFormat;
            if (!string.IsNullOrEmpty(Logo))
                options.Logo = Logo;
            if (!string.IsNullOrEmpty(ProductName))
                options.ProductName = ProductName;

            if (UseLocalTime)
                options.UseLocalTime = UseLocalTime;

            return options;
        }

    }
}
