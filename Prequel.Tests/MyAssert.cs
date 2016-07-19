

namespace Prequel.Tests
{
    using System.Linq;
    using Xunit;
    using FluentAssertions;
    using FluentAssertions.Execution;
    using FluentAssertions.Primitives;
    using System;

    
    public class ResultAssertions : ReferenceTypeAssertions<CheckResults, ResultAssertions>
    {
        public ResultAssertions(CheckResults results)
        {
            Subject = results;
        }

        protected override string Context
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        
    }

    public static class MyAssert
    {
        public static ResultAssertions Should(this CheckResults results)
        {
            return new ResultAssertions(results);
        }

        public static ResultAssertions HaveNoErrorsOrWarnings(this ResultAssertions assertions)
        {
            CheckResults results = assertions.Subject;

            MyAssert.HaveNoErrors(assertions);

            Execute
                .Assertion
                .ForCondition(results.Warnings.Count == 0)
                .BecauseOf("checking this sql should not report any warnings")
                .FailWith("Found {0} warnings, the first one is '{1}'", results.Warnings.Count, results.Warnings.FirstOrDefault());

            Execute
                .Assertion
                .ForCondition(results.ExitCode == ExitReason.Success)
                .BecauseOf("if there are no errors or warnings, checking should be successful")
                .FailWith($"Exit code was {results.ExitCode}");

            return assertions;
        }        

        public static ResultAssertions HaveNoErrors(this ResultAssertions assertions)
        {
            CheckResults results = assertions.Subject;

            Execute
              .Assertion
              .ForCondition(results.Errors.Count == 0)
              .BecauseOf("checking this sql should not report any errors")
              .FailWith("Found {0} errors, the first one is '{1}'", results.Errors.Count, results.Errors.FirstOrDefault());

            return assertions;
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
