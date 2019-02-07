using System;
using System.Collections.Generic;
using System.Text;

namespace Relay
{
    public enum TokenType
    {
        Error,
        Whitespace,
        Identifier,
        Number,
        Operator,
        True,
        False,
        Null,
        String,
        DateTime,
    }

    public readonly struct Token : IEquatable<Token>, IEquatable<string>
    {
        public readonly string Source;
        public readonly int Offset;
        public readonly int Length;
        public readonly TokenType Type;

        public Token(string source, int offset, int length, TokenType type)
        {
            this.Source = source;
            this.Offset = offset;
            this.Length = length;
            this.Type = type;
        }

        public bool Equals(Token other)
        {
            return other.Source == Source && other.Offset == Offset && other.Length == Length;
        }

        public static implicit operator string(Token token)
        {
            return token.ToString();
        }

        public bool Equals(string other)
        {
            if(other.Length != this.Length)
            {
                return false;
            }

            for(int i = 0; i < Length; ++i)
            {
                if(other[i] != Source[Offset + i])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object other)
        {
            if(other is Token tok)
            {
                return this.Equals(tok);
            }
            if(other is string s)
            {
                return this.Equals(s);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return 2049 * Offset + Length;            
        }        

        public override string ToString()
        {
            if(Source is null)
            {
                return "";
            }

            return Source.Substring(Offset, Length);
        }
    }
}
