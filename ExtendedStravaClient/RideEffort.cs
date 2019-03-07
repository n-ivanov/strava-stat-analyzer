using System;

namespace ExtendedStravaClient
{
    public interface IRideEffort
    {
        long Id { get; }
        double AverageSpeed { get; }
        int MovingTime { get; }
        DateTime Date { get; }
    }


    public class RideEffort : IRideEffort
    {
        public long Id { get; }
        public double AverageSpeed { get; }
        public int MovingTime { get; }
        public DateTime Date { get; }

        public RideEffort(long id, double averageSpeed, int movingTime, DateTime date)
        {
            Id = id;
            AverageSpeed = averageSpeed;
            MovingTime = movingTime;
            Date = date;
        }
    }

}