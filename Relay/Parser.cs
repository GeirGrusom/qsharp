
namespace Relay
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using static System.Linq.Expressions.Expression;

    internal class ParserState
    {
        private readonly Func<Type, string, PropertyInfo> propertyLookup;
        public readonly Token[] Tokens;
        public readonly ParameterExpression Obj;

        private int currentIndex;

        public ref Token CurrentToken => ref Tokens[currentIndex];

        public bool Eof => currentIndex >= Tokens.Length;

        public ParserState(ParameterExpression obj, Func<Type, string, PropertyInfo> propertyLookup, IEnumerable<Token> tokens)
        {
            this.Tokens = tokens.Where(t => t.Type != TokenType.Whitespace).ToArray();
            this.propertyLookup = propertyLookup;
            this.Obj = obj;
        }

        private Expression Coerce(Expression exp, Expression targetType)
        {
            if(exp.Type == targetType.Type)
            {
                return exp;
            }
            if(exp is ConstantExpression con)
            {
                return Constant(System.Convert.ChangeType(con.Value, targetType.Type));
            }
            else
            {
                return Convert(exp, targetType.Type);
            }
        }

        public Expression Parse()
        {
            var firstError = this.Tokens.Select(x => (Token?)x).FirstOrDefault(x => x.Value.Type == TokenType.Error);
                
            if(firstError != null)
            {
                throw new ParseException($"Invalid character sequence '{firstError.Value}'", firstError.Value.Offset);
            }

            this.currentIndex = 0;
            var result = ParseOr();
            if(CurrentToken.Type != TokenType.EndOfFile)
            {
                throw new ParseException("Expected end of statement", this.CurrentToken.Offset);
            }

            return result;
        }

        public Expression ParseOr()
        {
            var lhs = ParseAnd();
            
            if(!Eof && (CurrentToken.Equals("or") || CurrentToken.Equals(",")))
            {
                ++currentIndex;
                var rhs = ParseOr();
                return OrElse(lhs, rhs);
            }
            else
            {
                return lhs;
            }
        }

        public Expression ParseAnd()
        {
            var lhs = ParseEquality();

            if (!Eof && (CurrentToken.Equals("and") || CurrentToken.Equals(";")))
            {
                ++currentIndex;
                var rhs = ParseAnd();
                return AndAlso(lhs, rhs);
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
                return Equal(lhs, Coerce(rhs, lhs));
            }
            else if (CurrentToken.Equals("<>") || CurrentToken.Equals("!="))
            {
                ++currentIndex;
                var rhs = ParseComparison();
                return Not(Equal(lhs, Coerce(rhs, lhs)));
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
                return GreaterThan(lhs, Coerce(rhs, lhs));
            }
            else if (CurrentToken.Equals("<"))
            {
                ++currentIndex;
                var rhs = ParseComparison();
                return LessThan(lhs, Coerce(rhs, lhs));
            }
            if (CurrentToken.Equals(">="))
            {
                ++currentIndex;
                var rhs = ParseComparison();
                return GreaterThanOrEqual(lhs, Coerce(rhs, lhs));
            }
            if (CurrentToken.Equals("<="))
            {
                ++currentIndex;
                var rhs = ParseComparison();
                return LessThanOrEqual(lhs, Coerce(rhs, lhs));
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
                ++currentIndex;
                return Constant(value);
            }
            else if(CurrentToken.Type == TokenType.True)
            {
                ++currentIndex;
                return Constant(true);
            }
            else if (CurrentToken.Type == TokenType.False)
            {
                ++currentIndex;
                return Constant(false);
            }
            else if (CurrentToken.Type == TokenType.Null)
            {
                ++currentIndex;
                return Constant(null);
            }
            else if(CurrentToken.Type == TokenType.DateTime)
            {
                var dt = DateTimeOffset.Parse(CurrentToken.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None);
                ++currentIndex;
                return Constant(dt);
            }
            else if(CurrentToken.Type == TokenType.String)
            {
                var result = new StringBuilder(CurrentToken.Length - 2);
                bool lastWasSlash = false;
                for(int i = CurrentToken.Offset + 1; i < CurrentToken.Offset + CurrentToken.Length - 1; ++i)
                {
                    char c = CurrentToken.Source[i];
                    if(c == '\\')
                    {
                        if (lastWasSlash)
                        {
                            lastWasSlash = false;
                            result.Append(c);
                        }
                        else
                        {
                            lastWasSlash = true;
                        }                        
                    }
                    else if(lastWasSlash)
                    {
                        lastWasSlash = false;
                        switch(c)
                        {
                            case 'n':
                                result.Append('\n');
                                break;
                            case 'r':
                                result.Append('\r');
                                break;
                            case 'b':
                                result.Append('\b');
                                break;
                            case 't':
                                result.Append('\t');
                                break;
                            case 'v':
                                result.Append('\v');
                                break;
                            default:
                                if (c != CurrentToken.Source[CurrentToken.Offset])
                                {
                                    result.Append('\\');
                                }
                                result.Append(c);
                                break;
                        }
                    }
                    else
                    {
                        result.Append(c);
                    }
                    
                }
                ++currentIndex;
                return Constant(result.ToString());
            }

            return ParseIdentifier();
        }

        public Expression ParseIdentifier()
        {
            Type GetNullableType(Type t)
            {
                if(t.IsClass)
                {
                    return t;
                }
                return typeof(Nullable<>).MakeGenericType(t);
            }

            Expression baseExp = Obj;
            if(Eof)
            {
                throw new ParseException("Expected identifier", CurrentToken.Offset);
            }
            bool nullCheck = false;
            do
            {
                if (Eof)
                {
                    return baseExp;
                }


                if (CurrentToken.Type != TokenType.Identifier)
                {
                    throw new ParseException("Expected identifier", CurrentToken.Offset);
                }

                var propertyInfo = this.propertyLookup(baseExp.Type, CurrentToken);

                if(propertyInfo is null)
                {
                    throw new ParseException($"Unrecognized member '{CurrentToken}' of '{baseExp.Type.Name}'", CurrentToken.Offset);
                }

                if (nullCheck)
                {
                    var p = Property(baseExp, propertyInfo);
                    baseExp = IfThenElse(ReferenceEqual(baseExp, Constant(null, baseExp.Type)), Constant(null, GetNullableType(p.Type)), p);
                }
                else
                {
                    baseExp = Property(baseExp, propertyInfo);
                }

                ++this.currentIndex;

                if(Eof)
                {
                    break;
                }

                nullCheck = CurrentToken.Equals("?.");
                if (CurrentToken.Type == TokenType.Operator && (CurrentToken.Equals(".") || nullCheck))
                {
                    ++currentIndex;
                    continue;
                }
                break;

            } while (true);

            return baseExp;
        }
    }

    public interface ITypeConfigure<TOwner>
    {
        ITypeConfigure<TOwner> Whitelist<TResult>(Expression<Func<TOwner, TResult>> property);
        ITypeConfigure<TOwner> Whitelist();
    }

    public sealed class Parser
    {
        private readonly Dictionary<Type, Dictionary<string, PropertyInfo>> propertyWhitelist;

        public Parser()
        {
            propertyWhitelist = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
        }

        private sealed class TypeConfigure<TOwner> : ITypeConfigure<TOwner>
        {
            private readonly Parser parser;

            public TypeConfigure(Parser parser)
            {
                this.parser = parser;
            }

            public ITypeConfigure<TOwner> Whitelist<TResult>(Expression<Func<TOwner, TResult>> property)
            {
                if (!parser.propertyWhitelist.TryGetValue(typeof(TOwner), out var dict))
                {
                    dict = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
                    parser.propertyWhitelist[typeof(TOwner)] = dict;
                }
                var member = (PropertyInfo)((MemberExpression)property.Body).Member;
                dict[member.Name] = member;
                return this;
            }

            public ITypeConfigure<TOwner> Whitelist()
            {
                if (!parser.propertyWhitelist.TryGetValue(typeof(TOwner), out var dict))
                {
                    dict = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
                    parser.propertyWhitelist[typeof(TOwner)] = dict;
                }

                foreach(var prop in typeof(TOwner).GetProperties())
                {
                    dict[prop.Name] = prop;
                }
                return this;
            }
        }

        public Parser Configure<T>(Action<ITypeConfigure<T>> config)
        {
            var cfg = new TypeConfigure<T>(this);
            config(cfg);
            return this;
        }

        private PropertyInfo LookupProperty(Type owner, string propertyName)
        {
            if(!propertyWhitelist.TryGetValue(owner, out var whitelist) || !whitelist.TryGetValue(propertyName, out var propInfo))
            {
                return null;
            }

            return propInfo;
        }

        public Expression<Func<T, bool>> Parse<T>(string expression)
        {
            var tokenizer = new Tokenizer(expression);

            var parameter = Parameter(typeof(T), "obj");

            var state = new ParserState(parameter, LookupProperty, tokenizer.Tokenize());

            var body = state.Parse();

            return Lambda<Func<T, bool>>(body, parameter);
        }
    }
}
