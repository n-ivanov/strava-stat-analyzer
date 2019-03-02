using System;
using System.Linq;
using System.Collections.Generic;

namespace ExtendedStravaClient
{
    public interface IRideEffortAnalysis
    {
        string Name { get; }
        List<IRideEffort> Rides { get; }
        int IntervalLength {get;}
        IExtendedStatisticalAnalysis<double> Speed { get; }
        IExtendedStatisticalAnalysis<int> Time { get; } 
    }

    public class RideEffortAnalysis : IRideEffortAnalysis
    {
        public string Name { get; }
        public List<IRideEffort> Rides { get; }
        public int IntervalLength => Rides.Count;
        public IExtendedStatisticalAnalysis<double> Speed { get; }
        public IExtendedStatisticalAnalysis<int> Time { get; } 

        public RideEffortAnalysis(string name, List<IRideEffort> rides)
        {
            Name = name;
            Rides = rides;
            Speed = new DoubleStatisticalAnalysis(rides.Select(r => r.AverageSpeed).ToList());
            Time = new IntegerStatisticalAnalysis(rides.Select(r => r.MovingTime).ToList());
        }
    }
}