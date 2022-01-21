using System;
using System.Runtime.Serialization;

namespace Proxy
{
    [Serializable]
    public class InvalidProxyUrlFormatException : Exception
    {
        public InvalidProxyUrlFormatException()
        {
        }

        public InvalidProxyUrlFormatException(string message) : base(message)
        {
        }

        public InvalidProxyUrlFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidProxyUrlFormatException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
