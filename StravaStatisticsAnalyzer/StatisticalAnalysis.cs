using System;
using System.Linq;
using System.Collections.Generic;

namespace StravaStatisticsAnalyzer
{
    public interface IStatisticalAnalysis<T> where T: struct, IComparable<T>, IConvertible
    {
        T Maximum { get; }
        T Minimum { get; }
    }

    public interface IExtendedStatisticalAnalysis<T> : IStatisticalAnalysis<T> where T: struct, IComparable<T>, IConvertible
    {
        double Average { get; }
        double StandardDeviation { get; }
    }

    public class StatisticAnalysis<T> : IStatisticalAnalysis<T> where T: struct, IComparable<T>, IConvertible
    {
        public T Maximum { get; }
        public T Minimum { get; }
       
        public StatisticAnalysis(List<T> items)
        {
            if(items.Count == 0)
            {
                Maximum = default(T);
                Minimum = default(T);
            }
            else
            {
                Minimum = items[0];
                Maximum = items[0];
                foreach(var item in items)
                {
                    if(item.CompareTo(Minimum) < 0)
                    {
                        Minimum = item;
                    }
                    else if(item.CompareTo(Maximum) > 0)
                    {
                        Maximum = item;
                    }
                }
            }
        }
    }

    public class IntegerStatisticalAnalysis : StatisticAnalysis<int>, IExtendedStatisticalAnalysis<int>
    {
        public double Average { get; }
        public double StandardDeviation { get; }
        public IntegerStatisticalAnalysis(List<int> items) : base(items)
        {
            Average = items.Average();
            StandardDeviation = Math.Sqrt(items.Average(v=>Math.Pow(v-Average,2)));
        }
    }

    public class DoubleStatisticalAnalysis : StatisticAnalysis<double>, IExtendedStatisticalAnalysis<double>
    {
        public double Average { get; }
        public double StandardDeviation { get; }
        public DoubleStatisticalAnalysis(List<double> items) : base(items)
        {
            Average = items.Average();
            StandardDeviation = Math.Sqrt(items.Average(v=>Math.Pow(v-Average,2)));
        }
    }
}