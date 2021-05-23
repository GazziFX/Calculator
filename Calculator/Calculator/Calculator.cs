/*
    Copyright (c) 2011-2012, Måns Andersson <mail@mansandersson.se>

    Permission to use, copy, modify, and/or distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/
using Calculator.Operands;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Calculator
{
    /// <summary>
    /// Modes to operate calculator in
    /// </summary>
    public enum CalculatorMode
    {
        /// <summary>Mathematics mode of calculator (XOR=XOR, AND=AND and so on, all division is floating point)</summary>
        Mathematics,
        /// <summary>Programming mode of calculator (XOR=^, AND=& and so on, all division is treated like in programming (e.g. int/int gives int)</summary>
        Programming,
    }

    /// <summary>
    /// Different types of tokens for quick identification
    /// </summary>
    public enum TokenType
    {
        /// <summary>Operator</summary>
        Operator,
        /// <summary>Left Parenthesis</summary>
        LeftParenthesis,
        /// <summary>RightParenthesis</summary>
        RightParenthesis,
        /// <summary>Value</summary>
        Value,
        /// <summary>Constant</summary>
        Constant,
        /// <summary>Function</summary>
        Function,
    }

    public struct Token
    {
        public string Value;
        public TokenType Type;
    }

    /// <summary>
    /// Calculator execution class
    /// </summary>
    public static class Calculator
    {
        /// <summary>
        /// Regular expression used for splitting the expression into tokens
        /// </summary>
        public static string GetTokenSplitRegex(CalculatorMode mode)
        {

            switch (mode)
            {
                default:
                case CalculatorMode.Mathematics:
                    return @"( AND | OR | XOR | NOT | CBRT |[%\+\-\*\^\(\)\/\ ])";
                case CalculatorMode.Programming:
                    return @"(<<|>>|[%\+\-\*\&\|\^\(\)\/\ ])";
            }
        }

        public static double Calculate(string input, CalculatorMode mode)
        {
            string[] text = Regex.Split(input, GetTokenSplitRegex(mode));
            List<Token> tokenList = new List<Token>(text.Length); // Let's split the input string into a token list
            for (int i = 0; i < text.Length; i++)
            {
                string str = text[i];
                if (string.IsNullOrWhiteSpace(str))
                    continue;

                str = str.Trim();
                var type = GetTokenType(str);
                tokenList.Add(new Token { Value = str, Type = type });
            }

            return Calculate(tokenList, mode);
        }

        /// <summary>
        /// Evaluate expression with Dijkstra's Shunting Yard Algorithm
        /// </summary>
        /// <returns>result of calculations</returns>
        public static double Calculate(List<Token> tokenList, CalculatorMode mode)
        {
            Stack<Op> operatorStack = new Stack<Op>();
            Queue<Op> outputQueue = new Queue<Op>();
            for (int tokenNum = 0; tokenNum < tokenList.Count; tokenNum++)
            {
                Token token = tokenList[tokenNum];

                // Handle this token and insert into the correct queue or stack
                switch (token.Type)
                {
                    case TokenType.Value:
                    case TokenType.Constant:
                        double? tmpValue = Operand.ParseValue(token.Value);
                        if (tmpValue.HasValue)
                            outputQueue.Enqueue(new Operand(tmpValue.Value));
                        else
                            throw new ArgumentException("Unknown operand " + token.Value);
                        break;
                    case TokenType.Operator:
                        Operator newOperator = GetOperator(token.Value, mode, tokenNum == 0 || tokenList[tokenNum - 1].Type == TokenType.LeftParenthesis);
                        if (operatorStack.Count > 0)
                        {
                            Op topOperator = operatorStack.Peek();
                            if (topOperator is Operator)
                            {
                                if (newOperator.Precedence <= ((Operator)topOperator).Precedence)
                                {
                                    outputQueue.Enqueue(operatorStack.Pop());
                                }
                            }
                        }
                        operatorStack.Push(newOperator);
                        break;
                    case TokenType.LeftParenthesis:
                        operatorStack.Push(new LeftParenthesis());
                        break;
                    case TokenType.RightParenthesis:
                        // Handle all operators in the stack (i.e. move them to the outputQueue)
                        // until we find the LeftParenthesis
                        while (!(operatorStack.Peek() is LeftParenthesis))
                        {
                            outputQueue.Enqueue(operatorStack.Pop());
                        }
                        operatorStack.Pop();
                        break;
                    case TokenType.Function:
                        if ((tokenList.Count >= tokenNum + 1) && (tokenList[tokenNum + 1].Type == TokenType.LeftParenthesis))
                        {
                            Function.FunctionTypes type = Function.GetFunctionType(token.Value.ToLower());
                            if (type == Function.FunctionTypes.UNKNOWN)
                            {
                                throw new ArgumentException("Unknown function " + token.Value);
                            }
                            operatorStack.Push(new Function(type));
                        }
                        break;
                }

                // If we don't find any token between a value and parenthesis, automatically
                // add a multiply sign
                if (token.Type == TokenType.Value || token.Type == TokenType.RightParenthesis)
                {
                    if (tokenNum < tokenList.Count - 1)
                    {
                        Token nextToken = tokenList[tokenNum + 1];
                        if (nextToken.Type != TokenType.Operator && nextToken.Type != TokenType.RightParenthesis)
                        {
                            tokenList.Insert(tokenNum + 1, new Token { Value = "*", Type = TokenType.Operator });
                        }
                    }
                }
            }

            // Move all operators into the outputqueue
            while (operatorStack.Count > 0)
            {
                Op operand = operatorStack.Pop();
                if (operand is LeftParenthesis || operand is RightParenthesis)
                {
                    throw new ArgumentException("Mismatched parentheses");
                }

                outputQueue.Enqueue(operand);
            }

            // Now we have the expression in reverse polish notation and it's easy to calculate
            // Step through the outputQueue and calculate the result
            Stack<Operand> outputStack = new Stack<Operand>();
            while (outputQueue.Count > 0)
            {
                Op currentOp = outputQueue.Dequeue();

                if (currentOp is Operand)
                {
                    outputStack.Push((Operand)currentOp);
                }
                else if (currentOp is Operator)
                {
                    Operator currentOperator = (Operator)currentOp;
                    currentOperator.Execute(outputStack, mode);
                }
            }

            // If we haven't got only one answer, the formula is invalid, return that.
            if (outputStack.Count != 1)
            {
                throw new ArgumentException("Invalid formula");
            }

            // Pop and return the result
            return outputStack.Pop().Value;
        }

        /// <summary>
        /// Checks which token type a token belongs to
        /// </summary>
        /// <param name="token">token to check</param>
        /// <returns>the token type for the token</returns>
        private static TokenType GetTokenType(string token)
        {
            if (Operand.ParseValue(token).HasValue)
                return TokenType.Value;

            switch (token)
            {
                case "(":
                    return TokenType.LeftParenthesis;
                case ")":
                    return TokenType.RightParenthesis;
                case "+":
                case "-":
                case "*":
                case "/":
                case "^":
                case "&":
                case "|":
                case "%":
                case "<<":
                case ">>":
                case "AND":
                case "OR":
                case "XOR":
                case "NOT":
                    return TokenType.Operator;
            }

            if (Regex.IsMatch(token, @"^[0-9A-Za-z]+$"))
                return TokenType.Function;

            throw new ArgumentException("Invalid token");
        }

        /// <summary>
        /// Get an operator object representing a token
        /// </summary>
        /// <param name="token">token to represent as an object</param>
        /// <param name="isFirstToken">whether this token is the first (first in expression, or first after a new sub-expression)</param>
        /// <returns>operator object corresponding to token</returns>
        private static Operator GetOperator(string token, CalculatorMode mode, bool isFirstToken)
        {
            if (mode == CalculatorMode.Mathematics)
            {
                switch (token)
                {
                    case "+":
                        return new Plus();
                    case "-":
                        if (isFirstToken)
                            return new Negative();
                        else
                            return new Minus();
                    case "*":
                        return new Multiplication();
                    case "/":
                        return new Div();
                    case "%":
                        return new Modulus();
                    case "^":
                        return new Exponent();
                    case "AND":
                        return new And();
                    case "OR":
                        return new Or();
                    case "XOR":
                        return new Xor();
                    case "NOT":
                        return new Not();
                    default:
                        throw new ArgumentException("Unknown operator " + token);
                }
            }
            else if (mode == CalculatorMode.Programming)
            {
                switch (token)
                {
                    case "+":
                        return new Plus();
                    case "-":
                        if (isFirstToken)
                            return new Negative();
                        else
                            return new Minus();
                    case "*":
                        return new Multiplication();
                    case "/":
                        return new Div();
                    case "<<":
                        return new LeftShift();
                    case ">>":
                        return new RightShift();
                    case "%":
                        return new Modulus();
                    case "&":
                        return new And();
                    case "|":
                        return new Or();
                    case "^":
                        return new Xor();
                    case "~":
                        return new Not();
                    default:
                        throw new ArgumentException("Unknown operator " + token);
                }
            }
            else
            {
                throw new ArgumentException("Unknown calculator mode");
            }
        }
    }
}
