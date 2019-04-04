using System;
using System.Runtime.Serialization;

namespace Digit.DeviceSynchronization.Models
{
    public class PushChannelNotFoundException : Exception
    {
        public PushChannelNotFoundException()
        {
        }

        public PushChannelNotFoundException(string message) : base(message)
        {
        }

        public PushChannelNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PushChannelNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}