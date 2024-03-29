using System;
using System.Extensions;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ExtendedStravaClient;

namespace StravaStatisticsAnalyzerConsole
{
    // TODO - change to use stored proc where possible
    // TODO - change to use parameters to protect against SQLInjections 
    internal class DBFacade : IDBFacade
    {
        private string connectionString_;
        private bool createNewTables_= false;
        private HashSet<long> insertedSegments_;

        public List<long> ActivityIds 
        {
            get
            {
                return SqlQuery<long>($"SELECT id FROM {Configuration.MySQL.Tables.Activity.NAME}",
                    "Unable to fetch activity IDs.",
                    r => r.GetInt64("id")
                );
            }
        }

        public bool Initialize()
        {
            if(InitializeConnection())
            {
                return InitializeTables();
            }
            insertedSegments_ = new HashSet<long>(SqlQuery<long>($"SELECT id FROM {Configuration.MySQL.Tables.Segment.NAME}",
                "Unable to fetch segment IDs.",
                r => r.GetInt64("id")));
            return false;
        }

        public void Shutdown()
        {
            Console.WriteLine("Closing connection...");
        }

        #region StravaFacade 

        public int GetLastUpdate()
        {
            using(var connection = new MySqlConnection(connectionString_))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT date_time FROM {Configuration.MySQL.Tables.Activity.NAME} ORDER BY date_time DESC";
                using(var reader = command.ExecuteReader())
                {
                    if(reader.Read())
                    {
                        var dateTime = reader.GetDateTime(0);
                        reader.Close();
                        return dateTime.ToEpoch();
                    }
                }
            }
            return -1;
        }

        public Dictionary<string,List<IRideEffort>> GetSegmentEffortsForActivity(string activityName, int? maxInterval)
        {
            var ids = GetSegmentIdsForActivity(activityName);
            var segmentNamesById = SqlQuery<long,string>
            (
                $"SELECT name,id FROM segment WHERE id in ({String.Join(",", ids)})",
                "Unable to fetch names for segments",
                r => r.GetInt64("id"),
                r => r.GetString("name")
            );
            var res = new Dictionary<string,List<IRideEffort>>();
            foreach(var id in ids)
            {
                var segmentEfforts = GetSegmentEffortsForSegment(id, maxInterval);
                if(segmentEfforts.Count > 0)
                {
                    res.Add(segmentNamesById[id], segmentEfforts);
                }
                else
                {
                    Console.WriteLine($"Failed to get segment efforts for {segmentNamesById[id]}");
                }
            }
            return res;
        }

        public Dictionary<string,List<IRideEffort>> GetSegmentEffortsForActivity(string activityName, DateTime? start, DateTime? end)
        {
            var ids = GetSegmentIdsForActivity(activityName);
            var segmentNamesById = SqlQuery<long,string>
            (
                $"SELECT name,id FROM segment WHERE id in ({String.Join(",", ids)})",
                "Unable to fetch names for segments",
                r => r.GetInt64("id"),
                r => r.GetString("name")
            );
            var res = new Dictionary<string,List<IRideEffort>>();
            foreach(var id in ids)
            {
                var segmentEfforts = GetSegmentEffortsForSegment(id, start, end);
                if(segmentEfforts.Count > 0)
                {
                    res.Add(segmentNamesById[id], segmentEfforts);
                }
                else
                {
                    Console.WriteLine($"Failed to get segment efforts for {segmentNamesById[id]}");
                }
            }
            return res;
        }


        private List<IRideEffort> GetSegmentEffortsForSegment(long segmentId, int? max)
        {
            return SqlQuery<IRideEffort>(
                $@"SELECT id,moving_time,distance,date_time FROM 
                    segment_effort WHERE segment_id = {segmentId} ORDER BY date_time DESC {(max.HasValue ? $"LIMIT {max.Value}" : "")};",
                $"Unable to fetch segment efforts for segment {segmentId}",
                (reader) => 
                {
                    var movingTime = reader.GetInt32("moving_time");
                    var distance = reader.GetDouble("distance");
                    var speed = distance / movingTime;
                    return new RideEffort(reader.GetInt64("id"), speed, movingTime, reader.GetDateTime("date_time"));
                }
            );
        }

        private List<IRideEffort> GetSegmentEffortsForSegment(long segmentId, DateTime? start, DateTime? end)
        {
            return SqlQuery<IRideEffort>(
                $@"SELECT id,moving_time,distance,date_time FROM 
                    segment_effort WHERE segment_id = {segmentId}
                    {(start.HasValue ? $" AND date_time > '{start:yyyy-MM-dd HH:mm:ss}'" : "")}
                    {(end.HasValue ? $" AND date_time < '{end:yyyy-MM-dd HH:mm:ss}'" : "")}",
                $"Unable to fetch segment efforts for segment {segmentId}",
                (reader) => 
                {
                    var movingTime = reader.GetInt32("moving_time");
                    var distance = reader.GetDouble("distance");
                    var speed = distance / movingTime;
                    return new RideEffort(reader.GetInt64("id"), speed, movingTime, reader.GetDateTime("date_time"));
                }
            );
        }

        public List<long> GetSegmentIdsForActivity(string activityName)
        {
            var segmentsWithCount = SqlQuery<(long id, int count)>(
                $@"SELECT id,count FROM  
                    ((SELECT segment_id AS id,count(*) AS count FROM segment_effort WHERE activity_id IN 
                        (SELECT id from activity WHERE name like @activityname
                    )
                    group by segment_id) as segment_by_count) ORDER BY count DESC;",
                $"Unable to fetch segments for @activityname",
                (reader => (reader.GetInt64("id"), reader.GetInt32("count"))),
                new List<(string param, object val)>()
                {
                    ("@activityname", activityName)
                }
            );
            var averageCount = segmentsWithCount.Select(i => i.count).Average();
            return segmentsWithCount.Where(i => i.count >= averageCount).Select(i => i.id).ToList();
        }       

        public List<long> GetActivityIds(string activityName, DateTime? start, DateTime? end)
        {
            if(activityName == null && !start.HasValue && !end.HasValue)
            {
                return ActivityIds;
            }
            return SqlQuery<long>(
                $@"SELECT id FROM activity WHERE
                    {(activityName != null ? $"name LIKE @activityname" : "")}
                    {(start.HasValue ? $" {(activityName != null ? "AND" : "")} date_time > '{start:yyyy-MM-dd HH:mm:ss}'" : "")}
                    {(end.HasValue ? $" {(activityName != null || start.HasValue ? "AND" : "")}date_time < '{end:yyyy-MM-dd HH:mm:ss}'" : "")}",
                $"Unable to read '{activityName}' activities",
                (reader => reader.GetInt64("id")),
                activityName != null ? 
                    new List<(string param, object val)>()
                    {
                        ("@activityname",activityName)
                    } : 
                    null
            );
        }

        public List<IRideEffort> GetActivityEfforts(string activityName, int? maxInterval)
        {
            return SqlQuery<IRideEffort>(
                $@"SELECT id,avg_speed,moving_time,date_time FROM activity WHERE name LIKE @activityname 
                    ORDER BY date_time DESC {(maxInterval.HasValue ? $"LIMIT {maxInterval.Value}" : "")}",
                $"Unable to read '{activityName}' activities",
                (reader => new RideEffort(reader.GetInt64("id"), reader.GetDouble("avg_speed"), reader.GetInt32("moving_time"),reader.GetDateTime("date_time"))),
                new List<(string param, object val)>()
                {
                    ("@activityname", activityName)
                }
            );
        }

        public List<IRideEffort> GetActivityEfforts(string activityName, DateTime? start, DateTime? end)
        {
            return SqlQuery<IRideEffort>(
                $@"SELECT id,avg_speed,moving_time,date_time FROM activity WHERE name LIKE @activityname
                    {(start.HasValue ? $" AND date_time > '{start:yyyy-MM-dd HH:mm:ss}'" : "")}
                    {(end.HasValue ? $" AND date_time < '{end:yyyy-MM-dd HH:mm:ss}'" : "")}",
                $"Unable to read '{activityName}' activities",
                (reader => new RideEffort(reader.GetInt64("id"), reader.GetDouble("avg_speed"), reader.GetInt32("moving_time"),reader.GetDateTime("date_time"))),
                new List<(string param, object val)>()
                {
                    ("@activityname", activityName),
                }
            );
        }

        private List<T> SqlQuery<T>(string commandText, string errorMessage, Func<MySqlDataReader,T> createElementFunc, List<(string param, object val)> parameters = null)
        {
            var list = new List<T>();
            using(var connection = new MySqlConnection(connectionString_))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = commandText;
                if(parameters != null)
                {
                    foreach(var parameter in parameters)
                    {
                        command.Parameters.AddWithValue(parameter.param, parameter.val);
                    }
                }
                try
                {
                    using(var reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            list.Add(createElementFunc(reader));
                        }
                    }
                }
                catch(MySqlException ex)
                {
                    Console.WriteLine(errorMessage);
                    Console.WriteLine($"Command - {command.CommandText}");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
            return list;
        }

        private Dictionary<TKey,TValue> SqlQuery<TKey,TValue>(string commandText, string errorMessage, 
            Func<MySqlDataReader,TKey> createKeyFunc, Func<MySqlDataReader,TValue> createValueFunc,
            List<(string param, object val)> parameters = null)
        {
            var dict = new Dictionary<TKey,TValue> ();
            using(var connection = new MySqlConnection(connectionString_))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = commandText;
                if(parameters != null)
                {
                    foreach(var parameter in parameters)
                    {
                        command.Parameters.AddWithValue(parameter.param, parameter.val);
                    }
                }
                try 
                {
                    using(var reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            dict[createKeyFunc(reader)] = createValueFunc(reader);
                        }
                    }
                }
                catch(MySqlException ex)
                {
                    Console.WriteLine(errorMessage);
                    Console.WriteLine($"Command - {command.CommandText}");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
            return dict;
        }

        #endregion

        #region Insert

        public bool Insert(List<Activity> activities)
        {
            return InsertObjects(activities,Insert);
        }

        public bool Insert(List<SegmentEffort> segmentEfforts)
        {
            return InsertObjects(segmentEfforts,Insert);
        }

        public bool Insert(List<Segment> segments)
        {
            return InsertObjects(segments,Insert,insertedSegments_);
        }

        public bool Insert(Activity activity)
        {
            Action<Activity,MySqlCommand> insertAct = (obj,command) => 
            {
                command.Parameters.AddWithValue("@id", obj.Id); 
                command.Parameters.AddWithValue("@name", obj.Name);
                command.Parameters.AddWithValue("@distance", obj.Distance);
                command.Parameters.AddWithValue("@moving_time", obj.Moving_Time);
                command.Parameters.AddWithValue("@elapsed_time", obj.Elapsed_Time);
                command.Parameters.AddWithValue("@avg_speed", obj.Average_Speed);
                command.Parameters.AddWithValue("@max_speed", obj.Max_Speed);
                command.Parameters.AddWithValue("@date_time", obj.DateTimeStr);
                command.Parameters.AddWithValue("@athlete_id", obj.Athlete.Id);
                command.Parameters.AddWithValue("@total_elevation_gain", obj.Total_Elevation_Gain);
                command.Parameters.AddWithValue("@elev_high", obj.Elev_High);
                command.Parameters.AddWithValue("@elev_low", obj.Elev_Low);
                command.Parameters.AddWithValue("@start_latitude", obj.Start_Latitude);
                command.Parameters.AddWithValue("@start_longitude", obj.Start_Longitude);
                command.Parameters.AddWithValue("@end_latitude",obj.End_Latitude);
                command.Parameters.AddWithValue("@end_longitude",obj.End_Longitude);
                command.Parameters.AddWithValue("@description", obj.Description);
                command.Parameters.AddWithValue("@commute", obj.Commute);
            }; 
            return InsertObjectIntoTable(activity, insertAct, Configuration.MySQL.Tables.Activity.COLUMNS, 
                Configuration.MySQL.Tables.Activity.NAME); 
        }

        public bool Insert(SegmentEffort segmentEffort)
        {
            Action<SegmentEffort,MySqlCommand> insertAct = (obj,command) => 
            {
                command.Parameters.AddWithValue("@id", obj.Id);
                command.Parameters.AddWithValue("@athlete_id", obj.Athlete.Id);
                command.Parameters.AddWithValue("@segment_id", obj.Segment.Id);
                command.Parameters.AddWithValue("@activity_id", obj.Activity.Id); 
                command.Parameters.AddWithValue("@name", obj.Name);
                command.Parameters.AddWithValue("@distance", obj.Distance);
                command.Parameters.AddWithValue("@moving_time", obj.Moving_Time);
                command.Parameters.AddWithValue("@elapsed_time", obj.Elapsed_Time);
                command.Parameters.AddWithValue("@date_time", obj.DateTimeStr);
            };
            return InsertObjectIntoTable(segmentEffort, insertAct, Configuration.MySQL.Tables.SegmentEffort.COLUMNS, 
                Configuration.MySQL.Tables.SegmentEffort.NAME); 
        }

        public bool Insert(Segment segment)
        {
            Action<Segment,MySqlCommand> insertAct = (obj, command) =>
            {
                command.Parameters.AddWithValue("@id", segment.Id);
                command.Parameters.AddWithValue("@name", segment.Name);
                command.Parameters.AddWithValue("@distance",segment.Distance);
                command.Parameters.AddWithValue("@avg_grade", segment.Average_Grade);
                command.Parameters.AddWithValue("@max_grade", segment.Maximum_Grade);
                command.Parameters.AddWithValue("@elev_high", segment.Elevation_High);
                command.Parameters.AddWithValue("@elev_low", segment.Elevation_Low);
                command.Parameters.AddWithValue("@start_latitude", segment.Start_Latitude);
                command.Parameters.AddWithValue("@start_longitude", segment.Start_Longitude);
                command.Parameters.AddWithValue("@end_latitude", segment.End_Latitude);
                command.Parameters.AddWithValue("@end_longitude", segment.End_Longitude);
                command.Parameters.AddWithValue("@starred", segment.Starred);
            };
            return InsertObjectIntoTable(segment, insertAct, Configuration.MySQL.Tables.Segment.COLUMNS, 
                Configuration.MySQL.Tables.Segment.NAME); 
        }

        public bool Update(Activity activity)
        {
            return UpdateObjectInTable(Configuration.MySQL.Tables.Activity.NAME, $"id={activity.Id}", 
                new Dictionary<string, string>()
                {
                    {"commute",activity.Commute ? "1" : "0"},
                    {"name",activity.Name},
                    {"description",activity.Description},
                });
        }

        private bool UpdateObjectInTable(string tableName, string whereCondition, Dictionary<string,string> cols)
        {
            using(var connection = new MySqlConnection(connectionString_))
            {
                connection.Open();
                var command = connection.CreateCommand();
                var columns = String.Join(",", cols.Keys);
                var columnsAsParams =$"@{String.Join(", @", cols.Keys)}";
                StringBuilder sb = new StringBuilder();
                sb.Append($"UPDATE {tableName} SET ");
                foreach(var kvp in cols)
                {
                    sb.Append($"{kvp.Key}=@{kvp.Key},");
                    command.Parameters.AddWithValue($"{kvp.Key}", kvp.Value);
                }
                sb.Length--;
                sb.Append($" WHERE {whereCondition}");
                command.CommandText = sb.ToString();
                try
                {
                    if(command.ExecuteNonQuery() > 0)
                    {
                        return true;
                    }
                    Console.WriteLine($"Unable to update {tableName}.");
                }
                catch(MySqlException ex)
                {
                    Console.WriteLine($"Unable to update {tableName}.");
                    Console.WriteLine(command.ToString());
                    Console.WriteLine(command.CommandText);
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);                    
                }
                return false;
            }
        }

        private bool InsertObjects<T>(List<T> objs, Func<T,bool> insertFunc, HashSet<long> existingObjs) where T : IStravaObject
        {
            foreach(var obj in objs.Where(o => existingObjs != null && !existingObjs.Contains(o.Id)))
            {
                if(!insertFunc(obj))
                {
                    return false;
                }
                else
                {
                    existingObjs.Add(obj.Id);
                }
            }
            return true;
        }

        private bool InsertObjects<T>(List<T> objs, Func<T,bool> insertFunc) where T : IStravaObject
        {
            foreach(var obj in objs)
            {
                if(!insertFunc(obj))
                {
                    return false;
                }
            }
            return true;
        }

        private bool InsertObjectIntoTable<T>(T obj, Action<T,MySqlCommand> createCommand, Dictionary<string,string> cols, string tableName) where T : IStravaObject
        {
            using(var connection = new MySqlConnection(connectionString_))
            {
                connection.Open();
                var command = connection.CreateCommand();
                var columns = String.Join(",", cols.Keys);
                var columnsAsParams =$"@{String.Join(", @", cols.Keys)}";
                command.CommandText = $"INSERT INTO {tableName}({columns}) VALUES({columnsAsParams})";
                createCommand(obj, command);
                try
                {
                    if(command.ExecuteNonQuery() > 0)
                    {
                        Console.WriteLine($"Successfully added {tableName} {obj.Name} ({obj.Id})");
                        return true;
                    }
                    Console.WriteLine($"Unable to add {tableName} {obj.Name} ({obj.Id})");
                }
                catch(MySqlException ex)
                {
                    Console.WriteLine($"Unable to add {tableName} {obj.Name} ({obj.Id})");
                    Console.WriteLine(command.ToString());
                    Console.WriteLine(command.CommandText);
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);                    
                }
                return false;
            }
        }

        #endregion
        
        #region Initialization
        private bool InitializeConnection()
        {
            Console.WriteLine("Initializing connection...");
            try 
            {
                var server = Program.Configuration["mySQL"]["server"];
                var uid = Program.Configuration["mySQL"]["uid"];
                var password = Program.Configuration["mySQL"]["password"];
                var database = Program.Configuration["mySQL"]["database"];
                connectionString_ = 
                    $"server={server};uid={uid};pwd={password};database={database}";
                return true;
            }
            catch(MySqlException ex)
            {
                Console.WriteLine($"Unable to connect with connection string {connectionString_}");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Unable to connect.");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            return false;
        }


        private bool InitializeTables()
        {
            using(var connection = new MySqlConnection(connectionString_))
            {
                connection.Open();
                Console.WriteLine($"Successfully connected. MySQL v{connection.ServerVersion}");
                if(createNewTables_)
                {
                    DropTable(Configuration.MySQL.Tables.Activity.NAME, connection);
                    DropTable(Configuration.MySQL.Tables.Segment.NAME, connection);
                    DropTable(Configuration.MySQL.Tables.SegmentEffort.NAME, connection);
                }
                var res = true;
                res |= InitializeTable(Configuration.MySQL.Tables.Activity.NAME, Configuration.MySQL.Tables.Activity.COLUMNS, connection);
                res |= InitializeTable(Configuration.MySQL.Tables.Segment.NAME, Configuration.MySQL.Tables.Segment.COLUMNS, connection);
                res |= InitializeTable(Configuration.MySQL.Tables.SegmentEffort.NAME, Configuration.MySQL.Tables.SegmentEffort.COLUMNS, connection);
                return res;
            }
        }

        private bool InitializeTable(string tableName, Dictionary<string,string> columns, MySqlConnection connection)
        {
            if(!TableExists(tableName, connection))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"CREATE TABLE {tableName} (");
                foreach(var kvp in columns)
                {
                    sb.Append($"{kvp.Key} {kvp.Value},");
                } 
                sb.Append("PRIMARY KEY (id))");
                var createTableStr = sb.ToString();
                var cmd = new MySqlCommand(createTableStr, connection);
                try 
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine($"Successfully created '{tableName}'.");
                    return true;
                }
                catch(MySqlException ex)
                {
                    Console.WriteLine($"Unable to create table with command {createTableStr}");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return false;
                }
            }
            return true;
        }

        private bool TableExists(string tableName, MySqlConnection connection)
        {
            var check = $"SHOW TABLES LIKE \'{tableName}\'";
            var cmd = new MySqlCommand(check, connection);
            var reader = cmd.ExecuteReader();
            var exists = reader.HasRows;
            reader.Close();
            return exists;
        }

        private bool DropTable(string tableName, MySqlConnection connection)
        {
            var cmd = new MySqlCommand($"DROP TABLE {tableName}", connection);
            try 
            {
                cmd.ExecuteNonQuery();
                Console.WriteLine($"Successfully dropped table {tableName}");
                return true;
            }
            catch(MySqlException ex)
            {
                Console.WriteLine($"Unable to drop table {tableName}");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            return false;
        }

        #endregion
    }
}