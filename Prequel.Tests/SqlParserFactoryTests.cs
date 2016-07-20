using System;
using Microsoft.SqlServer.TransactSql.ScriptDom;

using Xunit;
using FluentAssertions;

namespace Prequel.Tests
{    
    public class SqlParserFactoryTests
    {
        [Fact]
        public void DefaultparserIsT120()
        {
            SqlParserFactory.DefaultType.Should().Be(typeof(TSql120Parser));
        }

        [InlineData("foo")]
        [InlineData("")]
        [InlineData(null)]
        [Theory]
        public void InvalidVersionsAreRejected(string version)
        {
            Action act = () => SqlParserFactory.Type(version);
            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void InvalidVersionExceptionContainsKnownVersions()
        {
            Action act = () => SqlParserFactory.Type("foo");            
            act.ShouldThrow<ArgumentException>().WithMessage("*" + SqlParserFactory.AllVersions + "*");
        }
    }
}
