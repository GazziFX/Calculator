using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace Calculator
{
    public partial class MainPage : ContentPage
    {
        private ExtraOperationsPage extraPopupPage;
        private Color textColor;
        private List<Token> tokenList = new List<Token>(16);
        private StringBuilder charBuffer = new StringBuilder(128);

        public void UpdateText()
        {
            var buffer = charBuffer;
            buffer.Clear();
            var list = tokenList;
            for (int i = 0; i < list.Count; i++)
            {
                buffer.Append(list[i].Value);
            }
            ResultLabel.TextColor = textColor;
            ResultLabel.Text = buffer.ToString();
        }

        public void AppendToken(Token token)
        {
            tokenList.Add(token);
            UpdateText();
        }

        public void AppendTokens(IEnumerable<Token> tokens)
        {
            tokenList.AddRange(tokens);
            UpdateText();
        }

        public void Clear()
        {
            tokenList.Clear();
            ResultLabel.Text = string.Empty;
        }

        public MainPage()
        {
            InitializeComponent();
            textColor = ResultLabel.TextColor;
            extraPopupPage = new ExtraOperationsPage(this);
        }

        public void ParenthesisButtonClicked(object sender, EventArgs e)
        {
            var text = (sender as Button).Text;
            TokenType type;
            if (text == "(")
                type = TokenType.LeftParenthesis;
            else if (text == ")")
                type = TokenType.RightParenthesis;
            else
                throw new ArgumentException("Invalid parenthesis");

            AppendToken(new Token { Value = text, Type = type });
        }

        public void ConstantButtonClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            AppendToken(new Token { Value = button.Text, Type = TokenType.Constant });
        }

        public void OperatorButtonClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            AppendToken(new Token { Value = button.Text, Type = TokenType.Operator });
        }

        public void FunctionButtonClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            tokenList.Add(new Token { Value = button.Text, Type = TokenType.Function });
            tokenList.Add(new Token { Value = "(", Type = TokenType.LeftParenthesis });
            UpdateText();
        }

        public void ValueButtonClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            int index = tokenList.Count - 1;
            if (index >= 0)
            {
                var lastToken = tokenList[index];
                if (lastToken.Type == TokenType.Value)
                {
                    tokenList[index] = new Token { Value = lastToken.Value + button.Text, Type = TokenType.Value };
                    UpdateText();
                    return;
                }
            }

            AppendToken(new Token { Value = button.Text, Type = TokenType.Value });
        }

        private void RemoveClicked(object sender, EventArgs e)
        {
            int index = tokenList.Count - 1;
            if (index < 0)
                return;

            var lastToken = tokenList[index];
            if (lastToken.Type == TokenType.Value && lastToken.Value.Length > 1)
                tokenList[index] = new Token { Value = lastToken.Value.Substring(0, lastToken.Value.Length - 1), Type = TokenType.Value };
            else
                tokenList.RemoveAt(index);

            UpdateText();
        }

        private void ClearClicked(object sender, EventArgs e)
        {
            Clear();
        }

        private void CalculateClicked(object sender, EventArgs e)
        {
            try
            {
                double result = Calculator.Calculate(tokenList, CalculatorMode.Mathematics);
                tokenList.Clear();
                AppendToken(new Token { Value = result.ToString(), Type = TokenType.Value });
                UpdateText();
            }
            catch (Exception ex)
            {
                ResultLabel.TextColor = Color.Red;
                ResultLabel.Text = "Error: " + ex.Message;
                tokenList.Clear();
            }
        }

        private void MoreOperationsClicked(object sender, EventArgs e)
        {
            var page = extraPopupPage;
            if (!PopupNavigation.Instance.PopupStack.Contains(page))
                PopupNavigation.Instance.PushAsync(page);
        }
    }
}
