﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Office;
using Microsoft.Office.Interop.Word;

namespace RBCCD
{
    class MSWord_Reader : Reader
    {
        private Microsoft.Office.Interop.Word.Application oWord = null;
        private Microsoft.Office.Interop.Word.Document oDoc = null;
        object missing = System.Reflection.Missing.Value;
        object filenameobj = null;

        public MSWord_Reader(string FileName)
        {
            this.FileName = FileName;
            filenameobj = this.FileName;
        }
        public override void Open()
        {
            ProgressPercentage = 0;
            try
            {
                oWord = new Microsoft.Office.Interop.Word.Application();
                oWord.Visible = false;
            }
            catch (Exception)
            {
                throw new InternalErrorException();
            }
            try
            {
                oDoc = oWord.Documents.Open(ref filenameobj, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing);
            }
            catch (Exception)
            {
                throw new UnsupportedFileFormatException();
            }
        }

        public override IEnumerator<BookParagraph> GetEnumerator()
        {
            int ProcessedParagraphs = 0;
            int index = 1;
            string ExtractedText;
            string ExtractedLine;
            string TestString = "";
            Encoding TextEncoding;
            BookParagraph output = null;
            Dictionary<string, object> ClipboardBackup;

            // Backup of the clipboard data
            ClipboardBackup = new Dictionary<string, object>();
            IDataObject ClipboardDataObject = Clipboard.GetDataObject();
            string[] Formats = ClipboardDataObject.GetFormats(false);
            foreach (var DataFormat in Formats)
                ClipboardBackup.Add(DataFormat, ClipboardDataObject.GetData(DataFormat, false));

            foreach (Microsoft.Office.Interop.Word.Paragraph objParagraph in oDoc.Paragraphs)
            {
                TestString += objParagraph.Range.Text.Trim();
                if (TestString.Length >= 1024) break;
            }

            TextEncoding = EncodingTools.GetMostEfficientEncoding(TestString);
            // MessageBox.Show(TextEncoding.CodePage.ToString() + " " + TextEncoding.BodyName + " " + TextEncoding.EncodingName);
            foreach (Microsoft.Office.Interop.Word.Paragraph objParagraph in oDoc.Paragraphs)
            {
                objParagraph.Range.TextRetrievalMode.IncludeHiddenText = true;
                objParagraph.Range.TextRetrievalMode.IncludeFieldCodes = true;
                ExtractedText = objParagraph.Range.Text.Trim();
                if (ExtractedText.Length > 1)
                {
                    ExtractedText = Encoding.UTF8.GetString(Encoding.Convert(TextEncoding, Encoding.UTF8, TextEncoding.GetBytes(ExtractedText)));
                    string[] TextLines = ExtractedText.Split("\n\v\r".ToCharArray());
                    foreach (string line in TextLines)
                        if ((ExtractedLine = line.Trim()).Length > 1)
                        {
                            if (output != null) yield return output;
                            index++;
                            output = new BookParagraph(BookParagraph.TYPE_ORDINARY_PAR, ExtractedLine, index, index + 1);
                        }
                }
                if (objParagraph.Range.ShapeRange.Count != 0)
                {
                    objParagraph.Range.ShapeRange.Select();
                    oWord.Selection.Copy();
                    if (Clipboard.ContainsImage())
                    {
                        if (output != null) yield return output;
                        index++;
                        output = new BookParagraph(BookParagraph.TYPE_PICTURE, Clipboard.GetImage(), index, index + 1);
                    }
                }
                if (objParagraph.Range.InlineShapes.Count != 0)
                {
                    foreach (InlineShape Shape in objParagraph.Range.InlineShapes)
                    {
                        Shape.Select();
                        oWord.Selection.Copy();
                        if (Clipboard.ContainsImage())
                        {
                            if (output != null) yield return output;
                            index++;
                            output = new BookParagraph(BookParagraph.TYPE_PICTURE, Clipboard.GetImage(), index, index + 1);
                        }
                    }
                }
                ProcessedParagraphs++;
                ProgressPercentage = 100 * ProcessedParagraphs / oDoc.Paragraphs.Count;
            }

            // Restoring the clipboard data from backup 
            foreach (KeyValuePair<string, object> ClipboardData in ClipboardBackup)
                Clipboard.SetData(ClipboardData.Key, ClipboardData.Value);

            ProgressPercentage = 100;
            output.NextParagraphID = -1;
            yield return output;
        }
        public override void Close()
        {
            try
            {
                if (oDoc != null) oDoc.Close(WdSaveOptions.wdDoNotSaveChanges, missing, missing);
                oDoc = null;
                object save = false;
                if (oWord != null) oWord.Quit(ref save, ref missing, ref missing);
                oWord = null;
            }
            catch (Exception)
            {
                oDoc = null;
                oWord = null;
                throw new InternalErrorException();
            }
        }
    }
}
