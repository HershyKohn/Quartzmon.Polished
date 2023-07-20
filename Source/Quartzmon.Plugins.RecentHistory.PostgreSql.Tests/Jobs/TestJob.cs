using Quartz;

namespace Quartzmon.Plugins.RecentHistory.PostgreSql.Tests.Jobs
{
    public class TestJob : IJob
    {
        public static Action<IJobExecutionContext> Callback { get; set; }
        
        public Task Execute(IJobExecutionContext context)
        {
            if (Callback != null)
            {
                Callback(context);
            }

            return Task.CompletedTask;
        }
    }
}
