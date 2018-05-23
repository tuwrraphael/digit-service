using System;
using System.Runtime.Serialization;

namespace DigitService.Client
{
    [Serializable]
    public class DigitServiceException : Exception
    {
        public DigitServiceException()
        {
        }

        public DigitServiceException(string message) : base(message)
        {
        }

        public DigitServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}