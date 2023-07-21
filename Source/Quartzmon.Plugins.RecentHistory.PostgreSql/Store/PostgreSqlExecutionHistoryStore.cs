using Npgsql;
using NpgsqlTypes;
using C = Quartzmon.Plugins.RecentHistory.PostgreSql.Store.PostgreSqlExecutionHistoryStoreConstants;

namespace Quartzmon.Plugins.RecentHistory.PostgreSql.Store
{
    public class PostgreSqlExecutionHistoryStore: IExecutionHistoryStore
    {
        private DateTime _nextPurgeTime = DateTime.UtcNow;
        
        public string SchedulerName { get; set; }
        public string ConnectionString { get; set; }
        public string TablePrefix { get; set; }
        public int PurgeIntervalInMinutes { get; set; }
        public int EntryTtlInMinutes { get; set; }
        
        public async Task<ExecutionHistoryEntry> Get(string fireInstanceId)
        {
            if (fireInstanceId == null) throw new ArgumentNullException(nameof(fireInstanceId));

            string query =
                $"SELECT * FROM {GetTableName(C.TableExecutionHistoryEntries)} \n" +
                $"WHERE {C.ColumnFireInstanceId} = @FireInstanceId";

            var entries = await ExecuteExecutionHistoryEntryQuery(query, c => c.Parameters.AddWithValue("@FireInstanceId", fireInstanceId));
            return entries.FirstOrDefault();
        }

        public async Task Save(ExecutionHistoryEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            if (_nextPurgeTime < DateTime.UtcNow)
            {
                _nextPurgeTime = DateTime.UtcNow.AddMinutes(PurgeIntervalInMinutes);
                await Purge();
            }

            using (var sqlConnection = new NpgsqlConnection(ConnectionString))
            {
                await sqlConnection.OpenAsync();

                string query =
                    $"INSERT INTO {GetTableName(C.TableExecutionHistoryEntries)} \n" +
                    $"({C.ColumnFireInstanceId}, {C.ColumnSchedulerInstanceId}, {C.ColumnSchedulerName}, \n" +
                    $"{C.ColumnJob}, {C.ColumnTrigger}, {C.ColumnScheduledFireTimeUtc}, {C.ColumnActualFireTimeUtc}, \n" +
                    $"{C.ColumnRecovering}, {C.ColumnVetoed}, {C.ColumnFinishedTimeUtc}, {C.ColumnExceptionMessage}) \n" +
                    $"VALUES (@FireInstanceId, @SchedulerInstanceId, @SchedulerName, @Job, @Trigger, @ScheduledFireTimeUtc, \n" +
                    $"@ActualFireTimeUtc, @Recovering, @Vetoed, @FinishedTimeUtc, @ExceptionMessage) \n" +
                    $"ON CONFLICT ({C.ColumnFireInstanceId}) DO UPDATE SET \n" +
                    $"{C.ColumnFireInstanceId} = @FireInstanceId, {C.ColumnSchedulerInstanceId} = @SchedulerInstanceId, {C.ColumnSchedulerName} = @SchedulerName, \n" +
                    $"{C.ColumnJob} = @Job, {C.ColumnTrigger} = @Trigger, {C.ColumnScheduledFireTimeUtc} = @ScheduledFireTimeUtc, {C.ColumnActualFireTimeUtc} = @ActualFireTimeUtc, \n" +
                    $"{C.ColumnRecovering} = @Recovering, {C.ColumnVetoed} = @Vetoed, {C.ColumnFinishedTimeUtc} = @FinishedTimeUtc, {C.ColumnExceptionMessage} = @ExceptionMessage;"
                    ;

                using (var sqlCommand = new NpgsqlCommand(query, sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@FireInstanceId", entry.FireInstanceId);
                    sqlCommand.Parameters.AddWithValue("@SchedulerInstanceId", entry.SchedulerInstanceId);
                    sqlCommand.Parameters.AddWithValue("@SchedulerName", entry.SchedulerName);
                    sqlCommand.Parameters.AddWithValue("@Job", entry.Job);
                    sqlCommand.Parameters.AddWithValue("@Trigger", entry.Trigger);
                    sqlCommand.Parameters.AddWithValue("@ScheduledFireTimeUtc", NpgsqlDbType.Timestamp, entry.ScheduledFireTimeUtc != null ? (object) FixDateTimeKind(entry.ScheduledFireTimeUtc.Value) : DBNull.Value);
                    sqlCommand.Parameters.AddWithValue("@ActualFireTimeUtc", NpgsqlDbType.Timestamp, FixDateTimeKind(entry.ActualFireTimeUtc));
                    sqlCommand.Parameters.AddWithValue("@Recovering", entry.Recovering);
                    sqlCommand.Parameters.AddWithValue("@Vetoed", entry.Vetoed);
                    sqlCommand.Parameters.AddWithValue("@FinishedTimeUtc", NpgsqlDbType.Timestamp, entry.FinishedTimeUtc != null ? (object) FixDateTimeKind(entry.FinishedTimeUtc.Value) : DBNull.Value);
                    sqlCommand.Parameters.AddWithValue("@ExceptionMessage", entry.ExceptionMessage != null ? (object)entry.ExceptionMessage : DBNull.Value);

                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }

        private DateTime FixDateTimeKind(DateTime dt)
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
        }

        public async Task Purge()
        {
            using (var sqlConnection = new NpgsqlConnection(ConnectionString))
            {
                await sqlConnection.OpenAsync();

                string commandText = $"DELETE FROM {GetTableName(C.TableExecutionHistoryEntries)} WHERE {C.ColumnActualFireTimeUtc} < @PurgeThreshold";
                using (var sqlCommand = new NpgsqlCommand(commandText, sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@PurgeThreshold", DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(EntryTtlInMinutes)));

                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryJob(int limitPerJob)
        {
            return FilterLastOf(C.ColumnJob, limitPerJob);
        }

        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryTrigger(int limitPerTrigger)
        {
            return FilterLastOf(C.ColumnTrigger, limitPerTrigger);
        }

        private async Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOf(string columnName, int limit)
        {
            string query =
                $"WITH SELECTION AS ( \n" +
                $"	SELECT *, \n" +
                $"	ROW_NUMBER() OVER(PARTITION BY {columnName} ORDER BY {C.ColumnActualFireTimeUtc} DESC) AS ROW_KEY \n" +
                $"	FROM {GetTableName(C.TableExecutionHistoryEntries)} \n" +
                $"	WHERE {C.ColumnSchedulerName} = @SchedulerName \n" +
                $") \n" +
                $"SELECT * \n" +
                $"FROM SELECTION \n" +
                $"WHERE ROW_KEY <= {limit}";

            return await ExecuteExecutionHistoryEntryQuery(query, c => c.Parameters.AddWithValue("@SchedulerName", SchedulerName));
        }

        public async Task<IEnumerable<ExecutionHistoryEntry>> FilterLast(int limit)
        {
            string query =
                $"SELECT * FROM {GetTableName(C.TableExecutionHistoryEntries)} \n" +
                $"WHERE {C.ColumnSchedulerName} = @SchedulerName \n" +
                $"ORDER BY {C.ColumnActualFireTimeUtc} DESC \n" +
                $"LIMIT {limit}";

            return await ExecuteExecutionHistoryEntryQuery(query, c => c.Parameters.AddWithValue("@SchedulerName", SchedulerName));
        }

        public async Task<int> GetTotalJobsExecuted()
        {
            try
            {
                return (int)await GetStatValue(C.StatTotalJobsExecuted);
            }
            catch (OverflowException)
            {
                /*  should actually log here, but Quartz does not expose its
                    logging facilities to external plugins */
                return -1;
            }
        }

        public async Task<int> GetTotalJobsFailed()
        {
            try
            {
                return (int)await GetStatValue(C.StatTotalJobsFailed);
            }
            catch (OverflowException)
            {
                /*  should actually log here, but Quartz does not expose its
                    logging facilities to external plugins */
                return -1;
            }
        }

        public Task IncrementTotalJobsExecuted()
        {
            return IncrementStatValue(C.StatTotalJobsExecuted);
        }

        public Task IncrementTotalJobsFailed()
        {
            return IncrementStatValue(C.StatTotalJobsFailed);
        }
        
        public async Task ClearSchedulerData()
        {
            using (var sqlConnection = new NpgsqlConnection(ConnectionString))
            {
                await sqlConnection.OpenAsync();

                string commandText = 
                    $"DELETE FROM {GetTableName(C.TableExecutionHistoryEntries)} WHERE {C.ColumnSchedulerName} = @SchedulerName;\n" +
                    $"DELETE FROM {GetTableName(C.TableExecutionHistoryStats)} WHERE {C.ColumnSchedulerName} = @SchedulerName;";
                using (var sqlCommand = new NpgsqlCommand(commandText, sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@SchedulerName", SchedulerName);

                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }
        
        protected async Task<long> GetStatValue(string statName)
        {
            if (statName == null) throw new ArgumentNullException(nameof(statName));

            using (var sqlConnection = new NpgsqlConnection(ConnectionString))
            {
                await sqlConnection.OpenAsync();

                string query =
                    $"SELECT {C.ColumnStatValue} FROM {GetTableName(C.TableExecutionHistoryStats)} \n" +
                    $"WHERE {C.ColumnStatName} = @StatName AND {C.ColumnSchedulerName} = @SchedulerName";

                using (var sqlCommand = new NpgsqlCommand(query, sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@SchedulerName", SchedulerName);
                    sqlCommand.Parameters.AddWithValue("@StatName", statName);

                    var scalar = await sqlCommand.ExecuteScalarAsync();
                    if (scalar != null)
                    {
                        return (long)scalar;
                    }

                    return 0;
                }
            }
        }

        protected async Task IncrementStatValue(string statName)
        {
            if (statName == null) throw new ArgumentNullException(nameof(statName));

            using (var sqlConnection = new NpgsqlConnection(ConnectionString))
            {
                await sqlConnection.OpenAsync();

                string query =
                    $"INSERT INTO {GetTableName(C.TableExecutionHistoryStats)} AS Target \n" +
                    $"({C.ColumnSchedulerName}, {C.ColumnStatName}, {C.ColumnStatValue}) \n" +
                    $"VALUES (@SchedulerName, @StatName, 1) \n"+
                    $"ON CONFLICT ({C.ColumnStatName}, {C.ColumnSchedulerName}) DO UPDATE SET \n" +
                    $"{C.ColumnStatValue} = EXCLUDED.{C.ColumnStatValue} + 1;"
                    ;

                using (var sqlCommand = new NpgsqlCommand(query, sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@SchedulerName", SchedulerName);
                    sqlCommand.Parameters.AddWithValue("@StatName", statName);

                    try
                    {
                        await sqlCommand.ExecuteNonQueryAsync();
                    }
                    catch (NpgsqlException e) when (e.ErrorCode == 22003) // SQL overflow exception
                    {
                        /*  should actually log here, but Quartz does not expose its
                            logging facilities to external plugins */
                    }
                }
            }
        }

        private string GetTableName(string tableNameWithoutPrefix)
        {
            if (tableNameWithoutPrefix == null)
            {
                throw new ArgumentNullException(nameof(tableNameWithoutPrefix));
            }

            return $"{TablePrefix}{tableNameWithoutPrefix}";
        }
        
        private async Task<List<ExecutionHistoryEntry>> ExecuteExecutionHistoryEntryQuery(string query, Action<NpgsqlCommand> sqlCommandModifier = null)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            using (var sqlConnection = new NpgsqlConnection(ConnectionString))
            {
                await sqlConnection.OpenAsync();

                using (var sqlCommand = new NpgsqlCommand(query, sqlConnection))
                {
                    sqlCommandModifier?.Invoke(sqlCommand);

                    using (var sqlDataReader = await sqlCommand.ExecuteReaderAsync())
                    {
                        var entries = new List<ExecutionHistoryEntry>();

                        while (await sqlDataReader.ReadAsync())
                        {
                            var entry = new ExecutionHistoryEntry();
                            await HydrateExecutionHistoryEntry(sqlDataReader, entry);
                            entries.Add(entry);
                        }

                        return entries;
                    }
                }
            }
        }
        
        private async Task HydrateExecutionHistoryEntry(NpgsqlDataReader sqlDataReader, ExecutionHistoryEntry entry)
        {
            var r = sqlDataReader;

            entry.ActualFireTimeUtc = r.GetDateTime(r.GetOrdinal(C.ColumnActualFireTimeUtc));
            entry.ExceptionMessage = await r.IsDBNullAsync(r.GetOrdinal(C.ColumnExceptionMessage)) ?
                null : r.GetString(r.GetOrdinal(C.ColumnExceptionMessage));
            entry.FinishedTimeUtc = await r.IsDBNullAsync(r.GetOrdinal(C.ColumnFinishedTimeUtc)) ?
                (DateTime?)null : r.GetDateTime(r.GetOrdinal(C.ColumnFinishedTimeUtc));
            entry.FireInstanceId = r.GetString(r.GetOrdinal(C.ColumnFireInstanceId));
            entry.Job = r.GetString(r.GetOrdinal(C.ColumnJob));
            entry.Recovering = r.GetBoolean(r.GetOrdinal(C.ColumnRecovering));
            entry.ScheduledFireTimeUtc = await r.IsDBNullAsync(r.GetOrdinal(C.ColumnScheduledFireTimeUtc)) ?
                (DateTime?)null : r.GetDateTime(r.GetOrdinal(C.ColumnScheduledFireTimeUtc));
            entry.SchedulerInstanceId = r.GetString(r.GetOrdinal(C.ColumnSchedulerInstanceId));
            entry.SchedulerName = r.GetString(r.GetOrdinal(C.ColumnSchedulerName));
            entry.Trigger = r.GetString(r.GetOrdinal(C.ColumnTrigger));
            entry.Vetoed = r.GetBoolean(r.GetOrdinal(C.ColumnVetoed));
        }
    }
}