using Microsoft.Extensions.Configuration;
using Quartzmon.Plugins.RecentHistory.PostgreSql.Store;

namespace Quartzmon.Plugins.RecentHistory.PostgreSql.Tests.Tests;

public abstract class PostgreSqlExecutionHistoryTestBase
{
    protected string ConnectionString { get; }
    protected string SchedulerName { get; }
    protected string TablePrefix { get; }
    private int  PurgeIntervalInMinutes{ get; }
    private int  EntryTtlInMinutes{ get; }

    private const string AppSettingsSectionName = "PostgreSqlHistory";
    
    protected PostgreSqlExecutionHistoryTestBase()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .Build();

        ConnectionString = config[$"{AppSettingsSectionName}:ConnectionString"];
        SchedulerName = config[$"{AppSettingsSectionName}:SchedulerName"];
        TablePrefix = config[$"{AppSettingsSectionName}:TablePrefix"];
        PurgeIntervalInMinutes = int.Parse(config[$"{AppSettingsSectionName}:PurgeIntervalInMinutes"]);
        EntryTtlInMinutes = int.Parse(config[$"{AppSettingsSectionName}:EntryTtlInMinutes"]);
    }
    
    protected PostgreSqlExecutionHistoryStore CreateStore()
    {
        var store = new PostgreSqlExecutionHistoryStore
        {
            SchedulerName = SchedulerName,
            ConnectionString = ConnectionString,
            TablePrefix = TablePrefix,
            PurgeIntervalInMinutes = PurgeIntervalInMinutes,
            EntryTtlInMinutes = EntryTtlInMinutes
        };
        return store;
    }
}