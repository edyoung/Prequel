namespace Prequel
{
    public struct AssignmentResult
    {
        public bool IsOK { get; private set; }

        public AssignmentResult(bool isOK)
        {
            IsOK = isOK;
        }

        private static AssignmentResult ok = new AssignmentResult(true);

        public static AssignmentResult OK
        {
            get
            {
                return ok;
            }
        }
    }
}