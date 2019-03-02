using System;
using System.Extensions;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ExtendedStravaClient;

namespace StravaStatisticsAnalyzerConsole
{
    internal class MySqlDBFacade : IDBFacade
    {
        private MySql.Data.MySqlClient.MySqlConnection connection_;
        private string connectionString_;
        private bool createNewTables_= false;
        private HashSet<long> insertedSegments_;

        public bool Initialize()
        {
            insertedSegments_ = new HashSet<long>();
            if(InitializeConnection())
            {
                return InitializeTables();
            }
            return false;
        }

        public void Shutdown()
        {
            Console.WriteLine("Closing connection...");
            connection_.Close();
        }

        #region Fetcher 

        public int GetLastUpdate()
        {
            var command = connection_.CreateCommand();
            command.CommandText = $"SELECT date_time FROM {Configuration.MySQL.Tables.Activity.NAME} ORDER BY date_time DESC";
            var reader = command.ExecuteReader();
            if(reader.Read())
            {
                var dateTime = reader.GetDateTime(0);
                reader.Close();
                return dateTime.ToEpoch();
            }
            reader.Close();
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

        public List<long> GetSegmentIdsForActivity(string activityName)
        {
            var segmentsWithCount = SqlQuery<(long id, int count)>(
                $@"SELECT id,count FROM  
                    ((SELECT segment_id AS id,count(*) AS count FROM segment_effort WHERE activity_id IN 
                        (SELECT id from activity WHERE name like '{activityName}'
                    )
                    group by segment_id) as segment_by_count) ORDER BY count DESC;",
                $"Unable to fetch segments for '{activityName}'",
                (reader => (reader.GetInt64("id"), reader.GetInt32("count")))
            );
            var averageCount = segmentsWithCount.Select(i => i.count).Average();
            return segmentsWithCount.Where(i => i.count >= averageCount).Select(i => i.id).ToList();
        }       

        public List<IRideEffort> GetActivities(string activityName, int? maxInterval)
        {
            return SqlQuery<IRideEffort>(
                $"SELECT id,avg_speed,moving_time,date_time FROM activity WHERE name LIKE '{activityName}' ORDER BY date_time DESC {(maxInterval.HasValue ? $"LIMIT {maxInterval.Value}" : "")}",
                $"Unable to read '{activityName}' activities",
                (reader => new RideEffort(reader.GetInt64("id"), reader.GetDouble("avg_speed"), reader.GetInt32("moving_time"),reader.GetDateTime("date_time")))
            );
        }

        private List<T> SqlQuery<T>(string commandText, string errorMessage, Func<MySqlDataReader,T> createElementFunc)
        {
            var list = new List<T>();
            var command = connection_.CreateCommand();
            command.CommandText = commandText;
            MySqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while(reader.Read())
                {
                    list.Add(createElementFunc(reader));
                }
            }
            catch(MySqlException ex)
            {
                Console.WriteLine(errorMessage);
                Console.WriteLine($"Command - {command.CommandText}");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            reader?.Close();
            return list;
        }

        private Dictionary<TKey,TValue> SqlQuery<TKey,TValue>(string commandText, string errorMessage, 
            Func<MySqlDataReader,TKey> createKeyFunc, Func<MySqlDataReader,TValue> createValueFunc)
        {
            var dict = new Dictionary<TKey,TValue> ();
            var command = connection_.CreateCommand();
            command.CommandText = commandText;
            MySqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while(reader.Read())
                {
                    dict[createKeyFunc(reader)] = createValueFunc(reader);
                }
            }
            catch(MySqlException ex)
            {
                Console.WriteLine(errorMessage);
                Console.WriteLine($"Command - {command.CommandText}");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            reader?.Close();
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
                command.Parameters.AddWithValue("@max_grade", segment.Max_Grade);
                command.Parameters.AddWithValue("@elev_high", segment.Elev_High);
                command.Parameters.AddWithValue("@elev_low", segment.Elev_Low);
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
            throw new NotImplementedException($"{nameof(MySqlDBFacade)}.{nameof(Update)} has not been implemented.");
        }

        private bool InsertObjects<T>(List<T> objs, Func<T,bool> insertFunc, HashSet<long> existingObjs) where T : IStravaObject
        {
            foreach(var obj in objs.Where(o => !existingObjs.Contains(o.Id)))
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
            var command = connection_.CreateCommand();
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

        #endregion
        
        #region Initialization
        private bool InitializeConnection()
        {
            Console.WriteLine("Initializing connection...");
            try 
            {
                connectionString_ = 
                    $"server={Configuration.MySQL.SERVER};uid={Configuration.MySQL.UID};pwd={Configuration.MySQL.PASSWORD};database={Configuration.MySQL.DATABASE}";
                connection_ = new MySqlConnection(connectionString_);
                connection_.Open();
                Console.WriteLine($"Successfully connected. MySQL v{connection_.ServerVersion}");
                return true;
            }
            catch(MySqlException ex)
            {
                Console.WriteLine($"Unable to connect with connection string {connectionString_}");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            return false;
        }


        private bool InitializeTables()
        {
            if(createNewTables_)
            {
                DropTable(Configuration.MySQL.Tables.Activity.NAME);
                DropTable(Configuration.MySQL.Tables.Segment.NAME);
                DropTable(Configuration.MySQL.Tables.SegmentEffort.NAME);
            }
            var res = true;
            res |= InitializeTable(Configuration.MySQL.Tables.Activity.NAME, Configuration.MySQL.Tables.Activity.COLUMNS);
            res |= InitializeTable(Configuration.MySQL.Tables.Segment.NAME, Configuration.MySQL.Tables.Segment.COLUMNS);
            res |= InitializeTable(Configuration.MySQL.Tables.SegmentEffort.NAME, Configuration.MySQL.Tables.SegmentEffort.COLUMNS);
            return res;
        }

        private bool InitializeTable(string tableName, Dictionary<string,string> columns)
        {
            if(!TableExists(tableName))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"CREATE TABLE {tableName} (");
                foreach(var kvp in columns)
                {
                    sb.Append($"{kvp.Key} {kvp.Value},");
                } 
                sb.Append("PRIMARY KEY (id))");
                var createTableStr = sb.ToString();
                var cmd = new MySqlCommand(createTableStr, connection_);
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
            Console.WriteLine($"Table '{tableName}' already exists.");
            return true;
        }

        private bool TableExists(string tableName)
        {
            var check = $"SHOW TABLES LIKE \'{tableName}\'";
            var cmd = new MySqlCommand(check, connection_);
            var reader = cmd.ExecuteReader();
            var exists = reader.HasRows;
            reader.Close();
            return exists;
        }

        private bool DropTable(string tableName)
        {
            var cmd = new MySqlCommand($"DROP TABLE {tableName}", connection_);
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