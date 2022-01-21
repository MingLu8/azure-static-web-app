using System;
using System.Runtime.Serialization;

namespace Proxy
{
  [Serializable]
  public class InvalidServiceBaseUrlException : Exception
  {
    public InvalidServiceBaseUrlException()
    {
    }

    public InvalidServiceBaseUrlException(string message) : base(message)
    {
    }

    public InvalidServiceBaseUrlException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected InvalidServiceBaseUrlException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}
