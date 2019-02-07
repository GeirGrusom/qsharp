using System;
using System.Collections.Generic;
using System.Text;

namespace Relay
{
    public sealed class Tokenizer
    {
        private readonly string input;

        private Token currentToken;

        int lastPosition;
        int currentPosition;

        public Tokenizer(string input)
        {
            this.input = input;
        }

        bool ConsumeWhitespace()
        {
            if(ConsumeWhile(char.IsWhiteSpace))
            {
                currentToken = new Token(input, lastPosition, currentPosition - lastPosition, TokenType.Whitespace);
                lastPosition = currentPosition;
                return true;
            }
            return false;
        }

        bool ConsumeIdentifier()
        {
            if(!ConsumeCount(c => char.IsLetter(c) || c == '_', 1))
            {
                return false;
            }
            ConsumeWhile(c => char.IsLetterOrDigit(c) || c == '_');

            currentToken = new Token(input, lastPosition, currentPosition - lastPosition, TokenType.Identifier);

            if(currentToken.Equals("true"))
            {
                currentToken = new Token(input, lastPosition, currentPosition - lastPosition, TokenType.True);
            }
            else if (currentToken.Equals("false"))
            {
                currentToken = new Token(input, lastPosition, currentPosition - lastPosition, TokenType.False);
            }
            else if (currentToken.Equals("null"))
            {
                currentToken = new Token(input, lastPosition, currentPosition - lastPosition, TokenType.Null);
            }

            lastPosition = currentPosition;
            return true;
        }

        HashSet<char> operatorChars = new HashSet<char>
        {
            '=',
            '!',
            '<',
            '>',
            'i',
            'n',
            'a',
            'd',
            'o',
            'r',
            '(',
            ')',
            '.',
            '?',
            ','
        };

        HashSet<string> operators = new HashSet<string>
        {
            "=",
            "!=",
            "<>",
            ">",
            "<",
            "in",
            "!",
            "and",
            "or",
            "(",
            ")",
            ".",
            "?.",
            ","
        };

        bool ConsumeOperator()
        {
            var lastPos = currentPosition;
            
            if(ConsumeWhile(c => operatorChars.Contains(c)))
            {
                currentToken = new Token(input, lastPosition, currentPosition - lastPosition, TokenType.Operator);

                if(!operators.Contains(currentToken.ToString()))
                {
                    this.lastPosition = lastPos;
                    this.currentPosition = lastPos;
                }

                this.lastPosition = this.currentPosition;
                return true;
            }
            return false;
        }

        bool ConsumeString(char quote)
        {
            if(!Consume(quote))
            {
                return false;
            }

            bool previousWasQuote = false;

            while(this.currentPosition < input.Length)
            {
                if(this.input[this.currentPosition] == '\\')
                {
                    if(previousWasQuote)
                    {
                        previousWasQuote = false;
                        ++this.currentPosition;
                        continue;
                    }
                }
                else if(this.input[this.currentPosition] == quote && !previousWasQuote)
                {
                    ++this.currentPosition;
                    break;
                }
                else 
                {
                    ++this.currentPosition;
                }
            }

            currentToken = new Token(input, lastPosition, currentPosition - lastPosition, TokenType.String);
            lastPosition = currentPosition;

            return true;
        }

        bool Consume(char c)
        {
            if(input[this.currentPosition] == c)
            {
                ++this.currentPosition;
                return true;
            }
            return false;
        }

        bool ConsumeNumber()
        {
            if(ConsumeWhile(char.IsNumber))
            {
                if(ConsumeCount(c => c == '.', 1))
                {
                    if(!ConsumeWhile(char.IsNumber))
                    {
                        throw new FormatException("Invalid number format");
                    }
                }

                currentToken = new Token(input, lastPosition, currentPosition - lastPosition, TokenType.Number);
                lastPosition = currentPosition;
                return true;
            }
            else if(ConsumeCount(c => c == '.', 1))
            {
                if (!ConsumeWhile(char.IsNumber))
                {
                    throw new FormatException("Invalid number format");
                }

                currentToken = new Token(input, lastPosition, currentPosition - lastPosition, TokenType.Number);
                lastPosition = currentPosition;
                return true;
            }
            return false;
        }

        bool ConsumeCount(Func<char, bool> predicate, int count)
        {
            var oldPos = currentPosition;
            int i;
            for(i = 0; i < count; ++i)
            {
                if (currentPosition < input.Length && predicate(input[currentPosition]))
                {
                    ++currentPosition;
                }
                else
                {
                    break;
                }
            }
            if(i < count)
            {
                currentPosition = oldPos;
                return false;
            }

            return true;
        }

        bool ConsumeWhile(Func<char, bool> predicate)
        {
            do
            {
                if(currentPosition < input.Length && predicate(input[currentPosition]))
                {
                    ++currentPosition;
                }
                else
                {
                    break;
                }
            } while (true);

            return currentPosition != lastPosition;
        }

        public IEnumerable<Token> Tokenize()
        {
            int failStart = -1;
            int failCount = 0;
            while(currentPosition < input.Length)
            {
                if(ConsumeWhitespace() || ConsumeNumber() || ConsumeOperator() || ConsumeIdentifier() || ConsumeString('"') || ConsumeString('\''))
                {
                    if(failStart != -1)
                    {

                        yield return new Token(this.input, failStart, failCount, TokenType.Error);
                        failStart = -1;
                        failCount = 0;
                    }

                    yield return currentToken;
                    continue;
                }
                else
                {
                    if(failStart == -1)
                    {
                        failStart = currentPosition;
                        failCount = 1;
                    }
                    else
                    {
                        ++failCount;
                    }
                }
            }
        }
    }
}
