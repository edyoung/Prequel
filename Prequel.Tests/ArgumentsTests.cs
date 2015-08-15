using System;
using Xunit;

namespace Prequel.Tests
{
    internal static class TestData
    {
        
    }

    public class ArgumentsTests
    {
        [Fact]
        public void NoArgumentsRaisesUsageException()
        {
            Assert.Throws<UsageException>(() => new Arguments(new string[] { }));
        }

        [Fact]
        public void NoArgumentsSetsExitCode()
        {
            var ex = Assert.Throws<UsageException>( () => new Arguments(new string[] { } ));
            Assert.Equal(1, ex.ExitCode);
        }

        [Fact]
        public void SlashQuestionRaisesUsageExceptionWithZeroExitCode()
        {
            var ex = Assert.Throws<UsageException>(() => new Arguments("/?"));
            Assert.Equal(0, ex.ExitCode);
        }

        [Fact]
        public void MinusQuestionRaisesUsageExceptionWithZeroExitCode()
        {
            var ex = Assert.Throws<UsageException>(() => new Arguments("-?" ));
            Assert.Equal(0, ex.ExitCode);
        }

        [Fact]
        public void FileNameIsRecorded()
        {
            var a = new Arguments("foo.sql");
            Assert.Equal(new string[] { "foo.sql" }, a.Files);
        }

        [Fact]
        public void SqlVersionFlagIsRecorded()
        {
            var a = new Arguments("/v:12", "foo.sql");
            Assert.Equal(12, a.SqlVersion);
        }
        
        [Fact]
        public void DefaultSqlVersionIsZero()
        {
            var a = new Arguments("foo.sql");
            Assert.Equal(0, a.SqlVersion);
        }
    }
}
