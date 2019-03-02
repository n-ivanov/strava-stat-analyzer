using System.Collections.Generic;

namespace StravaStatisticsAnalyzerConsole
{
    internal static class Configuration
    {
        internal static class Strava 
        {
            public const int CLIENT_ID = -1; //<YOUR STRAVA CLIENT ID>
            public const string CLIENT_SECRET = "YOUR STRAVA CLIENT SECRET";
            public const int LOCAL_SERVER_PORT = 5001;
        }


        public static class MySQL
        {
            public const string SERVER = "127.0.0.1";
            public const string UID = "nivanov";
            public const string PASSWORD = "admin";
            public const string DATABASE = "strava";

            public static class Tables
            {
                public static class Activity
                {
                    public const string NAME = "activity";
                    public static readonly Dictionary<string,string> COLUMNS 
                        = new Dictionary<string,string>(){
                            {"id", "BIGINT"},
                            {"name","VARCHAR(50)"},
                            {"distance","DOUBLE"},
                            {"moving_time","INTEGER"},
                            {"elapsed_time","INTEGER"},
                            {"avg_speed", "DOUBLE"},
                            {"max_speed", "DOUBLE"},
                            {"date_time", "DATETIME"},
                            {"athlete_id", "BIGINT"},
                            {"total_elevation_gain","DOUBLE"}, 
                            {"elev_high","DOUBLE"},
                            {"elev_low","DOUBLE"},
                            {"start_latitude","DOUBLE"},
                            {"start_longitude","DOUBLE"},
                            {"end_latitude","DOUBLE"},
                            {"end_longitude","DOUBLE"},
                            {"description", "VARCHAR(255)"},
                            {"commute", "BOOL"},
                        };
                }

                public static class Segment
                {
                    public const string NAME = "segment";

                    public static readonly Dictionary<string,string> COLUMNS 
                        = new Dictionary<string,string>(){
                            {"id", "BIGINT"},
                            {"name","VARCHAR(100)"},
                            {"distance","DOUBLE"},
                            {"avg_grade", "DOUBLE"},
                            {"max_grade", "DOUBLE"},
                            {"elev_high","DOUBLE"},
                            {"elev_low","DOUBLE"},
                            {"start_latitude","DOUBLE"},
                            {"start_longitude","DOUBLE"},
                            {"end_latitude","DOUBLE"},
                            {"end_longitude","DOUBLE"},
                            {"starred", "BOOLEAN"}
                        };
                }

                public static class SegmentEffort
                {
                    public const string NAME = "segment_effort";

                    public static readonly Dictionary<string,string> COLUMNS 
                        = new Dictionary<string,string>(){
                            {"id", "BIGINT"},
                            {"name", "VARCHAR(100)"},
                            {"segment_id","BIGINT"},
                            {"activity_id","BIGINT"},
                            {"athlete_id","BIGINT"},
                            {"distance","DOUBLE"},
                            {"moving_time","INTEGER"},
                            {"elapsed_time","INTEGER"},
                            {"date_time", "DATETIME"},
                        };
                }               
            }
        }
    }
}