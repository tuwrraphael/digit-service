using System;
using System.Runtime.Serialization;

namespace DigitService.Impl
{
    [Serializable]
    internal class UserConfigurationException : Exception
    {
        public UserConfigurationException()
        {
        }

        public UserConfigurationException(string message) : base(message)
        {
        }

        public UserConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UserConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}