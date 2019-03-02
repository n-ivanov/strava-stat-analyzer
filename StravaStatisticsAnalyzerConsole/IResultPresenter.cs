using System;
using System.Text;
using System.Extensions;
using System.Collections.Generic;
using ExtendedStravaClient;

namespace StravaStatisticsAnalyzerConsole
{
    public interface IResultPresenter 
    {
        void PresentResults(Dictionary<string,List<IRideEffortAnalysis>> rideEffortAnalyses, int[] intervals);
    }
}