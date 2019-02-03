

namespace StravaStatisticsAnalyzer
{
    public class AbbreviatedActivity
    {
        public long Id {get; set;}
    }

    public class SegmentEffort : IStravaObject
    {
        #region Direct Deserialization Properties 
        public long Id { get; set; }
        public string Name { get; set; }
        public AbbreviatedAthlete Athlete { get; set; }
        public AbbreviatedActivity Activity {get; set;}
        public double Distance { get; set; }
        public int Elapsed_Time { get; set; }
        public int Moving_Time { get; set; }
        public string Start_Date { get; set; }
        public string End_Date { get; set; }
        public string Timezone { get; set; }
        public Segment Segment {get; set;}
        #endregion

        #region Manipulated Deserialization Properties 
        public string Date => Start_Date.Substring(0, Start_Date.IndexOf("T"));
        public string Time => Start_Date.Substring(Start_Date.IndexOf("T") + 1, 8);
        #endregion
    }
}