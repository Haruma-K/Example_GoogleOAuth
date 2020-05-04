using System;

namespace GoogleOAuth.Editor
{
    public class NetworkException : Exception
    {
        public long StatusCode { get; internal set; }
        
        public NetworkException(string message, long statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}