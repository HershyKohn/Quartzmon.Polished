using Quartzmon.Plugins.RecentHistory.PostgreSql.Store;

namespace Quartzmon.Plugins.RecentHistory.PostgreSql
{
    public class PostgreSqlExecutionHistoryPlugin : ExecutionHistoryPlugin
    {
        public string ConnectionString { get; set; }
        public string TablePrefix { get; set; } = "QRTZ_";
        public int PurgeIntervalInMinutes { get; set; } = 1;
        public int EntryTtlInMinutes { get; set; } = 5;

        protected override IExecutionHistoryStore CreateExecutionHistoryStore()
        {
            if (StoreType != null && StoreType != typeof(PostgreSqlExecutionHistoryStore))
            {
                throw new InvalidOperationException($"{nameof(PostgreSqlExecutionHistoryPlugin)} is only compatible with the {nameof(PostgreSqlExecutionHistoryStore)} store type");
            }
            
            var store = new PostgreSqlExecutionHistoryStore
            {
                ConnectionString = ConnectionString,
                TablePrefix = TablePrefix,
                PurgeIntervalInMinutes = PurgeIntervalInMinutes,
                EntryTtlInMinutes = EntryTtlInMinutes
            };
            return store;
        }
    }
}
