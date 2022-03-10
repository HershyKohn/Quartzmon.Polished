using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartzmon;

namespace AspNetCore3
{
	public class Program
	{
		public static void Main( string[] args )
		{
			var scheduler = DemoScheduler.Create().Result;

			var host = WebHost.CreateDefaultBuilder( args ).Configure( app =>
				{
					app.UseQuartzmon( new QuartzmonOptions() { Scheduler = scheduler } );

				} ).ConfigureServices( services =>
				{
					services.AddQuartzmon();

				} )
				.Build();

			host.Start();

			while ( !scheduler.IsShutdown )
				Thread.Sleep( 250 );
		}
	}
}
