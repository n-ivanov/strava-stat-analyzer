using System;
using System.Collections.Generic;
using System.Globalization;

namespace ExtendedStravaClient
{
    public class AbbreviatedAthlete
    {
        public long Id { get; set; }
    }

    public class Activity : IStravaObject
    {
        private DateTime dateTime_;

        #region Direct Deserialization Properties
        public AbbreviatedAthlete Athlete {get; set;}
        public string Name { get; set; }
        public double Distance { get; set; }
        public int Elapsed_Time { get; set; }
        public int Moving_Time { get; set; }
        public double Total_Elevation_Gain { get; set; }
        public long Id { get; set; }
        public string Start_Date { get; set; }
        public string End_Date { get; set; }
        public string Timezone { get; set; }
        public double Start_Latitude { get; set; }
        public double Start_Longitude { get; set; }
        public double End_Latitude { get; set; }
        public double End_Longitude { get; set; }
        public double Average_Speed { get; set; }
        public double Max_Speed { get; set; }
        public double Elev_High {get; set;}
        public double Elev_Low {get; set;}
        public bool Commute { get; set; }
        public string Description { get; set; }
        public List<SegmentEffort> Segment_Efforts {get; set;}
        #endregion

        #region Manipulated Deserialization Properties 
        public string DateTimeStr => Start_Date.Replace("T"," ").Replace("Z", "");
        public DateTime DateTime 
        {
            get 
            {
                if(dateTime_ == default(DateTime))
                {
                    dateTime_ = DateTime.TryParseExact(Start_Date, "yyyy-MM-ddTHH:mm:ssZ", 
                        CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt) 
                            ? dt 
                            : default(DateTime); 
                }
                return dateTime_;
            }
        }
        #endregion

    }
}