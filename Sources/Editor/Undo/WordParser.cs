/*
    Copyright (c) 2005-2012 Fedir Nepyivoda <fednep@gmail.com>
  
    This file is part of UV Outliner project.
    http://uvoutliner.com

    UV Outliner is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    UV Outliner is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with UV Outliner.  If not, see <http://www.gnu.org/licenses/>
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Windows.Documents;

namespace UVOutliner
{
    public enum WordParserState {WitespacesBeforeWord, ParsingSymbol, ParsingText, WhitespacesAfterWord}


    public class WordParser
    {
        LogicalDirection __DirectionToSearch;
        string __Text;
        WordParserState __State;

        private static string symbols = "~!@#$%^&*()_+-=\\|/.,[]";
        private static string whitespaces = " \t";

        public WordParser(string text, LogicalDirection direction)
        {
            __Text = text;
            __DirectionToSearch = direction;
            if (direction == LogicalDirection.Backward)
                __State = WordParserState.WitespacesBeforeWord;
            else
            {
                if (text.Length == 0)
                    __State = WordParserState.WitespacesBeforeWord;
                else
                {
                    if (symbols.Contains(__Text[0]))
                        __State = WordParserState.ParsingSymbol;
                    else if (whitespaces.Contains(__Text[0]))
                        __State = WordParserState.WitespacesBeforeWord;
                    else
                        __State = WordParserState.ParsingText;
                }
            }
        }

        public static bool IsAlphanumeric(char p)
        {
            if (symbols.Contains(p))
                return true;

            return false;
        }

        public static bool IsWhitespace(char p)
        {
            if (whitespaces.Contains(p))
                return true;

            return false;
        }

        public int FindNextWhitespace()
        {
            if (__DirectionToSearch == LogicalDirection.Backward)
                return FindBeckward();
            else
                return FindForward();
        }

        private int FindForward()
        {
            int pos = 0;

            while (pos < __Text.Length)
            {
                int res = Parse(pos);
                if (res != -1)
                    return res;
                pos++;
            }

            return -1;
        }

        private int FindBeckward()
        {
            int pos = __Text.Length - 1;

            while (pos >= 0)
            {
                int res = Parse(pos);
                if (res != -1)
                    return __Text.Length - res;
                pos--;
            }

            return -1;
        }

        private int Parse(int pos)
        {
            switch (__State)
            {

                case WordParserState.WitespacesBeforeWord:
                    if (__DirectionToSearch == LogicalDirection.Forward)
                    {
                        if (!whitespaces.Contains(__Text[pos]))
                            return pos;
                    }
                    else
                    {
                        if (symbols.Contains(__Text[pos]))
                            __State = WordParserState.ParsingSymbol;
                        else if (!whitespaces.Contains(__Text[pos]))
                            __State = WordParserState.ParsingText;
                    }
                    break;

                case WordParserState.ParsingSymbol:
                    if (!symbols.Contains(__Text[pos]))
                        return pos;
                    break;

                case WordParserState.ParsingText:
                    if (symbols.Contains(__Text[pos]))
                        return pos;

                    if (whitespaces.Contains(__Text[pos]))
                    {
                        if (__DirectionToSearch == LogicalDirection.Backward)
                            return pos;

                        __State = WordParserState.WhitespacesAfterWord;
                    }
                    break;

                case WordParserState.WhitespacesAfterWord:
                    if (!whitespaces.Contains(__Text[pos]))
                        return pos;
                    
                    break;
            }

            return -1;
        }

        public string Text
        {
            get { return __Text; }
            set { 
                __Text = value;                
            }
        }
    }
}
