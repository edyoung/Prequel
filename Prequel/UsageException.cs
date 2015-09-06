using System;
using System.Runtime.Serialization;

namespace Prequel
{
    [Serializable]
    public class UsageException : Exception, ISerializable
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
       
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ExitCode", ExitCode);
            base.GetObjectData(info, context);
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            GetObjectData(info, context);
        }


        public int ExitCode { get; set; }
    }
}