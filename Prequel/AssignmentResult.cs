namespace Prequel
{
    using System.Collections.Generic;

    public struct AssignmentResult
    {
        public bool IsOK { get; private set; }

        public AssignmentResult(bool isOK)
        {
            IsOK = isOK;
            Warnings = new List<Warning>();
        }

        private static AssignmentResult ok = new AssignmentResult(true);

        public static AssignmentResult OK
        {
            get
            {
                return ok;
            }
        }

        public IList<Warning> Warnings { get; internal set; }
    }
}