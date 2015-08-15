using System;
using System.Runtime.Serialization;

namespace Prequel
{
    [Serializable]
    public class UsageException : Exception
    {
        public UsageException()
        {
            ExitCode = 1;
        }

        public UsageException(string message) : base(message)
        {
            ExitCode = 1;
        }

        public UsageException(string message, Exception innerException) : base(message, innerException)
        {
            ExitCode = 1;
        }

        protected UsageException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ExitCode = 1;
        }

        public int ExitCode { get; set; }
    }
}