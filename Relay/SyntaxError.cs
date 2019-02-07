using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Relay
{
    public readonly struct SyntaxError
    {
        public readonly Token Token;
        public readonly string Message;

        public SyntaxError(in Token token, string message)
        {
            this.Token = token;
            this.Message = message;
        }

        public override string ToString()
        {
            return $"{Token.Offset}: {Message} ({Token})";
        }
    }

    public sealed class ParseException : Exception
    {
        public int Position { get; }

        public ParseException(string message, int position)
            : base(message)
        {
            this.Position = position;
        }
    }
}
