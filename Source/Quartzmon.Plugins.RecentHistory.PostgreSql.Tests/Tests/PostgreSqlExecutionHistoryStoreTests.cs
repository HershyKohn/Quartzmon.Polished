using FluentAssertions;
using NUnit.Framework;

namespace Quartzmon.Plugins.RecentHistory.PostgreSql.Tests.Tests;

public class PostgreSqlExecutionHistoryStoreTests: PostgreSqlExecutionHistoryTestBase
{
    [Test]
    public async Task GetsEntry()
    {
        var store = CreateStore();
        await store.ClearSchedulerData();

        var newEntry = new ExecutionHistoryEntry
        {
            ActualFireTimeUtc = DateTime.UtcNow.AddMinutes(1),
            ScheduledFireTimeUtc = DateTime.UtcNow,
            FinishedTimeUtc = DateTime.UtcNow.AddMinutes(2),
            ExceptionMessage = Guid.NewGuid().ToString(),
            Vetoed = true,
            Recovering= true,
            FireInstanceId = Guid.NewGuid().ToString(),
            Job = Guid.NewGuid().ToString(),
            SchedulerInstanceId = Guid.NewGuid().ToString(),
            SchedulerName = SchedulerName,
            Trigger = Guid.NewGuid().ToString()
        };
        await store.Save(newEntry);
        
        var fetchedEntry = await store.Get(newEntry.FireInstanceId);

        fetchedEntry.Should().NotBeNull();
        fetchedEntry.Should().NotBe(newEntry);
    }
    
    [Test]
    public async Task SavesEntry()
    {
        var store = CreateStore();
        await store.ClearSchedulerData();

        var newEntry = new ExecutionHistoryEntry
        {
            ActualFireTimeUtc = DateTime.UtcNow.AddMinutes(1),
            ScheduledFireTimeUtc = DateTime.UtcNow,
            FinishedTimeUtc = DateTime.UtcNow.AddMinutes(2),
            ExceptionMessage = Guid.NewGuid().ToString(),
            Vetoed = true,
            Recovering = true,
            FireInstanceId = Guid.NewGuid().ToString(),
            Job = Guid.NewGuid().ToString(),
            SchedulerInstanceId = Guid.NewGuid().ToString(),
            SchedulerName = SchedulerName,
            Trigger = Guid.NewGuid().ToString()
        };
        await store.Save(newEntry);

        var fetchedEntry = await store.Get(newEntry.FireInstanceId);

        fetchedEntry.Should().NotBeNull();
        
        fetchedEntry.Should().BeEquivalentTo(newEntry,
            options => options
                .Using<DateTime?>(
                    ctx =>
                        ctx.Subject.Should().BeCloseTo(ctx.Expectation ?? DateTime.MinValue, TimeSpan.FromSeconds(1)))
                .WhenTypeIs<DateTime?>()
                .Using<DateTime>(
                    ctx =>
                        ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromSeconds(1)))
                .WhenTypeIs<DateTime>()
            );
    }

    [Test]
    public async Task PurgesEntry()
    {
        var store = CreateStore();
        await store.ClearSchedulerData();

        var newEntry = new ExecutionHistoryEntry
        {
            ActualFireTimeUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(100)),
            ScheduledFireTimeUtc = DateTime.UtcNow,
            FinishedTimeUtc = DateTime.UtcNow.AddMinutes(2),
            ExceptionMessage = Guid.NewGuid().ToString(),
            Vetoed = true,
            Recovering = true,
            FireInstanceId = Guid.NewGuid().ToString(),
            Job = Guid.NewGuid().ToString(),
            SchedulerInstanceId = Guid.NewGuid().ToString(),
            SchedulerName = SchedulerName,
            Trigger = Guid.NewGuid().ToString()
        };
        await store.Save(newEntry);

        var fetchedEntryBeforePurge = await store.Get(newEntry.FireInstanceId);
        fetchedEntryBeforePurge.Should().NotBeNull();
        fetchedEntryBeforePurge.Should().NotBe(newEntry);

        await store.Purge();

        var fetchedEntryAfterPurge = await store.Get(newEntry.FireInstanceId);
        fetchedEntryAfterPurge.Should().BeNull();
    }

    [Test]
    public async Task FiltersLastOfEveryJob()
    {
        var store = CreateStore();
        await store.ClearSchedulerData();

        var job1 = Guid.NewGuid().ToString();
        for (int i = 0; i < 5; i++)
        {
            var newEntry = new ExecutionHistoryEntry
            {
                ActualFireTimeUtc = DateTime.UtcNow.AddMinutes(4 - i),
                ScheduledFireTimeUtc = DateTime.UtcNow,
                FinishedTimeUtc = DateTime.UtcNow.AddMinutes(5),
                ExceptionMessage = i.ToString(),
                Vetoed = true,
                Recovering = true,
                FireInstanceId = Guid.NewGuid().ToString(),
                Job = job1,
                SchedulerInstanceId = Guid.NewGuid().ToString(),
                SchedulerName = SchedulerName,
                Trigger = Guid.NewGuid().ToString()
            };

            await store.Save(newEntry);
        }

        var job2 = Guid.NewGuid().ToString();
        for (int i = 0; i < 5; i++)
        {
            var newEntry = new ExecutionHistoryEntry
            {
                ActualFireTimeUtc = DateTime.UtcNow.AddMinutes(4 - i),
                ScheduledFireTimeUtc = DateTime.UtcNow,
                FinishedTimeUtc = DateTime.UtcNow.AddMinutes(5),
                ExceptionMessage = i.ToString(),
                Vetoed = true,
                Recovering = true,
                FireInstanceId = Guid.NewGuid().ToString(),
                Job = job2,
                SchedulerInstanceId = Guid.NewGuid().ToString(),
                SchedulerName = SchedulerName,
                Trigger = Guid.NewGuid().ToString()
            };

            await store.Save(newEntry);
        }

        var allEntries = await store.FilterLastOfEveryJob(3);

        var job1Entries = allEntries
            .Where(f => f.Job == job1)
            .ToList();
        job1Entries.Count.Should().Be(3);
        var job1ExceptionMessages = job1Entries.Select(x => x.ExceptionMessage).ToList();
        var expectedExceptionMessages = new List<string>
        {
            "0",
            "1",
            "2",
        };
        job1ExceptionMessages.Should().BeEquivalentTo(expectedExceptionMessages, o=> o.WithStrictOrdering());
        
        var job2Entries = allEntries
            .Where(f => f.Job == job2)
            .ToList();
        job2Entries.Count.Should().Be(3);
        var job2ExceptionMessages = job2Entries.Select(x => x.ExceptionMessage).ToList();
        job2ExceptionMessages.Should().BeEquivalentTo(expectedExceptionMessages, o=> o.WithStrictOrdering());
    }
    
    [Test]
    public async Task FiltersLastOfEveryTrigger()
    {
        var store = CreateStore();
        await store.ClearSchedulerData();
    
        var trigger1 = Guid.NewGuid().ToString();
        for (int i = 0; i < 5; i++)
        {
            var newEntry = new ExecutionHistoryEntry
            {
                ActualFireTimeUtc = DateTime.UtcNow.AddMinutes(4 - i),
                ScheduledFireTimeUtc = DateTime.UtcNow,
                FinishedTimeUtc = DateTime.UtcNow.AddMinutes(5),
                ExceptionMessage = i.ToString(),
                Vetoed = true,
                Recovering = true,
                FireInstanceId = Guid.NewGuid().ToString(),
                Job = Guid.NewGuid().ToString(),
                SchedulerInstanceId = Guid.NewGuid().ToString(),
                SchedulerName = SchedulerName,
                Trigger = trigger1
            };
    
            await store.Save(newEntry);
        }
    
        var trigger2 = Guid.NewGuid().ToString();
        for (int i = 0; i < 5; i++)
        {
            var newEntry = new ExecutionHistoryEntry
            {
                ActualFireTimeUtc = DateTime.UtcNow.AddMinutes(4 - i),
                ScheduledFireTimeUtc = DateTime.UtcNow,
                FinishedTimeUtc = DateTime.UtcNow.AddMinutes(5),
                ExceptionMessage = i.ToString(),
                Vetoed = true,
                Recovering = true,
                FireInstanceId = Guid.NewGuid().ToString(),
                Job = Guid.NewGuid().ToString(),
                SchedulerInstanceId = Guid.NewGuid().ToString(),
                SchedulerName = SchedulerName,
                Trigger = trigger2
            };
    
            await store.Save(newEntry);
        }
    
        var allEntries = await store.FilterLastOfEveryTrigger(3);
    
        var trigger1Entries = allEntries
            .Where(f => f.Trigger == trigger1)
            .ToList();
        trigger1Entries.Count.Should().Be(3);
        var trigger1ExceptionMessages = trigger1Entries.Select(x => x.ExceptionMessage).ToList();
        var expectedExceptionMessages = new List<string>
        {
            "0",
            "1",
            "2",
        };
        trigger1ExceptionMessages.Should().BeEquivalentTo(expectedExceptionMessages, o=> o.WithStrictOrdering());

        var trigger2Entries = allEntries
            .Where(f => f.Trigger == trigger2)
            .ToList();
        trigger2Entries.Count.Should().Be(3);
        var trigger2ExceptionMessages = trigger1Entries.Select(x => x.ExceptionMessage).ToList();
        trigger2ExceptionMessages.Should().BeEquivalentTo(expectedExceptionMessages, o=> o.WithStrictOrdering());
    }
    
    [Test]
    public async Task FiltersLast()
    {
        var store = CreateStore();
        await store.ClearSchedulerData();
    
        var trigger1 = Guid.NewGuid().ToString();
        for (int i = 0; i < 5; i++)
        {
            var newEntry = new ExecutionHistoryEntry
            {
                ActualFireTimeUtc = DateTime.UtcNow.AddMinutes(5 + 4 - i),
                ScheduledFireTimeUtc = DateTime.UtcNow,
                FinishedTimeUtc = DateTime.UtcNow.AddMinutes(5),
                ExceptionMessage = i.ToString(),
                Vetoed = true,
                Recovering = true,
                FireInstanceId = Guid.NewGuid().ToString(),
                Job = Guid.NewGuid().ToString(),
                SchedulerInstanceId = Guid.NewGuid().ToString(),
                SchedulerName = SchedulerName,
                Trigger = trigger1
            };
    
            await store.Save(newEntry);
        }
    
        var trigger2 = Guid.NewGuid().ToString();
        for (int i = 0; i < 5; i++)
        {
            var newEntry = new ExecutionHistoryEntry
            {
                ActualFireTimeUtc = DateTime.UtcNow.AddMinutes(4 - i),
                ScheduledFireTimeUtc = DateTime.UtcNow,
                FinishedTimeUtc = DateTime.UtcNow.AddMinutes(5),
                ExceptionMessage = i.ToString(),
                Vetoed = true,
                Recovering = true,
                FireInstanceId = Guid.NewGuid().ToString(),
                Job = Guid.NewGuid().ToString(),
                SchedulerInstanceId = Guid.NewGuid().ToString(),
                SchedulerName = SchedulerName,
                Trigger = trigger2
            };
    
            await store.Save(newEntry);
        }
    
        var allEntries = await store.FilterLast(8);
    
        var trigger1Entries = allEntries
            .Where(f => f.Trigger == trigger1)
            .ToList();
        trigger1Entries.Count.Should().Be(5);
        var trigger1ExceptionMessages = trigger1Entries.Select(x => x.ExceptionMessage).ToList();
        var expectedExceptionMessages = new List<string>
        {
            "0",
            "1",
            "2",
            "3",
            "4",
        };
        trigger1ExceptionMessages.Should().BeEquivalentTo(expectedExceptionMessages, o=> o.WithStrictOrdering());

        var trigger2Entries = allEntries
            .Where(f => f.Trigger == trigger2)
            .ToList();
        trigger2Entries.Count.Should().Be(3);
        var trigger2ExceptionMessages = trigger2Entries.Select(x => x.ExceptionMessage).ToList();
        var expected2ExceptionMessages = new List<string>
        {
            "0",
            "1",
            "2",
        };
        trigger2ExceptionMessages.Should().BeEquivalentTo(expected2ExceptionMessages, o=> o.WithStrictOrdering());
    }
    
    [Test]
    public async Task IncrementTotalJobsExecuted()
    {
        var store = CreateStore();
        await store.ClearSchedulerData();
        
        int preCount = await store.GetTotalJobsExecuted();
    
        await store.IncrementTotalJobsExecuted();
    
        int afterCount = await store.GetTotalJobsExecuted();
        afterCount.Should().Be(preCount + 1);
    }
    
    [Test]
    public async Task IncrementTotalJobsFailed()
    {
        var store = CreateStore();
        await store.ClearSchedulerData();
    
        var preCount = await store.GetTotalJobsFailed();
    
        await store.IncrementTotalJobsFailed();
    
        var afterCount = await store.GetTotalJobsFailed();
        afterCount.Should().Be(preCount + 1);
    }
    
    [Test]
    public async Task GetTotalJobsExecuted()
    {
        var store = CreateStore();
        await store.ClearSchedulerData();
    
        var count = await store.GetTotalJobsExecuted();
        count.Should().Be(0);
    }
    
    [Test]
    public async Task GetTotalJobsFailed()
    {
        var store = CreateStore();
        await store.ClearSchedulerData();
    
        var count = await store.GetTotalJobsFailed();
        count.Should().Be(0);
    }
}