using RestSharp;

namespace RestSharp.Extensions
{
    public static class RestRequestExtensions
    {
        public static void AddNullableParameter<T>(this RestRequest req, string paramName, T? param) where T : struct
        {
            if(param.HasValue)
            {
                req.AddParameter(paramName, param.Value.ToString());
            }
        }
    }
}