using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Relay
{
    internal class ParserState
    {
        
        public readonly Token[] Tokens;
        public readonly ParameterExpression Obj;

        private int currentIndex;

        public ref Token CurrentToken => ref Tokens[currentIndex];

        public bool Eof => currentIndex >= Tokens.Length;

        public ParserState(ParameterExpression obj, IEnumerable<Token> tokens)
        {
            this.Tokens = tokens.Where(t => t.Type != TokenType.Whitespace).ToArray();
            this.Obj = obj;
        }

        public Expression Parse()
        {
            this.currentIndex = 0;
            return ParseOr();
        }

        public Expression ParseOr()
        {
            var lhs = ParseAnd();
            
            if(!Eof && CurrentToken.Equals("or"))
            {
                ++currentIndex;
                var rhs = ParseOr();
                return Expression.OrElse(lhs, rhs);
            }
            else
            {
                return lhs;
            }
        }

        public Expression ParseAnd()
        {
            var lhs = ParseEquality();

            if (!Eof && CurrentToken.Equals("and"))
            {
                ++currentIndex;
                var rhs = ParseAnd();
                return Expression.AndAlso(lhs, rhs);
            }
            else
            {
                return lhs;
            }
        }

        public Expression ParseEquality()
        {
            var lhs = ParseComparison();

            if(Eof)
            {
                return lhs;
            }

            if (CurrentToken.Equals("="))
            {
                ++currentIndex;
                var rhs = ParseComparison();
                return Expression.AndAlso(lhs, rhs);
            }
            else if (CurrentToken.Equals("<>") || CurrentToken.Equals("!="))
            {
                ++currentIndex;
                var rhs = ParseComparison();
                return Expression.Not(Expression.Equal(lhs, rhs));
            }
            else
            {
                return lhs;
            }
        }

        public Expression ParseComparison()
        {
            var lhs = ParseValue();

            if(Eof)
            {
                return lhs;
            }

            if (CurrentToken.Equals(">"))
            {
                ++currentIndex;
                var rhs = ParseComparison();
                return Expression.GreaterThan(lhs, Expression.Convert(rhs, lhs.Type));
            }
            else if (CurrentToken.Equals("<"))
            {
                ++currentIndex;
                var rhs = ParseComparison();
                return Expression.LessThan(lhs, Expression.Convert(rhs, lhs.Type));
            }
            if (CurrentToken.Equals(">="))
            {
                ++currentIndex;
                var rhs = ParseComparison();
                return Expression.GreaterThanOrEqual(lhs, rhs);
            }
            if (CurrentToken.Equals("<="))
            {
                ++currentIndex;
                var rhs = ParseComparison();
                return Expression.LessThanOrEqual(lhs, rhs);
            }
            else
            {
                return lhs;
            }
        }

        public Expression ParseValue()
        {
            if(CurrentToken.Type == TokenType.Number)
            {
                var value = decimal.Parse(CurrentToken);
                return Expression.Constant(value);
            }
            else if(CurrentToken.Type == TokenType.True)
            {
                ++currentIndex;
                return Expression.Constant(true);
            }
            else if (CurrentToken.Type == TokenType.False)
            {
                ++currentIndex;
                return Expression.Constant(false);
            }
            else if (CurrentToken.Type == TokenType.Null)
            {
                ++currentIndex;
                return Expression.Constant(null);
            }

            return ParseIdentifier();
        }

        public Expression ParseIdentifier()
        {
            Expression baseExp = Obj;
            if(Eof)
            {
                return null;
            }

            do
            {
                if (Eof)
                {
                    return baseExp;
                }


                if (CurrentToken.Type != TokenType.Identifier)
                {
                    return null;
                }

                baseExp = Expression.Property(baseExp, CurrentToken);

                ++this.currentIndex;

                if(CurrentToken.Type == TokenType.Operator && CurrentToken.Equals("."))
                {
                    ++currentIndex;
                }

            } while (false);

            return baseExp;
        }
    }

    public sealed class Parser
    {
        public Expression<Func<T, bool>> Parse<T>(string expression)
        {
            var tokenizer = new Tokenizer(expression);

            var parameter = Expression.Parameter(typeof(T), "obj");

            var state = new ParserState(parameter, tokenizer.Tokenize());

            var body = state.Parse();

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }
}
