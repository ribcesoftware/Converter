using System;
using System.Collections.Generic;
using System.Windows.Forms;
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
            BookParagraph output = null;
            foreach (Microsoft.Office.Interop.Word.Paragraph objParagraph in oDoc.Paragraphs)
            {
                if ((objParagraph.Range.Text.Trim() != "") && (objParagraph.Range.Text.Trim().Length != 1))
                {
                    if (output != null) yield return output;
                    index++;
                    output = new BookParagraph(BookParagraph.TYPE_ORDINARY_PAR, objParagraph.Range.Text, index, index + 1);
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
