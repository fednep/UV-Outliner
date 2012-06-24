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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Diagnostics;
using System.IO;
using UVOutliner.Editor;
using System.Text.RegularExpressions;

namespace UVOutliner
{

    public class MyEdit : System.Windows.Controls.RichTextBox
    {

        private static Regex HyperlinksRegEx = new Regex(@"((www\.|(http|https|ftp|file)+\:\/\/)[_.a-z0-9-]+\.[a-z0-9\/_:@=.+?,##%&~-]*[^.|\'|\# |!|\(|?|,| |>|<|;|\)])", RegexOptions.IgnoreCase);
        #region RoutedEvents
        public static readonly RoutedEvent MoveToPrevLine = EventManager.RegisterRoutedEvent("MoveToPrevLine",
            RoutingStrategy.Bubble, typeof(MoveToLiveEventArgs), typeof(MyEdit));

        public static readonly RoutedEvent MoveToNextLine = EventManager.RegisterRoutedEvent("MoveToNextLine",
                    RoutingStrategy.Bubble, typeof(MoveToLiveEventArgs), typeof(MyEdit));

        public static readonly RoutedEvent MoveToNextColumn = EventManager.RegisterRoutedEvent("MoveToNextColumn",
                    RoutingStrategy.Bubble, typeof(MoveToLiveEventArgs), typeof(MyEdit));

        public static readonly RoutedEvent MoveToPrevColumn = EventManager.RegisterRoutedEvent("MoveToPrevColumn",
                    RoutingStrategy.Bubble, typeof(MoveToLiveEventArgs), typeof(MyEdit));

        public static readonly RoutedEvent EditorGotFocusEvent = EventManager.RegisterRoutedEvent("EditorGotFocus",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MyEdit));

        public static readonly RoutedEvent EditorLostFocusEvent = EventManager.RegisterRoutedEvent("EditorLostFocus",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MyEdit));

        public static readonly RoutedEvent PreviewEditorGotFocusEvent = EventManager.RegisterRoutedEvent("PreviewEditorGotFocus",
                    RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MyEdit));

        public static readonly RoutedEvent PreviewEditorLostFocusEvent = EventManager.RegisterRoutedEvent("PreviewEditorLostFocus",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MyEdit));

        public static readonly RoutedEvent UndoActionEvent = EventManager.RegisterRoutedEvent("UndoAction",
            RoutingStrategy.Bubble, typeof(RoutedEventArgs), typeof(MyEdit));

        #endregion

        public event EventHandler EditorCommandIssued;

        public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register("Document", typeof(FlowDocument), typeof(MyEdit), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnDocumentChanged)));
        public static readonly DependencyProperty InInlineNoteProperty = DependencyProperty.Register("InInlineNote", typeof(bool), typeof(MyEdit), new FrameworkPropertyMetadata(false));

        private bool __AlterWatch = false;

        private CommandBinding cbup;
        private CommandBinding cbdown;
        private CommandBinding cbleft;
        private CommandBinding cbright;

        public MyEdit()
            : base()
        {
            AcceptsReturn = false;
            cbup = new CommandBinding(EditingCommands.MoveUpByLine, new ExecutedRoutedEventHandler(MoveUpByLineExecuted));
            CommandBindings.Add(cbup);
            cbdown = new CommandBinding(EditingCommands.MoveDownByLine, new ExecutedRoutedEventHandler(MoveDownByLineExecuted));
            CommandBindings.Add(cbdown);
            cbleft = new CommandBinding(EditingCommands.MoveLeftByCharacter, new ExecutedRoutedEventHandler(MoveLeftByCharacter));
            CommandBindings.Add(cbleft);
            cbright = new CommandBinding(EditingCommands.MoveRightByCharacter, new ExecutedRoutedEventHandler(MoveRightByCharacter));
            CommandBindings.Add(cbright);

            IsUndoEnabled = false;
            UndoLimit = 0;

            AllowDrop = false;

            PreviewTextInput += new TextCompositionEventHandler(TextInput_Preview);
            PreviewKeyDown += new KeyEventHandler(KeyDown_Preview);            

            AddHandler(CommandManager.PreviewExecutedEvent, new ExecutedRoutedEventHandler(ExecutedHandler), true);            
        }

        public bool InInlineNote
        {
            get
            {
                return (bool)GetValue(InInlineNoteProperty);
            }

            set
            {
                SetValue(InInlineNoteProperty, value);
            }
        }

        void KeyDown_Preview(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                TextEntered(" ");
                e.Handled = true;
            }
        }

        void TextInput_Preview(object sender, TextCompositionEventArgs e)
        {
            TextEntered(e.Text);
            e.Handled = true;
        }

        static void OnDocumentChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            RichTextBox rtb = (RichTextBox)obj;            
            
            // tbd: on argument exception, just clone FlowDocument, and leave it
            if (args.NewValue == null)
            {
                rtb.Document = new FlowDocument();
            }
            else
                rtb.Document = (FlowDocument)args.NewValue;

            if (rtb is MyEdit)
            {
                ((MyEdit)rtb).ResumeAlterWatch();
                ((MyEdit)rtb).Links_Update();
            }
            
            // bug: was commented out to fix bug with 
            //rtb.Document.FontSize = Settings.DefaultFontSize;
            //rtb.Document.FontFamily = rtb.Document.FontFamily;
        }

        public void Links_Update()
        {
            if (Document == null)
                return;

            List<Hyperlink> linkList = new List<Hyperlink>();
            foreach (var block in Document.Blocks)
                LinksFind(block, linkList);

            foreach (var hl in linkList)
                SetupLink(hl);
        }

        private void SetupLink(Hyperlink hl)
        {
            string linkUrl = GetLinkURL(hl);

            // To be sure that event is not set multiple times.
            hl.RequestNavigate -= new System.Windows.Navigation.RequestNavigateEventHandler(Link_Navigate);
            hl.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(Link_Navigate);

            try
            {
                hl.NavigateUri = new Uri(linkUrl);
            }
            catch { }

            hl.FocusVisualStyle = null;
            hl.Focusable = false;
        }

        private string GetLinkURL(Hyperlink hl)
        {
            string res = "";
            foreach (var inline in hl.Inlines)
            {
                if (inline is Run)                
                    res += ((Run)inline).Text;
                else if (inline is Span)
                {
                    Span s = (Span)inline;
                    foreach (var spanInline in s.Inlines)
                    {
                        if (spanInline is Run)
                            res += ((Run)spanInline).Text;
                    }
                }
            }

            if (!res.Contains("://"))
                res = "http://" + res;

            return res;
        }
        
        private void ResumeAlterWatch()
        {
            __AlterWatch = true;
        }
        
        void ToggleBold(object sender, ExecutedRoutedEventArgs e)
        {
            IsSelectionBold = !IsSelectionBold;
            e.Handled = true;
        }

        void ToggleItalic(object sender, ExecutedRoutedEventArgs e)
        {
            IsSelectionItalic = !IsSelectionItalic;
            e.Handled = true;
        }

        void ToggleUnderline(object sender, ExecutedRoutedEventArgs e)
        {
            IsSelectionUnderlined = !IsSelectionUnderlined;
            e.Handled = true;
        }
        

        public bool IsSelectionBold
        {
            get
            {
                return (TextRangeHelpers.IsBold(Selection) == true);
            }

            set
            {
                TextRangeHelpers.SetBold(Selection, value);
                DoEditorCommandIssued();
            }
        }

        public bool IsSelectionItalic
        {
            get
            {
                return (TextRangeHelpers.IsItalic(Selection) == true);
            }

            set
            {
                TextRangeHelpers.SetItalic(Selection, value);
                DoEditorCommandIssued();
            }
        }

        public bool IsSelectionUnderlined
        {
            get
            {
                return (TextRangeHelpers.GetTextDecorationOnSelection(Selection, TextDecorationLocation.Underline) == true);
            }
            set
            {
                TextRangeHelpers.SetTextDecorationOnSelection(Selection, TextDecorationLocation.Underline, TextDecorations.Underline, value);
                DoEditorCommandIssued();
            }
        }

        public bool IsSelectionStrikethrough
        {
            get
            {
                return (TextRangeHelpers.GetTextDecorationOnSelection(Selection, TextDecorationLocation.Strikethrough) == true);
            }
            set
            {
                TextRangeHelpers.SetTextDecorationOnSelection(Selection, TextDecorationLocation.Strikethrough, TextDecorations.Strikethrough, value);
                DoEditorCommandIssued();
            }
        }

        private void DoEditorCommandIssued()
        {
            EventHandler handler = EditorCommandIssued;
            if (handler != null)
                handler(this, new EventArgs());
        }

        void PasteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText();
                TextPointer p = this.CaretPosition;
                p.InsertTextInRun(text);
                p = this.CaretPosition;
                CaretPosition = p.GetPositionAtOffset(p.GetTextRunLength(LogicalDirection.Forward));

                LinksCheck(p);
                return;
            }
        }        

        protected override void OnTextChanged(TextChangedEventArgs e)
        {            
            base.OnTextChanged(e);

            if (__AlterWatch == false)
                return;
        }        

        void MoveLeftByCharacter(object target, ExecutedRoutedEventArgs e)
        {
            TextPointer tp = CaretPosition;
            CommandBindings.Remove(cbleft);
            EditingCommands.MoveLeftByCharacter.Execute(null, this);
            CommandBindings.Add(cbleft);
            if (tp.CompareTo(CaretPosition) == 0)
                RaiseEvent(new RoutedEventArgs(MoveToPrevColumn));
        }

        void MoveRightByCharacter(object target, ExecutedRoutedEventArgs e)
        {
            TextPointer tp = CaretPosition;
            CommandBindings.Remove(cbright);
            EditingCommands.MoveRightByCharacter.Execute(null, this);
            CommandBindings.Add(cbright);
            if (tp.CompareTo(CaretPosition) == 0)
                RaiseEvent(new RoutedEventArgs(MoveToNextColumn));
        }

        void MoveDownByLineExecuted(object target, ExecutedRoutedEventArgs e)
        {
            TextPointer tp = CaretPosition;
            Rect r = CaretPosition.GetCharacterRect(LogicalDirection.Forward);
            CommandBindings.Remove(cbdown);
            EditingCommands.MoveDownByLine.Execute(null, this);
            CommandBindings.Add(cbdown);
            if (tp.CompareTo(CaretPosition) == 0)
            {
                MoveToLiveEventArgs l = new MoveToLiveEventArgs(MyEdit.MoveToNextLine, r);
                RaiseEvent(l);
            }
        }

        void MoveUpByLineExecuted(object target, ExecutedRoutedEventArgs e)
        {
            TextPointer tp = CaretPosition;
            Rect r = CaretPosition.GetCharacterRect(LogicalDirection.Forward);
            CommandBindings.Remove(cbup);
            EditingCommands.MoveUpByLine.Execute(null, this);
            CommandBindings.Add(cbup);
            if (tp.CompareTo(CaretPosition) == 0)
            {
                MoveToLiveEventArgs l = new MoveToLiveEventArgs(MyEdit.MoveToPrevLine, r);
                RaiseEvent(l);
            }
        }

        bool __ImeJustPrecessed = false;
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            Debug.WriteLine("Key down: {0}", e.Key);
            __ImeJustPrecessed = (e.Key == Key.ImeProcessed);

            if (e.Key == Key.Up && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                OutlinerCommands.MoveRowUp.Execute(null, this);
                e.Handled = true;
            }
             else
            if (e.Key == Key.Down && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                OutlinerCommands.MoveRowDown.Execute(null, this);
                e.Handled = true;
            }
             else
            base.OnPreviewKeyDown(e);
        }
        
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                OutlinerCommands.UnfocusEditor.Execute(null, this);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Return && InInlineNote)
            {
                InsertLineBreak();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Return && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                InsertLineBreak();
                e.Handled = true;
            }
        }

        private void InsertLineBreak()
        {
            TextPointer p = this.CaretPosition;
            CaretPosition = p.InsertLineBreak();
            LinksCheck(p);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            RoutedEventArgs args = new RoutedEventArgs(MyEdit.PreviewEditorGotFocusEvent);
            RaiseEvent(args);

            args = new RoutedEventArgs(MyEdit.EditorGotFocusEvent);
            RaiseEvent(args);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            RoutedEventArgs args = new RoutedEventArgs(MyEdit.PreviewEditorLostFocusEvent);
            RaiseEvent(args);

            args = new RoutedEventArgs(MyEdit.EditorLostFocusEvent);
            RaiseEvent(args);
        }


        internal void MoveCaretToLastLine(Rect rect)
        {
            TextPointer tp = Document.ContentEnd;
            TextPointer last_tp = tp;

            Rect r = tp.GetCharacterRect(LogicalDirection.Backward);
            while (tp != null && r.X > rect.X && !tp.IsAtLineStartPosition)
            {                
                tp = tp.GetNextInsertionPosition(LogicalDirection.Backward);
                if (tp == null)
                    break;
                r = tp.GetCharacterRect(LogicalDirection.Forward);
                last_tp = tp;
            }

            if (last_tp != null)
                CaretPosition = last_tp;

            // Workaround for bug in .NET 3.0
            if (!CaretPosition.IsAtLineStartPosition)
            {
                EditingCommands.MoveLeftByCharacter.Execute(null, this);
                EditingCommands.MoveRightByCharacter.Execute(null, this);
            }
            else if (CaretPosition.CompareTo(Document.ContentEnd) != 0)
            {
                EditingCommands.MoveRightByCharacter.Execute(null, this);
                EditingCommands.MoveLeftByCharacter.Execute(null, this);
            }
        }

        internal void MoveCaretToFirstLine(Rect rect)
        {
            TextPointer tp = Document.ContentStart;
            TextPointer last_tp = tp;

            Rect r = tp.GetCharacterRect(LogicalDirection.Forward);
            while (tp != null && r.X < rect.X && tp.CompareTo(Document.ContentEnd) != 0)
            {
                tp = tp.GetNextInsertionPosition(LogicalDirection.Forward);
                if (tp == null)
                    break;
                r = tp.GetCharacterRect(LogicalDirection.Forward);
                last_tp = tp;
            }

            if (last_tp != null)
                CaretPosition = last_tp;

            // Workaround for bug in .NET 3.0
            if (!CaretPosition.IsAtLineStartPosition)
            {
                EditingCommands.MoveLeftByCharacter.Execute(null, this);
                EditingCommands.MoveRightByCharacter.Execute(null, this);
            }
            else if (CaretPosition.CompareTo(Document.ContentEnd) != 0)
            {
                EditingCommands.MoveRightByCharacter.Execute(null, this);
                EditingCommands.MoveLeftByCharacter.Execute(null, this);
            }
        }

        public new FlowDocument Document
        {
            get { return (FlowDocument)GetValue(DocumentProperty); }
            set
            {
                SetValue(DocumentProperty, value);
            }
        }
        
        private void TextEntered(string text)
        {            
            Debug.WriteLine(String.Format("Text entered: '{0}'", text));

            // Удалить то количество символов, которое указано в text из ввода.
            if (__ImeJustPrecessed)
            {                
                TextPointer remove_start = Selection.Start;
                TextPointer remove_end = remove_start.GetPositionAtOffset(-text.Length);
                TextRange remove_range = new TextRange(remove_start, remove_end);
                remove_range.Text = "";
            }

            if (!Selection.IsEmpty)
            {
                PushUndoAction(new FormatUndo(Document, Selection, this), true);
                
                TextPointer insertPoint = Selection.Start;

                Selection.Start.InsertTextInRun(text);
                TextPointer newPointer = insertPoint.GetPositionAtOffset(text.Length);
                Selection.Select(newPointer, Selection.End);
                Selection.Text = "";
                CaretPosition = newPointer;
                Selection.Select(CaretPosition, CaretPosition);
                return;
            }
   
            TextPointer beforeInsert = CaretPosition.GetPositionAtOffset(0, LogicalDirection.Backward);            
            TextPointer insert = CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
            insert.InsertTextInRun(text);            
            CaretPosition = insert.GetPositionAtOffset(0, LogicalDirection.Backward);

            TextRange range = new TextRange(beforeInsert, CaretPosition);
            range.ApplyPropertyValue(RichTextBox.FontWeightProperty, Selection.GetPropertyValue(RichTextBox.FontWeightProperty));
            range.ApplyPropertyValue(RichTextBox.FontFamilyProperty, Selection.GetPropertyValue(RichTextBox.FontFamilyProperty));
            range.ApplyPropertyValue(RichTextBox.FontSizeProperty, Selection.GetPropertyValue(RichTextBox.FontSizeProperty));
            range.ApplyPropertyValue(RichTextBox.FontStyleProperty, Selection.GetPropertyValue(RichTextBox.FontStyleProperty));
            range.ApplyPropertyValue(TextBlock.TextDecorationsProperty, Selection.GetPropertyValue(TextBlock.TextDecorationsProperty));
            PushUndoAction(new UndoTextEnter(this, range, text), true);

            TextPointer endPointer = CaretPosition.GetPositionAtOffset(0, LogicalDirection.Backward);
            Selection.Select(CaretPosition, endPointer);

            LinksCheck(CaretPosition);
        }

        /*
         *   Алгоритм расстановки линков
         *   взять весь текст параграфа
         *   найти координаты начала всех линков
         *   проверить, все ли они на своих местах (и совпадает ли их количество)
         *   если нет, пересоздать все линки (удалить старые, создать новые)
         */
        public void LinksCheck(TextPointer textPointer)
        {
            Paragraph paragraph = textPointer.Paragraph;
            LinksUpdateInParagraph(paragraph);
        }
            
        private void LinksUpdateInParagraph(Paragraph paragraph)
        {
            if (paragraph == null)
                return;

            TextPointer paragraphStart = paragraph.ContentStart;

            TextRange range = new TextRange(paragraphStart, paragraph.ContentEnd);
            List<Hyperlink> links = new List<Hyperlink>();
            LinksFind(paragraph.Inlines, links);
            
            bool allMatchesFound = 
                LinksAreAllInPlace(paragraph, range, links);

            if (!allMatchesFound)
            {
                // Store undo
                var undoFormat = new FormatUndo(Document, range, this);
                undoFormat.UndoNext = true;
                PushUndoAction(undoFormat, false);

                // Удалить старые ссылки
                foreach (Hyperlink hl in links)                
                    LinkRemove(paragraph, hl);
                
                // Расставить ссылки заново
                LinksHighlight(paragraph, range);

                // Обновить адреса и прочее сссылок
                Links_Update();
            }
        }

        private void LinksHighlight(Paragraph paragraph, TextRange range)
        {
            var match = HyperlinksRegEx.Match(range.Text);
            while (match.Success)
            {
                TextPointer start = paragraph.ContentStart.GetPositionAtOffset(AdjustOffset(paragraph, match.Index));
                TextPointer end = paragraph.ContentStart.GetPositionAtOffset(AdjustOffset(paragraph, match.Index + match.Length, false));

                var hyperlink = new Hyperlink(start, end);                
                SetupLink(hyperlink);

                match = match.NextMatch();
            }
        }

        private bool LinksAreAllInPlace(Paragraph paragraph, TextRange range, List<Hyperlink> links)
        {
            var paragraphStart = paragraph.ContentStart;
            bool allMatchesFound = true;
            var match = HyperlinksRegEx.Match(range.Text);
            int matchCount = 0;

            while (match.Success)
            {
                matchCount += 1;

                int start = AdjustOffset(paragraph, match.Index);
                int end = AdjustOffset(paragraph, match.Index + match.Length, false);

                bool matchFound = false;
                foreach (var link in links)
                {
                    if (paragraphStart.GetOffsetToPosition(link.ContentStart) == start &&
                        paragraphStart.GetOffsetToPosition(link.ContentEnd) == end)
                    {
                        matchFound = true;
                        break;
                    }
                }

                if (matchFound == false)
                {
                    allMatchesFound = false;
                    break;
                }

                match = match.NextMatch();
            }

            if (matchCount != links.Count)
                allMatchesFound = false;

            return allMatchesFound;
        }

        private void LinkRemove(Paragraph paragraph, Hyperlink hl)
        {
            Inline nextInline = hl.NextInline;
            Inline prevInline = hl.PreviousInline;            

            int caretOffset = 0;
            int caretPosition = paragraph.ContentStart.GetOffsetToPosition(CaretPosition);

            if (CaretPosition.CompareTo(hl.ContentStart) > 0)
                caretOffset++;
            if (CaretPosition.CompareTo(hl.ContentEnd) > 0)
                caretOffset++;
            
            paragraph.Inlines.Remove(hl);

            foreach (var hlInline in hl.Inlines)
            {
                Inline newInline = null;

                if (hlInline is Run)
                {
                    newInline = new Run(((Run)hlInline).Text);
                }
                else
                    if (hlInline is LineBreak)
                    {
                        newInline = new LineBreak();
                    }

                if (newInline == null)
                    continue;

                if (prevInline == null && nextInline == null)
                {
                    paragraph.Inlines.Add(newInline);
                } 
                else
                    if (prevInline != null)
                    {
                        paragraph.Inlines.InsertAfter(prevInline, newInline);
                    }
                    else
                        if (nextInline != null)
                        {
                            paragraph.Inlines.InsertBefore(nextInline, newInline);
                        }

                prevInline = newInline;
            }

            if (caretPosition - caretOffset >= 0)
            {
                var newPosition = paragraph.ContentStart.GetPositionAtOffset(caretPosition - caretOffset);
                if (newPosition != null)
                    CaretPosition = newPosition;
                else
                {
                    CaretPosition = paragraph.ContentEnd;
                }
            }
        }

        private static void LinksFind(Block block, List<Hyperlink> resultingList)
        {
            if (block is Paragraph)
            {
                LinksFind(((Paragraph)block).Inlines, resultingList);
            } else
                if (block is Section)
                {
                    Section section = (Section)block;
                    foreach (var sectionBlock in section.Blocks)
                        LinksFind(sectionBlock, resultingList);
                }
        }

        private static void LinksFind(InlineCollection inlines, List<Hyperlink> links)
        {
            foreach (var inline in inlines)
            {
                if (inline is Run || inline is LineBreak)
                    continue;

                if (inline is Hyperlink)
                    links.Add(inline as Hyperlink);
                else {
                    InlineCollection subInlines = null;
                    if (inline is Span)
                        subInlines = ((Span)inline).Inlines;

                    LinksFind(subInlines, links);                    
                }
            }            
        }

        private int GetTextLenInInline(Inline inline)
        {
            if (inline is Run)
                return ((Run)inline).Text.Length;

            InlineCollection inlines = null;

             if (inline is LineBreak)
                return 2;
            else if (inline is Hyperlink)            
                inlines = ((Hyperlink)inline).Inlines;
            else if(inline is Span)
                inlines = ((Span)inline).Inlines;
                            
            if (inlines != null)
                return GetTextLenInInlineCollection(inlines);            
                
            TextRange range = new TextRange(inline.ContentStart, inline.ContentEnd);
            return range.Text.Length;
        }

        private int GetTextLenInInlineCollection(InlineCollection hlInlines)
        {
            int hlLen = 0;
            foreach (var hlInline in hlInlines)
            {
                hlLen += GetTextLenInInline(hlInline);
            }

            return hlLen;
        }

        private int AdjustOffset(Paragraph paragraph, int p, bool fromStart = true)
        {
            int value = p;
            while (p >= 0)
            {
                foreach (var inline in paragraph.Inlines)
                {
                    int rangeLen = GetTextLenInInline(inline);                    

                    if (p == rangeLen && fromStart == false)
                        return paragraph.ContentStart.GetOffsetToPosition(inline.ContentEnd);

                    if (p < rangeLen)
                    {                        
                        return paragraph.ContentStart.GetOffsetToPosition(inline.ContentStart) + p;                     
                    }
                    else
                    {
                        p -= rangeLen;
                    }
                }
            }

            return value;
        }        

        void Link_Navigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Hyperlink link = sender as Hyperlink;
            if (link == null)
                return;

            var url = GetLinkURL(link);

            UrlHelpers.OpenInBrowser(url);
            e.Handled = true;
        }        

        private void ExecutedHandler(object sender, ExecutedRoutedEventArgs e)
        {
            // При любой команде мержинг отменяется, мало ли что там команда навыполняла
            PushUndoAction(null, false);

            RichTextBox edit = (RichTextBox)sender;

            if (e.Command == EditingCommands.DeletePreviousWord ||
                e.Command == EditingCommands.DeleteNextWord)
            {
                if (!edit.Selection.IsEmpty)
                {
                    RemoveSelection(edit);                    
                }
                else
                {
                    int offsetFromEnd = edit.Document.ContentEnd.GetOffsetToPosition(edit.CaretPosition);
                    int offsetFromStart = edit.Document.ContentStart.GetOffsetToPosition(edit.CaretPosition);
                    TextPointer pointer = edit.CaretPosition;

                    if (e.Command == EditingCommands.DeletePreviousWord)
                    {
                        int resOffset = FindNextWhitespaceBackward(edit);

                        if (resOffset != -1)
                        {
                            TextRange range = new TextRange(
                                edit.Document.ContentStart.GetPositionAtOffset(resOffset),
                                edit.Document.ContentEnd.GetPositionAtOffset(offsetFromEnd));

                            PushUndoAction(new UndoBlockRemove(edit, range), true);
                            range.Text = "";
                        }
                    }
                    else if (e.Command == EditingCommands.DeleteNextWord)
                    {
                        int resOffset = FindNextWhitespaceForward(edit);

                        if (resOffset != -1)
                        {
                            TextRange range = new TextRange(
                                edit.Document.ContentStart.GetPositionAtOffset(resOffset),
                                edit.Document.ContentStart.GetPositionAtOffset(offsetFromStart));

                            PushUndoAction(new UndoBlockRemove(edit, range), true);
                            range.Text = "";
                        }
                    }
                }

                e.Handled = true;
                LinksCheck(CaretPosition);
            }

            if (e.Command == ApplicationCommands.Cut)
            {
                if (!edit.Selection.IsEmpty)
                {                    
                    edit.Copy();
                    RemoveSelection(this);
                    LinksCheck(CaretPosition);
                }

                e.Handled = true;
                return;
            }

            if (e.Command == ApplicationCommands.Paste)
            {
                if (!Clipboard.ContainsData(DataFormats.Rtf) && !Clipboard.ContainsData(DataFormats.Text))
                {
                    e.Handled = true;
                    return;
                }

                var undoGroup = new UndoGroup();

                // Удалить старое содержимое
                if (!edit.Selection.IsEmpty)
                {
                    FormatUndo undoFormat = new FormatUndo(edit.Document, edit.Selection, edit);
                    undoGroup.Add(undoFormat);
                    edit.Selection.Text = "";
                    undoFormat.UpdateSelectionOffsets(edit, edit.Selection);                                       
                }

                int offsetStart = edit.Document.ContentStart.GetOffsetToPosition(edit.Selection.Start);
                int offsetEnd = edit.Document.ContentEnd.GetOffsetToPosition(edit.Selection.End);
                var undoPaste = new UndoPaste(edit, offsetStart, offsetEnd);

                bool wasError = false;
                if (Clipboard.ContainsData(DataFormats.Rtf))
                {
                    try
                    {
                        var rtfStream = MemoryStreamFromClipboard();
                        edit.Selection.Load(rtfStream, DataFormats.Rtf);
                    }
                    catch
                    {
                        wasError = true;
                    }
                }
                else if (Clipboard.ContainsData(DataFormats.Text))
                {
                    edit.Selection.Text = (string)Clipboard.GetData(DataFormats.Text);
                }

                // Если была ошибка добавления текста, то                 
                if (wasError == false)
                {
                    undoGroup.Add(undoPaste);
                    PushUndoAction(undoGroup, true);
                    edit.CaretPosition = edit.Selection.End;
                }

                // Проверить, все ли линки на месте
                LinksCheck(CaretPosition);
                
                // Обновить хендлеры у новых ссылок (которые могли прийти с клипбоардом)
                Links_Update();

                e.Handled = true;
                return;
            }

            if (e.Command == EditingCommands.AlignCenter ||
                e.Command == EditingCommands.AlignJustify ||
                e.Command == EditingCommands.AlignLeft ||
                e.Command == EditingCommands.AlignRight ||
                e.Command == EditingCommands.IncreaseIndentation ||
                e.Command == EditingCommands.TabBackward ||
                e.Command == EditingCommands.TabForward ||
                e.Command == EditingCommands.ToggleNumbering ||
                e.Command == EditingCommands.ToggleSubscript ||
                e.Command == EditingCommands.ToggleSuperscript)
            {
                e.Handled = true;
                return;
            }
            //e.Command == EditingCommands.ToggleUnderline

            if (e.Command == EditingCommands.IncreaseFontSize ||
                e.Command == EditingCommands.DecreaseFontSize ||
                e.Command == EditingCommands.ToggleBold ||
                e.Command == EditingCommands.ToggleItalic ||
                e.Command == EditingCommands.ToggleUnderline ||
                e.Command == OutlinerCommands.ToggleCrossed)
            {
                if (!edit.Selection.IsEmpty)
                {
                    var bu = new FormatUndo(edit.Document, edit.Selection, edit);
                    PushUndoAction(bu, true);

                    if (e.Command == EditingCommands.ToggleBold)
                        IsSelectionBold = !IsSelectionBold;
                    else if (e.Command == EditingCommands.ToggleItalic)
                        IsSelectionItalic = !IsSelectionItalic;
                    else if (e.Command == EditingCommands.ToggleUnderline)
                        IsSelectionUnderlined = !IsSelectionUnderlined;
                    else if (e.Command == OutlinerCommands.ToggleCrossed)
                        IsSelectionStrikethrough = !IsSelectionStrikethrough;
                    /*if (e.Command == OutlinerCommands.ToggleCrossed)
                        IsSelectionStrikethrough = !IsSelectionStrikethrough;*/

                    bu.UpdateSelectionOffsets(edit, edit.Selection);
                    e.Handled = true;
                }
                else
                {

                    TextRange paragraphRange;
                    if (edit.CaretPosition.Paragraph != null)
                        paragraphRange = new TextRange(edit.CaretPosition.Paragraph.ContentStart,
                                                             edit.CaretPosition.Paragraph.ContentEnd);
                    else
                        paragraphRange = new TextRange(edit.Document.ContentStart,
                                                             edit.Document.ContentEnd);

                    FontProperties fontProps = new FontProperties(paragraphRange);
                    var bu = new FormatUndo(edit.Document,
                                    paragraphRange, 
                                    edit);


                    int caretPositionStart = edit.Document.ContentStart.GetOffsetToPosition(edit.CaretPosition);
                    int caretPositionEnd = edit.Document.ContentEnd.GetOffsetToPosition(edit.CaretPosition);

                    if (e.Command == EditingCommands.ToggleBold)
                        IsSelectionBold = !IsSelectionBold;
                    else if (e.Command == EditingCommands.ToggleItalic)
                        IsSelectionItalic = !IsSelectionItalic;
                    else if (e.Command == EditingCommands.ToggleUnderline)
                        IsSelectionUnderlined = !IsSelectionUnderlined;
                    else if (e.Command == OutlinerCommands.ToggleCrossed)
                        IsSelectionStrikethrough = !IsSelectionStrikethrough;

                    int newCaretPositionStart = edit.Document.ContentStart.GetOffsetToPosition(edit.CaretPosition);
                    int newCaretPositionEnd = edit.Document.ContentEnd.GetOffsetToPosition(edit.CaretPosition);

                    string paragraphRangeText = paragraphRange.Text;
                    if (caretPositionStart != newCaretPositionStart || caretPositionEnd != newCaretPositionEnd ||
                        !fontProps.HasSameStyle(paragraphRange))
                        PushUndoAction(bu, true);

                    e.Handled = true;
                }
                return;
            }

            if (e.Command == EditingCommands.Backspace ||
                e.Command == EditingCommands.Delete)
            {

                if (!edit.Selection.IsEmpty)
                {                    
                    RemoveSelection(edit);
                    LinksCheck(CaretPosition);
                    e.Handled = true;                    
                }
                else
                {
                    TextPointer right = edit.Selection.Start;
                    TextPointer left = right.GetNextInsertionPosition(LogicalDirection.Backward);

                    if (e.Command == EditingCommands.Delete)
                    {
                        left = edit.Selection.Start;
                        right = right.GetNextInsertionPosition(LogicalDirection.Forward);
                    }

                    if (right != null && left != null)
                    {

                        TextRange range = new TextRange(right, left);
                        if (range.Text != "")
                        {
                            var bu = new UndoBlockRemove(edit, range);
                            PushUndoAction(bu, true);

                            range.ClearAllProperties();
                            range.Text = "";

                            bu.UpdateOffsets(edit, range);
                        }
                    }

                    LinksCheck(CaretPosition);
                    e.Handled = true;
                }
            }            
        }

        public void ApplyUndoAwarePropertyValue(TextRange range, DependencyProperty property, object value)
        {
            if (!range.IsEmpty)
            {
                var bu = new FormatUndo(Document, range, this);
                PushUndoAction(bu, true);

                range.ApplyPropertyValue(property, value);

                bu.UpdateSelectionOffsets(this, range);                
            }
            else
            {
                TextRange paragraphRange;

                if (CaretPosition.Paragraph != null)
                    paragraphRange = new TextRange(CaretPosition.Paragraph.ContentStart,
                                                         CaretPosition.Paragraph.ContentEnd);
                else
                    paragraphRange = new TextRange(CaretPosition.DocumentStart,
                                                         CaretPosition.DocumentEnd);

                FontProperties fontProps = new FontProperties(paragraphRange);
                var bu = new FormatUndo(Document,
                                paragraphRange, 
                                this);

                int caretPositionStart = Document.ContentStart.GetOffsetToPosition(CaretPosition);
                int caretPositionEnd = Document.ContentEnd.GetOffsetToPosition(CaretPosition);

                range.ApplyPropertyValue(property, value);

                int newCaretPositionStart = Document.ContentStart.GetOffsetToPosition(CaretPosition);
                int newCaretPositionEnd = Document.ContentEnd.GetOffsetToPosition(CaretPosition);

                string paragraphRangeText = paragraphRange.Text;
                if (caretPositionStart != newCaretPositionStart || caretPositionEnd != newCaretPositionEnd ||
                    !fontProps.HasSameStyle(paragraphRange))
                    PushUndoAction(bu, true);

            }
            return;
         
        }
        
        private TextRange SelectCurrentWord(RichTextBox edit)
        {
            TextPointer pointer = edit.CaretPosition;

            TextPointer wordEnd = edit.CaretPosition;
            TextPointer wordStart = edit.CaretPosition;

            // Ищем вперед
            pointer = wordEnd;
            bool wSpaceFound = false;
            while (pointer != null)
            {
                string nextText = pointer.GetTextInRun(LogicalDirection.Forward);
                if (nextText != "")                    
                {
                    for (int i = 0; i < nextText.Length; i++)
                    {
                        if (WordParser.IsAlphanumeric(nextText[i]) || WordParser.IsWhitespace(nextText[i]))
                        {
                            wordEnd = pointer.GetPositionAtOffset(i, LogicalDirection.Forward);
                            wSpaceFound = true;
                            break;
                        }
                    }
                    
                    if (wSpaceFound)
                        break;
                    else
                        wordEnd = pointer.GetPositionAtOffset(nextText.Length);
                }
                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);                    
            }

            // ищем назад
            pointer = wordStart;
            wSpaceFound = false;
            while (pointer != null)
            {
                string prevText = pointer.GetTextInRun(LogicalDirection.Backward);
                if (prevText != "")
                {                
                    for (int i = prevText.Length - 1; i >= 0; i--)
                    {
                        if (WordParser.IsAlphanumeric(prevText[i]) || WordParser.IsWhitespace(prevText[i]))
                        {
                            wordStart = pointer.GetPositionAtOffset(-(prevText.Length-i) + 1);
                            wSpaceFound = true;
                            break;
                        }
                    }
                    if (wSpaceFound)
                        break;
                }

                TextPointer lastPointer = pointer;
                pointer = pointer.GetNextContextPosition(LogicalDirection.Backward);
                if (pointer == null)
                    wordStart = lastPointer;
            }

            return new TextRange(wordStart, wordEnd);
        }

        private static int FindNextWhitespaceBackward(RichTextBox edit)
        {
            TextPointer pointer = edit.CaretPosition;
            WordParser parser = new WordParser("", LogicalDirection.Backward);
            int resOffset = -1;

            while (pointer != null)
            {
                string textInRun = pointer.GetTextInRun(LogicalDirection.Backward);
                if (textInRun != "")
                {
                    parser.Text = textInRun;

                    int res = parser.FindNextWhitespace();
                    if (res != -1)
                    {
                        resOffset = edit.Document.ContentStart.GetOffsetToPosition(pointer) - res + 1;
                        break;
                    }
                }
                pointer = pointer.GetNextContextPosition(LogicalDirection.Backward);
            }

            if (pointer == null)
                resOffset = 0;
            return resOffset;
        }

        private static int FindNextWhitespaceForward(RichTextBox edit)
        {
            TextPointer pointer = edit.CaretPosition;
            WordParser parser = null;
            int resOffset = -1;

            while (pointer != null)
            {
                string textInRun = pointer.GetTextInRun(LogicalDirection.Forward);
                if (textInRun != "")
                {
                    if (parser == null)
                        parser = new WordParser(textInRun, LogicalDirection.Forward);
                    else
                        parser.Text = textInRun;

                    int res = parser.FindNextWhitespace();
                    if (res != -1)
                    {
                        resOffset = edit.Document.ContentStart.GetOffsetToPosition(pointer) + res;
                        break;
                    }
                }
                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
            }

            if (pointer == null)
                resOffset = edit.Document.ContentStart.GetOffsetToPosition(edit.Document.ContentEnd);

            return resOffset;
        }
        
        private void RemoveSelection(RichTextBox edit)
        {
            FormatUndo undo = new FormatUndo(edit.Document, edit.Selection, edit);

            edit.Selection.Text = "";
            undo.UpdateSelectionOffsets(edit, edit.Selection);
            PushUndoAction(undo, true);
        }

        private Stream MemoryStreamFromClipboard()
        {
            MemoryStream stream = new MemoryStream();
            string st = (string)Clipboard.GetData(DataFormats.Rtf);

            StreamWriter writer = new StreamWriter(stream);
            writer.Write(st);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        private void PushUndoAction(UVEditUndoAction action, bool clearRedoStack)
        {
            UndoActionRoutedEventArgs args = 
                new UndoActionRoutedEventArgs(this, UndoActionEvent, action);
            RaiseEvent(args);          
        }
    }
}
