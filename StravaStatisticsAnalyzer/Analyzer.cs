namespace StravaStatisticsAnalyzer
{
    public class Analyzer
    {
        Fetcher fetcher_;
        DBWriter dbWriter_;

        public Analyzer()
        {
            fetcher_ = new Fetcher();
            dbWriter_ = new DBWriter();
        }

        public void Initialize(string accessToken)
        {
            fetcher_.Initialize(accessToken);
            dbWriter_.Initialize();
        }

        public void GetAndSaveActivities()
        {
            var activities = fetcher_.GetAllActivities(null, 1534982400);
            dbWriter_.Insert(activities);
        }
    }
}