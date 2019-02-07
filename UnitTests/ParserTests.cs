using NUnit.Framework;
using Relay;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTests
{
    public class Critter
    {
        public DateTimeOffset Created { get; }
        public string Name { get; }
        public House Dwelling { get;  }
    }

    public class House
    {
        public string Title { get;  }
    }

    public class ParserTests
    {
        public static object[][] ParseTestData =
        {
            new object[] { "true != false", "Not((True == False))" },
            new object[] { "true and false or false and true", "((True AndAlso False) OrElse (False AndAlso True))"},
            new object[] { "Length > 10", "(obj.Length > 10)" },
            new object[] { "Length = 10", "(obj.Length == 10)" },
            new object[] { "'\\'' = '\\''", "(\"'\" == \"'\")"},
            new object[] { "2019-01-01T12:00:00+01 < 2019-01-02T12:00:00+01", "(2019-01-01 12:00:00 +01:00 < 2019-01-02 12:00:00 +01:00)"},
        };

        [TestCaseSource(nameof(ParseTestData))]
        public void ParseTest(string input, string expected)
        {
            var parser = new Parser().Configure<string>(x => x.Whitelist());
            var exp = parser.Parse<string>(input);
            Assert.That(exp.Body.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void Parse_Critter()
        {
            var parser = new Parser()
                .Configure<Critter>(x => x.Whitelist(c => c.Dwelling).Whitelist(c => c.Created))
                .Configure<House>(x => x.Whitelist(c => c.Title));

            var exp = parser.Parse<Critter>("Dwelling.Title = 'House Harkonnen',Dwelling.Title = 'House Atreides'");
        }
    }
}
