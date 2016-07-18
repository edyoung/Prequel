

namespace Prequel.Tests
{
    using System.Linq;
    using Xunit;

    public static class MyAssert
    {
        public static void HaveNoErrorsOrWarnings(this CheckResults results)
        {
            Assert.Empty(results.Errors);
            Assert.Empty(results.Warnings);
            Assert.Equal(ExitReason.Success, results.ExitCode);
        }

        public static void NoErrors(this CheckResults results)
        {
            Assert.Empty(results.Errors);
        }

        public static Warning OneWarningOfType(WarningID id, CheckResults results)
        {
            Assert.Empty(results.Errors);
            Assert.Equal(1, results.Warnings.Count(warning => warning.Number == id));
            return results.Warnings.First(warning => warning.Number == id);
        }

        public static Warning OneWarningOfType(WarningID id, AssignmentResult results)
        {
            Assert.Equal(1, results.Warnings.Count(warning => warning.Number == id));
            return results.Warnings.First(warning => warning.Number == id);
        }

        public static void NoWarningsOfType(WarningID id, CheckResults results)
        {
            Assert.Empty(results.Errors);
            Assert.Equal(0, results.Warnings.Count(warning => warning.Number == id));
        }
    }
}
