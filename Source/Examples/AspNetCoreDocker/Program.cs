using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Quartzmon;
using System.Threading;

namespace AspNetCoreDocker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var scheduler = DemoScheduler.Create().Result;

            var host = WebHost.CreateDefaultBuilder(args).Configure(app => 
            {
                app.UseQuartzmon(new QuartzmonOptions() { Scheduler = scheduler });

            }).ConfigureServices(services => 
            {
                services.AddQuartzmon();

            })
            .Build();

            host.Start();

            while (!scheduler.IsShutdown)
                Thread.Sleep(250);
        }

    }
}

/*
docker run -d -p 9999:80 --name myapp quartzmon
docker exec -it myapp sh

docker run -it --rm -p 9999:80 --name myapp quartzmon

docker tag quartzmon docker:5000/quartzmon
docker push docker:5000/quartzmon
*/
