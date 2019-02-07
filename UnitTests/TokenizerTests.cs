using NUnit.Framework;
using Relay;
using System.Linq;

namespace Tests
{
    public class TokenizerTests
    {
        public static readonly object[][] SingelTokenTestData =
        {
            new object[] { "and", TokenType.Operator },
            new object[] { "or", TokenType.Operator },
            new object[] { "!=", TokenType.Operator },
            new object[] { "<>", TokenType.Operator },
            new object[] { ">", TokenType.Operator },
            new object[] { "<", TokenType.Operator },
            new object[] { "in", TokenType.Operator },
            new object[] { "(", TokenType.Operator },
            new object[] { ")", TokenType.Operator },
            new object[] { ",", TokenType.Operator },
            new object[] { "foo", TokenType.Identifier },
            new object[] { "foo123", TokenType.Identifier },
            new object[] { "_123", TokenType.Identifier },
            new object[] { "    \t\t\t", TokenType.Whitespace },
            new object[] { "123", TokenType.Number },
            new object[] { "123.456", TokenType.Number },
            new object[] { "0.123", TokenType.Number },
            new object[] { "'Hello World!'", TokenType.String },
            new object[] { "\"Hello World!\"", TokenType.String }
        };

        [TestCaseSource(nameof(SingelTokenTestData))]
        public void SingleToken_Ok(string tokenString, TokenType expectedType)
        {
            // Arrange
            var tokenizer = new Tokenizer(tokenString);

            // Act
            var token = tokenizer.Tokenize().Single();

            // Assert
            Assert.That(token, Is.EqualTo(tokenString));
            Assert.That(token.Type, Is.EqualTo(expectedType));
        }

        public static readonly object[][] MultipleTokensTestData =
        {
            new object[] { "foo != 123", new string[] { "foo", " ", "!=", " ", "123" }  }
        };

        [TestCaseSource(nameof(MultipleTokensTestData))]
        public void MultipleTokens(string input, string[] expectedTokens)
        {
            // Arrange
            var tokenizer = new Tokenizer(input);

            // Act
            var tokens = tokenizer.Tokenize().Select(x => x.ToString()).ToArray();

            // Assert
            Assert.That(tokens, Is.EquivalentTo(expectedTokens));
        }
    }
}