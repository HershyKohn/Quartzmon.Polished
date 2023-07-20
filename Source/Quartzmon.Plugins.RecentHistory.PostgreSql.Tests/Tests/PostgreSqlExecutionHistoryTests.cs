using System.Collections.Specialized;
using FluentAssertions;
using NUnit.Framework;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Quartzmon.Plugins.RecentHistory.PostgreSql.Tests.Jobs;

namespace Quartzmon.Plugins.RecentHistory.PostgreSql.Tests.Tests
{
    public class PostgreSqlExecutionHistoryTests : PostgreSqlExecutionHistoryTestBase, IDisposable
    {
        [Test]
        public async Task SavesJob()
        {
            var sched = await SetupScheduler();
            try
            {
                var jobName = "JB" + Guid.NewGuid();
                await ScheduleTestJob(sched, jobName: jobName, jobGroup: "TEST");

                sched.ListenerManager.AddJobListener(new TestJobListener(), EverythingMatcher<JobKey>.AllJobs());

                var resetEvent = new ManualResetEventSlim();
                TestJobListener.JobWasExecutedCallback = (c, e) => { resetEvent.Set(); };

                var store = CreateStore();
                await store.ClearSchedulerData();

                var lastJob = (await store.FilterLast(1)).FirstOrDefault();
                
                await sched.Start();

                resetEvent.Wait(3 * 1000);
                resetEvent.IsSet.Should().BeTrue();

                var newLastJob = (await store.FilterLast(1)).FirstOrDefault();
                newLastJob.Should().NotBeNull();
                if (lastJob != null)
                {
                    lastJob.FireInstanceId.Should().NotBe(newLastJob.FireInstanceId);
                }

                newLastJob.Job.Should().Be($"TEST.{jobName}");
            }
            finally
            {
                await sched.Shutdown(false);
            }
        }

        [Test]
        public async Task Purges()
        {
            var sched = await SetupScheduler();
            try
            {
                var jobName = "JB" + Guid.NewGuid();
                await ScheduleTestJob(sched, jobName: jobName, jobGroup: "TEST");

                sched.ListenerManager.AddJobListener(new TestJobListener(), EverythingMatcher<JobKey>.AllJobs());

                var resetEvent = new ManualResetEventSlim();
                TestJobListener.JobWasExecutedCallback = (c, e) => { resetEvent.Set(); };

                var store = CreateStore();
                await store.ClearSchedulerData();

                var entryToBePurged = new ExecutionHistoryEntry
                {
                    ActualFireTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(100)),
                    FireInstanceId = Guid.NewGuid().ToString(),
                    Job = "TEST.PurgeMe",
                    SchedulerInstanceId = Guid.NewGuid().ToString(),
                    SchedulerName = SchedulerName,
                    Trigger = Guid.NewGuid().ToString()
                };
                await store.Save(entryToBePurged);

                var lastJobs = await store.FilterLast(2);

                lastJobs.Should().Contain(j => j.FireInstanceId == entryToBePurged.FireInstanceId);

                await sched.Start();

                resetEvent.Wait(3 * 1000);
                resetEvent.IsSet.Should().BeTrue();

                var newLastJobs = await store.FilterLast(2);
                newLastJobs.Should().NotContain(x => x.FireInstanceId == entryToBePurged.FireInstanceId);
            }
            finally
            {
                await sched.Shutdown(false);
            }
        }
        
        [Test]
        public async Task IncrementsTotalJobsExecuted()
        {
            var sched = await SetupScheduler();
            try
            {
                await ScheduleTestJob(sched);

                sched.ListenerManager.AddJobListener(new TestJobListener(), EverythingMatcher<JobKey>.AllJobs());

                var resetEvent = new ManualResetEventSlim();
                TestJobListener.JobWasExecutedCallback = (c, e) =>
                {
                    resetEvent.Set();
                };

                var store = CreateStore();
                await store.ClearSchedulerData();

                int currentCount = await store.GetTotalJobsExecuted();

                await sched.Start();

                resetEvent.Wait(3 * 1000);
                resetEvent.IsSet.Should().BeTrue();

                int newCount = await store.GetTotalJobsExecuted();
                newCount.Should().Be(currentCount + 1);
            }
            finally
            {
                await sched.Shutdown(false);
            }
        }

        [Test]
        public async Task IncrementsTotalJobsFailed()
        {
            var sched = await SetupScheduler();
            try
            {
                await ScheduleTestJob(sched);

                sched.ListenerManager.AddJobListener(new TestJobListener(), EverythingMatcher<JobKey>.AllJobs());

                var resetEvent = new ManualResetEventSlim();
                TestJobListener.JobWasExecutedCallback = (c, e) =>
                {
                    if (e != null)
                    {
                        resetEvent.Set();
                    }
                };
                TestJob.Callback = c => throw new Exception("FAILURE!");

                var store = CreateStore();
                await store.ClearSchedulerData();

                var currentCount = await store.GetTotalJobsFailed();

                await sched.Start();

                resetEvent.Wait(3 * 1000);
                resetEvent.IsSet.Should().BeTrue();

                var newCount = await store.GetTotalJobsFailed();
                newCount.Should().Be(currentCount + 1);
            }
            finally
            {
                await sched.Shutdown(false);
            }
        }

        private async Task<IScheduler> SetupScheduler()
        {
            var properties = new NameValueCollection
            {
                ["quartz.scheduler.instanceName"] = SchedulerName,
                ["quartz.plugin.recentHistory.type"] = typeof(PostgreSqlExecutionHistoryPlugin).AssemblyQualifiedName,
                ["quartz.plugin.recentHistory.connectionString"] = ConnectionString
            };

            var sf = new StdSchedulerFactory(properties);
            var sched = await sf.GetScheduler();
            return sched;
        }

        private async Task ScheduleTestJob(IScheduler sched,
            string jobName = "job1",
            string jobGroup = "group1",
            string triggerName = "trigger1",
            string triggerGroup = "group1")
        {
            var job = JobBuilder.Create<TestJob>()
                .WithIdentity(jobName, jobGroup)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(triggerName, triggerGroup)
                .StartNow()
                .Build();

            await sched.ScheduleJob(job, trigger);
        }

        private void ResetCallbacks()
        {
            TestJobListener.JobWasExecutedCallback = null;
            TestJob.Callback = null;
        }

        public void Dispose()
        {
            ResetCallbacks();
        }
    }
}
