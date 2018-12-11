using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Text.RegularExpressions;

namespace JParse.Core
{
    public class CombinedParser
    {
        private readonly string _input;
        private int _index;

        public CombinedParser(string input)
        {
            _input = input;
        }

        public static object Parse(string json)
        {
            var parser = new CombinedParser(json);
            return parser.Parse();
        }

        public object Parse()
        {
            return Element();
        }

        private object Element()
        {
            EatWhitespace();
            var value = Value();
            EatWhitespace();
            return value;
        }

        private object Value()
        {
            var (value, success) = Try(Object);
            if (success) return value;

            (value, success) = Try(Array);
            if (success) return value;

            (value, success) = Try(String);
            if (success) return value;

            (value, success) = Try(Number);
            if (success) return value;

            if (Peek(4) == "true")
            {
                Advance(4);
                return true;
            }

            if (Peek(5) == "false")
            {
                Advance(5);
                return false;
            }

            if (Peek(4) == "null")
            {
                Advance(4);
                return null;
            }

            throw new ParseException();
        }

        private object Object()
        {
            if (Peek() != '{') throw new ParseException();
            Advance();

            var obj = new ExpandoObject();
            var objCollection = (ICollection<KeyValuePair<string, object>>) obj;

            EatWhitespace();

            var (member, success) = Try(Member);

            while (success)
            {
                objCollection.Add(member);
                if (Peek() == ',')
                    Advance();
                else
                    break;
                (member, success) = Try(Member);
            }

            if (Peek() != '}') throw new ParseException();
            Advance();

            return obj;
        }

        private KeyValuePair<string, object> Member()
        {
            EatWhitespace();

            var str = String();

            EatWhitespace();

            if (Peek() != ':') throw new ParseException();
            Advance();

            var elem = Element();

            return new KeyValuePair<string, object>(str, elem);
        }

        private object[] Array()
        {
            if (Peek() != '[') throw new ParseException();
            Advance();

            var list = new ArrayList();

            EatWhitespace();

            var (element, success) = Try(Element);

            while (success)
            {
                list.Add(element);
                if (Peek() == ',')
                    Advance();
                else
                    break;
                (element, success) = Try(Element);
            }

            if (Peek() != ']') throw new ParseException();
            Advance();

            return list.ToArray();
        }

        private string String()
        {
            if (Peek() != '"') throw new ParseException();
            Advance();

            var sb = new StringBuilder();

            var (nextChar, success) = Try(Character);

            while (success)
            {
                sb.Append(nextChar);
                (nextChar, success) = Try(Character);
            }

            if (Peek() != '"') throw new ParseException();
            Advance();

            return sb.ToString();
        }

        private char Character()
        {
            if (PeekChar()) return Eat().GetValueOrDefault();

            if (Peek() != '\\') throw new ParseException();
            Advance();

            switch (Peek())
            {
                case '"': Advance(); return '\"';
                case '\\': Advance(); return '\\';
                case '/': Advance(); return '/';
                case 'b': Advance(); return '\b';
                case 'n': Advance(); return '\n';
                case 'r': Advance(); return '\r';
                case 't': Advance(); return '\t';
                case 'u':
                    Advance();
                    if (!PeekHexDigit()) throw new ParseException();
                    var hd1 = Eat().GetValueOrDefault();
                    if (!PeekHexDigit()) throw new ParseException();
                    var hd2 = Eat().GetValueOrDefault();
                    if (!PeekHexDigit()) throw new ParseException();
                    var hd3 = Eat().GetValueOrDefault();
                    if (!PeekHexDigit()) throw new ParseException();
                    var hd4 = Eat().GetValueOrDefault();
                    var str = new string(new[] {'\\', 'u', hd1, hd2, hd3, hd4});
                    return char.Parse(Regex.Unescape(str));
                default:
                    throw new ParseException();
            }
        }

        private double Number()
        {
            var intPart = Int();
            var (fracPart, hasFrac) = Try(Frac);
            var (expPart, hasExp) = Try(Exp);

            var baseString = hasFrac ? intPart + "." + fracPart : intPart;
            var resultString = hasExp ? baseString + "e" + expPart : baseString;

            return double.Parse(resultString);
        }

        private string Int()
        {
            // C# number parsing wil handle negation
            var sb = new StringBuilder();
            if (Peek() == '-') sb.Append(Eat());

            // Make sure there is at least one number
            if (!PeekDigit()) throw new ParseException();

            if (Peek() == '0')
            {
                sb.Append(Eat());
                return sb.ToString();
            }

            // Get the rest of the digits
            while (PeekDigit())
            {
                sb.Append(Eat());
            }

            return sb.ToString();
        }

        private string Frac()
        {
            if (Peek() != '.') throw new ParseException();
            Advance();

            // Make sure there is at least one number
            if (!PeekDigit()) throw new ParseException();

            var sb = new StringBuilder();

            // Get the rest of the digits
            while (PeekDigit())
            {
                sb.Append(Eat());
            }

            return sb.ToString();
        }

        private string Exp()
        {
            if (Peek() != 'e' && Peek() != 'E') throw new ParseException();
            Advance();

            var sb = new StringBuilder();
            if (Peek() == '+' || Peek() == '-') sb.Append(Eat());

            // Make sure there is at least one number
            if (!PeekDigit()) throw new ParseException();

            // Get the rest of the digits
            while (PeekDigit())
            {
                sb.Append(Eat());
            }

            return sb.ToString();
        }

        #region Helpers

        private char? Peek() => _input.Length > _index ? _input[_index] : (char?) null;

        private string Peek(int range) => _input.Length >= _index + range ? _input.Substring(_index, range) : null;

        private bool PeekDigit()
        {
            var peek = Peek();
            return peek != null && char.IsDigit(peek.Value);
        }

        private bool PeekHexDigit()
        {
            var peek = Peek();
            return peek >= 'A' && peek <= 'F' ||
                   peek >= 'a' && peek <= 'f' ||
                   PeekDigit();
        }

        private bool PeekChar()
        {
            var peek = Peek();
            return peek != null &&
                   peek >= 0x0020 &&
                   peek <= 0x10ffff &&
                   peek != '"' &&
                   peek != '\\';
        }

        private void Advance() => _index++;

        private void Advance(int range) => _index += range;

        private char? Eat()
        {
            var value = Peek();
            Advance();
            return value;
        }

        private void EatWhitespace()
        {
            while (Peek() == 0x0009 ||
                   Peek() == 0x000a ||
                   Peek() == 0x000d ||
                   Peek() == 0x0020)
            {
                Advance();
            }
        }

        private (T Value, bool Success) Try<T>(Func<T> func)
        {
            var i = _index;
            try
            {
                return (func(), true);
            }
            catch (ParseException)
            {
                _index = i;
                return (default, false);
            }
            // TODO: Remove
            catch (NotSupportedException)
            {
                _index = i;
                return (default, false);
            }
        }

        #endregion
    }
}
