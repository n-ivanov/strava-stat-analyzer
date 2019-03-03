namespace ExtendedStravaClient
{

    public class Segment : IStravaObject
    {
        public string Name { get; set;}
        public long Id {get; set;}
        public double Distance {get; set;}
        public double Average_Grade {get; set;}
        public double Maximum_Grade {get; set;}
        public double Start_Latitude { get; set; }
        public double Start_Longitude { get; set; }
        public double End_Latitude { get; set; }
        public double End_Longitude { get; set; }
        public double Elevation_High {get; set;}
        public double Elevation_Low {get; set;}
        public bool Starred { get; set; }
    }
}