namespace Prequel
{
    using System.Collections.Generic;

    public struct AssignmentResult
    {
        public bool IsOK { get; private set; }

        public AssignmentResult(bool isOK, IList<Warning> warnings = null)
        {
            IsOK = isOK;
            Warnings = warnings ?? new List<Warning>();
        }

        private static AssignmentResult ok = new AssignmentResult(true);

        public static AssignmentResult OK
        {
            get
            {
                return ok;
            }
        }

        public IList<Warning> Warnings { get; private set; }
    }
}