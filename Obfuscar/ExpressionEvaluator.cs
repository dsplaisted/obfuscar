using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Obfuscar
{
    class ExpressionEvaluator
    {
        public static bool Evaluate(string expression, Func<string, bool> valueGetter)
        {
            Tokenizer tokenizer = new Tokenizer(expression);
            bool ret = EvaluateExpression(tokenizer, valueGetter);

            if (tokenizer.CurrentToken != null)
            {
                throw new FormatException(
                    $"Unexpected token '{tokenizer.CurrentToken.Text}' at position {tokenizer.CurrentToken.Position}");
            }

            return ret;
        }

        static bool EvaluateExpression(Tokenizer tokenizer, Func<string, bool> valueGetter)
        {
            bool ret = EvaluateSubExpression(tokenizer, valueGetter);

            while (tokenizer.CurrentToken != null && (tokenizer.CurrentToken.Type == TokenType.BinaryAnd ||
                                                      tokenizer.CurrentToken.Type == TokenType.BinaryOr))
            {
                var operatorTokenType = tokenizer.CurrentToken.Type;
                int opPosition = tokenizer.CurrentToken.Position;
                tokenizer.Consume();

                bool rightHandVal = EvaluateSubExpression(tokenizer, valueGetter);

                if (operatorTokenType == TokenType.BinaryAnd)
                {
                    ret = ret && rightHandVal;
                }
                else if (operatorTokenType == TokenType.BinaryOr)
                {
                    ret = ret || rightHandVal;
                }
                else
                {
                    throw new FormatException($"Unrecognized binary operator '{operatorTokenType}' at position {opPosition}");
                }
            }

            return ret;
        }

        static bool EvaluateSubExpression(Tokenizer tokenizer, Func<string, bool> valueGetter)
        {

            if (tokenizer.CurrentToken == null)
            {
                throw new FormatException("Unexpected end of expression");
            }
            else if (tokenizer.CurrentToken.Type == TokenType.OpenParen)
            {
                tokenizer.Consume();
                var ret = EvaluateExpression(tokenizer, valueGetter);
                tokenizer.Consume(TokenType.CloseParen);
                return ret;
            }
            else if (tokenizer.CurrentToken.Type == TokenType.UnaryNot)
            {
                tokenizer.Consume();
                return !EvaluateSubExpression(tokenizer, valueGetter);
            }
            else if (tokenizer.CurrentToken.Type == TokenType.Name)
            {
                bool val = valueGetter(tokenizer.CurrentToken.Text);
                tokenizer.Consume();

                return val;
            }
            else
            {
                throw new FormatException(
                    $"Unexpected token '{tokenizer.CurrentToken.Text}' at position {tokenizer.CurrentToken.Position}");
            }
        }




        enum TokenType
        {
            None,
            OpenParen,
            CloseParen,
            Name,
            BinaryAnd,
            BinaryOr,
            UnaryNot,
        }

        class Token
        {
            public TokenType Type { get; set; }
            public string Text { get; set; }
            public int Position { get; set; }
        }

        class Tokenizer
        {
            private readonly string _expression;
            private int _currentIndex;
            private Token _currentToken;

            public Tokenizer(string expression)
            {
                _expression = expression;
                _currentIndex = 0;
                Read();
            }

            public Token CurrentToken { get { return _currentToken; } }

            public Token Consume()
            {
                if (CurrentToken == null)
                {
                    throw new FormatException("Unexpected end of expression");
                }
                var ret = CurrentToken;
                Read();
                return ret;
            }

            public Token Consume(TokenType tokenType)
            {
                if (CurrentToken == null)
                {
                    throw new FormatException("Unexpected end of expression");
                }
                if (CurrentToken.Type != tokenType)
                {
                    throw new FormatException(
                        $"Expected {tokenType} but got {CurrentToken.Type} at position {CurrentToken.Position}");
                }
                return Consume();
            }

            void Read()
            {
                StringBuilder text = new StringBuilder();
                while (_currentIndex < _expression.Length && char.IsWhiteSpace(_expression[_currentIndex]))
                {
                    _currentIndex++;
                }
                if (_currentIndex >= _expression.Length)
                {
                    _currentToken = null;
                    return;
                }

                _currentToken = new Token();
                _currentToken.Position = _currentIndex;

                char ch = _expression[_currentIndex];
                if (isNameChar(ch))
                {
                    _currentToken.Type = TokenType.Name;
                    _currentToken.Text = ConsumeCharsWhile(isNameChar);
                    if (_currentToken.Text.Equals("and", StringComparison.OrdinalIgnoreCase))
                    {
                        _currentToken.Type = TokenType.BinaryAnd;
                    }
                    else if (_currentToken.Text.Equals("or", StringComparison.OrdinalIgnoreCase))
                    {
                        _currentToken.Type = TokenType.BinaryOr;
                    }
                }
                else if (ch == '(')
                {
                    _currentToken.Type = TokenType.OpenParen;
                    _currentToken.Text = ConsumeChar();
                }
                else if (ch == ')')
                {
                    _currentToken.Type = TokenType.CloseParen;
                    _currentToken.Text = ConsumeChar();
                }
                else if (ch == '&')
                {
                    _currentToken.Type = TokenType.BinaryAnd;
                    _currentToken.Text = ConsumeChar();
                }
                else if (ch == '|')
                {
                    _currentToken.Type = TokenType.BinaryOr;
                    _currentToken.Text = ConsumeChar();
                }
                else if (ch == '!')
                {
                    _currentToken.Type = TokenType.UnaryNot;
                    _currentToken.Text = ConsumeChar();
                }
                else
                {
                    throw new FormatException($"Unexpected character '{ch}' at position {_currentIndex}");
                }

            }

            string ConsumeCharsWhile(Func<char, bool> charTester)
            {
                StringBuilder sb = new StringBuilder();

                while (_currentIndex < _expression.Length && charTester(_expression[_currentIndex]))
                {
                    sb.Append(_expression[_currentIndex]);
                    _currentIndex++;
                }

                return sb.ToString();
            }

            string ConsumeChar()
            {
                char ch = _expression[_currentIndex];
                _currentIndex++;
                return ch.ToString();
            }

            bool isNameChar(char ch)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    return true;
                }
                else if (ch == '_' | ch == '.')
                {
                    return true;
                }
                return false;
            }
        }
    }
}
