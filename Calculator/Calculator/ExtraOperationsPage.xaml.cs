using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Calculator
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ExtraOperationsPage : Rg.Plugins.Popup.Pages.PopupPage
    {
        private class SpecialButton
        {
            MainPage Owner;
            Token[] Tokens;

            public SpecialButton(MainPage owner, Token[] tokens)
            {
                Tokens = tokens;
                Owner = owner;
            }

            public void FunctionButtonClicked(object sender, EventArgs e)
            {
                Owner.AppendTokens(Tokens);
            }
        }

        public ExtraOperationsPage(MainPage owner)
        {
            InitializeComponent();
            CuberRootButton.Clicked += new SpecialButton(owner,
                new Token[] {
                    new Token { Value = "cbrt", Type = TokenType.Function },
                    new Token { Value = "(", Type = TokenType.LeftParenthesis }
                }).FunctionButtonClicked;
            Pow10Button.Clicked += new SpecialButton(owner,
                new Token[] {
                    new Token { Value = "pow10", Type = TokenType.Function },
                    new Token { Value = "(", Type = TokenType.LeftParenthesis }
                }).FunctionButtonClicked;
            AtanButton.Clicked += owner.FunctionButtonClicked;
            PiButton.Clicked += owner.ConstantButtonClicked;
        }
    }
}