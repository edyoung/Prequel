using System;
using System.Runtime.Serialization;

namespace Prequel
{
    [Serializable]
    public class ProgramTerminatingException : Exception, ISerializable
    {
        public ProgramTerminatingException()
        {
            ExitCode = 1;
        }

        public ProgramTerminatingException(string message) : base(message)
        {
            ExitCode = 1;
        }

        public ProgramTerminatingException(string message, Exception innerException) : base(message, innerException)
        {
            ExitCode = 1;
        }

        protected ProgramTerminatingException(SerializationInfo info, StreamingContext context) : base(info, context)
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