using System;
using Microsoft.SqlServer.TransactSql.ScriptDom;

using Xunit;

namespace Prequel.Tests
{    
    public class SqlParserFactoryTests
    {
        [Fact]
        public void DefaultparserIsT120()
        {
            Assert.Equal(typeof(TSql120Parser), SqlParserFactory.DefaultType);
        }

        [InlineData("foo")]
        [InlineData("")]
        [InlineData(null)]
        [Theory]
        public void InvalidVersionsAreRejected(string version)
        {
            Assert.Throws<ArgumentException>(() => SqlParserFactory.Type(version));
        }

        [Fact]
        public void InvalidVersionExceptionContainsKnownVersions()
        {
            var ex = Assert.Throws<ArgumentException>(() => SqlParserFactory.Type("foo"));
            Assert.Contains(SqlParserFactory.AllVersions, ex.Message);
        }
    }
}
