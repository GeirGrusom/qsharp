using NUnit.Framework;
using Relay;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTests
{
    public class ParserTests
    {
        public static object[][] ParseTestData =
        {
            new object[] { "true != false", "Not((True == False))" },
            new object[] { "true and false or false and true", "((True AndAlso False) OrElse (False AndAlso True))"},
            new object[] { "Length > 10", "(obj.Length) > 10"}

        };

        [TestCaseSource(nameof(ParseTestData))]
        public void ParseTest(string input, string expected)
        {
            var parser = new Parser();
            var exp = parser.Parse<string>(input);

            Assert.That(exp.Body.ToString(), Is.EqualTo(expected));
        }
    }
}
