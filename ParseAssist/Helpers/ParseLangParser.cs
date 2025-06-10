using Avalonia.Media;
using Avalonia.Media.Immutable;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextMateSharp.Grammars;

namespace ParseAssist.Helpers
{
    public class ParseLangParser
    {
        private class Fragment
        {
            public int start;
            public int size;
            public string color;
            public Stack<string> parameters = new Stack<string>();

            public void AppendParam(string param)
            {
                parameters.Push(param);
            }

            private static byte TryConvertHex(string color, string def)
            {
                try
                {
                    return Convert.ToByte(color.Substring(0, 2), 16);
                }
                catch
                {
                    return Convert.ToByte(def, 16);
                }
            }

            public static Color HexToColor(string color)
            {
                byte r, g, b, a;

                r = TryConvertHex(color.Substring(0, 2), "FF");
                g = TryConvertHex(color.Substring(2, 2), "FF");
                b = TryConvertHex(color.Substring(4, 2), "FF");
                a = TryConvertHex(color.Substring(6, 2), "FF");

                return new Color(a, r, g, b);
            }
        }

        private struct PLToken
        {
            // TODO, implement requires
            public char token;        // Token Identifier (\v for variable, \t for numbers, \0 for invalid)
            public char requires;     // required predicesor token (\0 means no tokens on the stack and \r for any element)
            public char pops;         // Token to remove from the stack until "pops" token is reached and removed (\0 for pops nothing, \a for pops everything)
        }

        private string ParseData { get; set; }

        private List<PLToken> SyntaxTokens = new List<PLToken>
        {
            new PLToken { token = '\\', requires = '\0', pops = '\0' },     // START TOKEN, FIRST ELEMENT ON STACK
            new PLToken { token = '<' , requires = '\0', pops = '\0' },
            new PLToken { token = '>' , requires = '<' , pops = '<'  },
            new PLToken { token = '$' , requires = '\0', pops = '\0' },
            new PLToken { token = '\v', requires = '\r', pops = '\0' },     // Variables
            new PLToken { token = '\t', requires = '[' , pops = '\0' },     // Numbers
            new PLToken { token = '@' , requires = '\v', pops = '\0' },
            new PLToken { token = '{' , requires = '\t', pops = '\0' },
            new PLToken { token = '}' , requires = '{' , pops = '$'  },
            new PLToken { token = '[' , requires = '{' , pops = '\0' },
            new PLToken { token = ']' , requires = '[' , pops = '[' },
            new PLToken { token = ';' , requires = '\v', pops = '\0' },  
        };

        private List<string> Keywords = new List<string>
        {
            "color",
            "text",
        };

        private List<Fragment> Fragments = new List<Fragment>();

        // Stacks

        private Stack<char> Tokens = new Stack<char>();
        private Stack<string> Variables = new Stack<string>();
        private Stack<int> Integers = new Stack<int>();
        private Stack<char> Contexts = new Stack<char>();

        private StringBuilder VarBuilder = new StringBuilder(); 

        // Helper Functions
        private void EndVariableBuild(ref Fragment frag)
        {
            if (VarBuilder.Length > 0)
            {
                if (Tokens.Peek() == '\v')
                    Variables.Push(VarBuilder.ToString());
                else if (Tokens.Peek() == '\t')
                    Integers.Push(Convert.ToInt32(VarBuilder.ToString()));

                VarBuilder.Clear();
            }
        }

        private void PopStackTokens(ref PLToken refToken, ref Fragment frag)
        {
            while (refToken.token != refToken.pops && refToken.pops != '\0')
            {
                // Pop next token on stack
                Tokens.Pop();
                
                // Set next token in line
                refToken.token = Tokens.Peek();

                // Pop if context matches
                PopContext(refToken.token);

                // Check special cases
                if (refToken.token == '@')
                {
                    Fragments.Add(frag);
                    frag = new Fragment();
                }
                else if (refToken.token == '\t')
                {
                    frag.size += frag.start;
                    frag.start = Integers.Pop();
                }
                else if (refToken.token == '\v')
                {
                    if (GetCurrentContext() == '<')
                        frag.parameters.Push(Variables.Peek());

                    Variables.Pop();
                }
            }
        }

        private void PushContext(ref PLToken refToken, char c)
        {
            // if token type is NOT a "closer" then push the context
            if (refToken.pops == '\0' && refToken.token != '\t' && refToken.token != '\v')
                Contexts.Push(c);
        }

        private void PopContext(char c)
        {
            // If token found as next in context, pop it
            if (c == GetCurrentContext())
                Contexts.Pop();
        }

        public ParseLangParser(string _data)
        {
            Tokens.Push('\\');
            Contexts.Push('\\');

            this.ParseData = _data;
            PLToken refToken = new PLToken();
            Fragment frag = new Fragment();

            foreach (char c in this.ParseData)
            {
                if (IsWhitespaceChar(c))
                {
                    // Finish building any varaibles
                    EndVariableBuild(ref frag);
                    continue;
                }
                
                if ((refToken = ValidateToken(c)).token != '\0')
                {
                    // Finish building any varaibles
                    EndVariableBuild(ref frag);

                    // Push the token
                    Tokens.Push(c);

                    // Update the context
                    PushContext(ref refToken, c);

                    // Pop tokens if necessary
                    PopStackTokens(ref refToken, ref frag);
                }
                else
                {
                    if (VarBuilder.Length == 0)
                    {
                        if (c >= 48 && c <= 57 && Tokens.Peek() != '\v')
                            Tokens.Push('\t');
                        else
                            Tokens.Push('\v');
                    }

                    VarBuilder.Append(c);
                }
            }
        }
        private bool IsWhitespaceChar(char c)
        {
            if (c <= 32 || c == 127)
                return true;

            return false;
        }

        private PLToken ValidateToken(char c)
        {
            foreach (var tkn in SyntaxTokens)
                if (c == tkn.token && !IsWhitespaceChar(c)) 
                    return tkn;

            return new PLToken { token='\0' };
        }

        private char GetCurrentContext()
        {
            return Contexts.Peek();
        }

        public void UtilizeColorizer(SyntaxColorizer colorizer)
        {
            colorizer.ClearParts();

            // Generate Line Parts
            foreach (var frag in Fragments)
            {
                SyntaxColorizer.LinePart lp = new SyntaxColorizer.LinePart();

                lp.offset = frag.start;
                lp.size = frag.size;

                // Loop through color params
                while (frag.parameters.Count > 0)
                {
                    var param = frag.parameters.Pop();

                    if (param == "color")
                    {
                        lp.area = SyntaxColorizer.LinePart.Area.back;
                        param = frag.parameters.Pop();
                        frag.color = param.Substring(1);
                        lp.color = new ImmutableSolidColorBrush(Fragment.HexToColor(frag.color));
                        colorizer.AppendPart(lp);
                    }
                    else if (param == "text")
                    {
                        lp.area = SyntaxColorizer.LinePart.Area.front;
                        param = frag.parameters.Pop();
                        frag.color = param.Substring(1);
                        lp.color = new ImmutableSolidColorBrush(Fragment.HexToColor(frag.color));
                        colorizer.AppendPart(lp);
                    }
                }
            }
        }
    }
}
