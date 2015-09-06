using System;
using System.Runtime.Serialization;

namespace Prequel
{   
    public enum ExitReason
    {
        Success = 0,
        GeneralFailure = 1,
        IOError = 2
    }

    /// <summary>
    /// We throw one of these when we encounter a problem bad enough to warrant failing out of the program,
    /// providing an exit code. 
    /// </summary> 
    [Serializable]
    public class ProgramTerminatingException : Exception, ISerializable
    {
        public ProgramTerminatingException(ExitReason exitcode=ExitReason.GeneralFailure)
        {
            ExitCode = exitcode;
        }

        public ProgramTerminatingException(string message, ExitReason exitcode=ExitReason.GeneralFailure) : base(message)
        {
            ExitCode = exitcode;
        }

        public ProgramTerminatingException(string message, Exception innerException, ExitReason exitcode=ExitReason.GeneralFailure) : base(message, innerException)
        {
            ExitCode = exitcode;
        }

        protected ProgramTerminatingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ExitCode = (ExitReason)info.GetInt32("ExitCode");
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


        public ExitReason ExitCode { get; set; }
    }
}