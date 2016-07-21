

namespace Prequel.Tests
{
    using System.Linq;
    using Xunit;
    using FluentAssertions;
    using FluentAssertions.Execution;
    using FluentAssertions.Primitives;
    using System;
    using System.Linq.Expressions;

    public class CheckResultAssertions : ReferenceTypeAssertions<CheckResults, CheckResultAssertions>
    {
        public CheckResultAssertions(CheckResults results)
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

    public class AssignmentResultAssertions : ReferenceTypeAssertions<AssignmentResult, AssignmentResultAssertions>
    {
        public AssignmentResultAssertions(AssignmentResult results)
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

    public static class Assertions
    {
        public static CheckResultAssertions Should(this CheckResults results)
        {
            return new CheckResultAssertions(results);
        }

        public static AssignmentResultAssertions Should(this AssignmentResult results)
        {
            return new AssignmentResultAssertions(results);
        }

        public static CheckResultAssertions HaveNoErrorsOrWarnings(this CheckResultAssertions assertions)
        {
            CheckResults results = assertions.Subject;

            Assertions.HaveNoErrors(assertions);

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

        public static CheckResultAssertions NotWarnAbout(this CheckResultAssertions assertions, WarningID id)
        {
            CheckResults results = assertions.Subject;
            results.Warnings.Should().NotContain(x => x.Number == id);
            return assertions;
        }

        public static CheckResultAssertions WarnAbout(this CheckResultAssertions assertions, WarningID id)
        {
            CheckResults results = assertions.Subject;
            results.Warnings.Should().Contain(x => x.Number == id);
            return assertions;
        }

        public static CheckResultAssertions WarnAbout(this CheckResultAssertions assertions, Expression<Func<Warning,bool>> predicate)
        {
            CheckResults results = assertions.Subject;
            results.Warnings.Should().Contain(predicate);
            return assertions;
        }

        public static CheckResultAssertions HaveNoErrors(this CheckResultAssertions assertions)
        {
            CheckResults results = assertions.Subject;

            Execute
              .Assertion
              .ForCondition(results.Errors.Count == 0)
              .BecauseOf("checking this sql should not report any errors")
              .FailWith("Found {0} errors, the first one is '{1}'", results.Errors.Count, results.Errors.FirstOrDefault());

            return assertions;
        }

        public static AssignmentResultAssertions WarnAbout(this AssignmentResultAssertions assertions, Expression<Func<Warning, bool>> predicate)
        {
            AssignmentResult results = assertions.Subject;
            results.Warnings.Should().Contain(predicate);
            return assertions;
        }

        public static AssignmentResultAssertions WarnAbout(this AssignmentResultAssertions assertions, WarningID id)
        {
            AssignmentResult results = assertions.Subject;
            results.Warnings.Should().Contain(x => x.Number == id);
            return assertions;
        }      
    }
}
