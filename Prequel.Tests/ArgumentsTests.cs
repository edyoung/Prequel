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

        [InlineData("/v:", "")]
        [InlineData("/v:x", "x")]
        [InlineData("/v:8.4", "8.4")]
        [Theory]
        public void InvalidVersionStringIsReported(string version, string versionToComplainAbout)
        {
            var ex = Assert.Throws<UsageException>( () => new Arguments("foo.sql", version));
            Assert.Contains(String.Format("Invalid SQL Version '{0}'", versionToComplainAbout), ex.Message);
        }
    }
}
