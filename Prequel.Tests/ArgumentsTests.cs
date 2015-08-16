using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.IO;
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
            var a = new Arguments("/v:2008", "foo.sql");
            Assert.Equal(typeof(TSql100Parser), a.SqlParserType);
        }
        
        

        [InlineData("/v:", "")]
        [InlineData("/v:x", "x")]
        [InlineData("/v:8.4", "8.4")]
        [Theory]
        public void InvalidVersionStringIsReported(string version, string versionToComplainAbout)
        {
            var ex = Assert.Throws<UsageException>( () => new Arguments("foo.sql", version));
            Assert.Contains(String.Format("Unknown SQL version '{0}'", versionToComplainAbout), ex.Message);
        }

        [Fact]
        public void InlineSqlReadIntoStream()
        {
            var a = new Arguments("/i:foo bar");

            foreach (var s in a.Streams)
            {
                TextReader reader = new StreamReader(s);
                string contents = reader.ReadToEnd();
                Assert.Equal("foo bar", contents);
            }
        }
    }
}
