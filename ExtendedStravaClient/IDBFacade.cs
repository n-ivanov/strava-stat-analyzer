using System;
using System.Extensions;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace ExtendedStravaClient
{
    public interface IDBFacade
    {
        bool Initialize();
        void Shutdown();

        bool Insert(List<Activity> activities);
        bool Insert(List<SegmentEffort> segmentEfforts);
        bool Insert(List<Segment> segments);

        bool Insert(Activity activity);
        bool Insert(SegmentEffort segmentEffort);
        bool Insert(Segment segment);

        int GetLastUpdate();
        Dictionary<string,List<IRideEffort>> GetSegmentEffortsForActivity(string activityName, int? maxInterval);
        List<long> GetSegmentIdsForActivity(string activityName);
        List<IRideEffort> GetActivities(string activityName, int? maxInterval);

        bool Update(Activity activity);

    }
}