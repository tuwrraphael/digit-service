using System;
using System.Runtime.Serialization;

namespace DigitService.Impl
{
    [Serializable]
    internal class ReminderException : Exception
    {
        public ReminderException()
        {
        }

        public ReminderException(string message) : base(message)
        {
        }

        public ReminderException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ReminderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}