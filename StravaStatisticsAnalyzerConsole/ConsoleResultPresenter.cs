using System;
using System.Text;
using System.Extensions;
using System.Collections.Generic;
using StravaStatisticsAnalyzer;

namespace StravaStatisticsAnalyzerConsole
{
    public class ConsoleResultPresenter : IResultPresenter
    {
        private const int RIDE_NAME_COL_WIDTH = -30;
        private const int DATA_COL_WIDTH = 20;

        private const int METERS_PER_KILOMETER = 1000;
        private const int SECONDS_PER_HOUR = 3600;
        private const double MPS_TO_KPH = (double)SECONDS_PER_HOUR / METERS_PER_KILOMETER;

        public void PresentResults(Dictionary<string,List<IRideEffortAnalysis>> rideEffortAnalyses, int[] intervals)
        {
            var exampleListEnumerator = rideEffortAnalyses.Values.GetEnumerator();
            exampleListEnumerator.MoveNext();
            var countAnalyses = exampleListEnumerator.Current.Count;
            var rideName = exampleListEnumerator.Current[0].Name;
            
            var mainColWidths = new int[1 + 2 * intervals.Length];
            mainColWidths[0] = RIDE_NAME_COL_WIDTH * -1;
            for(int i = 1; i < mainColWidths.Length; i++)
            {
                mainColWidths[i] = DATA_COL_WIDTH;
            }
            string intersectionLine = CreateLineWithIntersections(mainColWidths);

            Console.WriteLine($" Analysis of {rideName} ".PadBoth(intersectionLine.Length, '='));
            Console.WriteLine(CreateHeaders(countAnalyses, intervals));
            bool firstRow = true;
            foreach(var kvp in rideEffortAnalyses)
            {
                Console.WriteLine(CreateRow(kvp.Key, kvp.Value));
                if(firstRow)
                {
                    firstRow = false;
                    Console.WriteLine(intersectionLine);
                }       
            }
            Console.WriteLine(intersectionLine);

        }
        
        private string CreateRow(string rideName, List<IRideEffortAnalysis> rideEffortAnalyses)
        {
            var numAnalyses = rideEffortAnalyses.Count;
            var row = new string[numAnalyses * 2];
            for(int i = 0 ; i < numAnalyses; i++)
            {
                var analysis = rideEffortAnalyses[i];
                row[i] = $"{((int)analysis.Time.Average).ToTime()} @ {MPS_TO_KPH*analysis.Speed.Average:##.#0}";
                row[i + numAnalyses] = $"{analysis.Time.Minimum.ToTime()} @ {MPS_TO_KPH*analysis.Speed.Maximum:##.#0}";
            }
            StringBuilder sb = new StringBuilder();
            if(rideName.Length > (RIDE_NAME_COL_WIDTH * -1))
            {
                rideName = rideName.Substring(0, RIDE_NAME_COL_WIDTH * -1);
            }
            sb.Append($"|{rideName,RIDE_NAME_COL_WIDTH}");
            foreach(var col in row)
            {
                sb.Append($"|{col,DATA_COL_WIDTH}");
            }
            sb.Append("|");
            return sb.ToString();
        }

        private string CreateHeaders(int numAnalyses, int[] intervals)
        {
            StringBuilder header = new StringBuilder();
            var aggregateColWidth = numAnalyses * (DATA_COL_WIDTH + 1) - 1;
            var mainColWidths = new int[1 + 2 * intervals.Length];
            mainColWidths[0] = RIDE_NAME_COL_WIDTH * -1;
            for(int i = 1; i < mainColWidths.Length; i++)
            {
                mainColWidths[i] = DATA_COL_WIDTH;
            }
            var topLine = CreateLineWithIntersections(new [] {RIDE_NAME_COL_WIDTH * -1, aggregateColWidth, aggregateColWidth});
            var midLine = CreateLineWithIntersections(mainColWidths);
            header.AppendLine(topLine);
            header.Append($"|{"",RIDE_NAME_COL_WIDTH}");
            header.Append($"|{"Avg (time @ km/h)".PadBoth(aggregateColWidth)}");
            header.Append($"|{"Best (time @ km/h)".PadBoth(aggregateColWidth)}");
            header.Append($"|{System.Environment.NewLine}");
            header.AppendLine(midLine);
            header.Append($"|{"Ride/Segment Name",RIDE_NAME_COL_WIDTH}");
            StringBuilder subHeader = new StringBuilder();
            foreach(var interval in intervals)
            {
                subHeader.Append($"|{$"Last {interval} Rides" , DATA_COL_WIDTH}");
            }
            
            header.Append($"{subHeader}");
            header.Append($"{subHeader}|\n");
            header.Append(midLine);
            return header.ToString();
        }

        private string CreateLine(int length)
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < length; i++)
            {
                sb.Append("-");
            }
            return sb.ToString();
        }

        private string CreateLineWithIntersections(IEnumerable<int> colWidths)
        {
            StringBuilder sb = new StringBuilder();
            foreach(var width in colWidths)
            {
                sb.AppendFormat($"+{{0,{width}}}", CreateLine(width));
            }
            sb.Append("+");
            return sb.ToString();
        }
    }
}