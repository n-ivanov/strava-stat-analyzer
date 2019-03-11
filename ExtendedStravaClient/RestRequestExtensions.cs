using System;
using System.Text;

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

        public static void AddNullableParameter(this RestRequest req, string paramName, string param)
        {
            if(!String.IsNullOrWhiteSpace(param))
            {
                req.AddParameter(paramName, param);
            }
        }
    }
}