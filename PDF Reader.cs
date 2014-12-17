using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Text;
using iTextSharp;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace RBCCD
{
    internal class RIBCEBookPDFExtractionStrategy : ITextExtractionStrategy
    {
        private int index;
        private Vector lastStart;
        private Vector lastEnd;
        private SortedDictionary<int, StringBuilder> results = new SortedDictionary<int, StringBuilder>();
        private string textRange;
        private List<BookParagraph> ExtractedParagraphs;

        public RIBCEBookPDFExtractionStrategy(int startIndex)
        {
            textRange = "";
            index = startIndex;
            results = new SortedDictionary<int, StringBuilder>();
            ExtractedParagraphs = new List<BookParagraph>();
        }

        public void BeginTextBlock() { }
        public void EndTextBlock() { }
        public String GetResultantText() // Stub
        {
            return "";
        }

        public int GetNextIndex()
        {
            return index;
        }

        public List<BookParagraph> GetParagraphList()
        {
            AddAllTextPars();
            return ExtractedParagraphs;
        }

        private void AddAllTextPars()
        {
            foreach (var parText in results)
                if (parText.Value.ToString().Trim() != "")
                    ExtractedParagraphs.Add(new BookParagraph(BookParagraph.TYPE_ORDINARY_PAR, parText.Value.ToString().Trim(), index, ++index));
            results.Clear();
        }

        public void RenderText(TextRenderInfo renderInfo)
        {
            bool firstRender = results.Count == 0;

            LineSegment segment = renderInfo.GetBaseline();
            Vector start = segment.GetStartPoint();
            Vector end = segment.GetEndPoint();

            int currentLineKey = (int)start[1];

            if (!firstRender)
            {
                Vector x0 = start;
                Vector x1 = lastStart;
                Vector x2 = lastEnd;

                float dist = (x2.Subtract(x1)).Cross((x1.Subtract(x0))).LengthSquared / x2.Subtract(x1).LengthSquared;

                float sameLineThreshold = 1f;
                if (dist <= sameLineThreshold)
                {
                    currentLineKey = (int)lastStart[1];
                }
            }
            
            currentLineKey = currentLineKey * -1;
            if (!results.ContainsKey(currentLineKey))
            {
                results.Add(currentLineKey, new StringBuilder());
            }

            if (!firstRender &&
                results[currentLineKey].Length != 0 &&
                !results[currentLineKey].ToString().EndsWith(" ") &&
                renderInfo.GetText().Length > 0 &&
                !renderInfo.GetText().StartsWith(" "))
            {
                float spacing = lastEnd.Subtract(start).Length;
                if (spacing > renderInfo.GetSingleSpaceWidth() / 2f)
                {
                    results[currentLineKey].Append(" ");
                }
            }

            results[currentLineKey].Append(renderInfo.GetText());
            lastStart = start;
            lastEnd = end;
        }

        public void RenderImage(ImageRenderInfo renderInfo)
        {
            Image imageRange = null;
            iTextSharp.text.pdf.parser.PdfImageObject image = renderInfo.GetImage();
            if (image == null) return;
            try
            {
                imageRange = Image.FromStream(new MemoryStream(image.GetImageAsBytes()));
            }
            catch (Exception) {}
            if (imageRange != null)
            {
                if (textRange.Trim() != "")
                    AddAllTextPars();
                ExtractedParagraphs.Add(new BookParagraph(BookParagraph.TYPE_PICTURE, imageRange, index, ++index));
            }
        }
    }
    class PDF_Reader : Reader
    {
        PdfReader PDFReaderObj = null;
        PdfReaderContentParser PDFParserObj = null;

        public PDF_Reader(string FileName)
        {
            this.FileName = FileName;
        }
        public override void Open()
        {
            ProgressPercentage = 0;
            try
            {
                PDFReaderObj = new iTextSharp.text.pdf.PdfReader(FileName);
                PDFParserObj = new PdfReaderContentParser(PDFReaderObj);
            }
            catch (Exception)
            {
                throw new UnsupportedFileFormatException();
            }
        }

        public override IEnumerator<BookParagraph> GetEnumerator()
        {
            int ProcessedParagraphs = 0;
            int index = 2;
            BookParagraph output = null;

            for (int i = 1; i <= PDFReaderObj.NumberOfPages; i++)
            {
                RIBCEBookPDFExtractionStrategy PDFExtractionStrategy = new RIBCEBookPDFExtractionStrategy(index);
                PDFParserObj.ProcessContent(i, PDFExtractionStrategy);

                foreach (BookParagraph par in PDFExtractionStrategy.GetParagraphList())
                {
                    if (output != null) yield return output;
                    output = par;
                }
                index = PDFExtractionStrategy.GetNextIndex();
                ProcessedParagraphs++;
                ProgressPercentage = 100 * ProcessedParagraphs / PDFReaderObj.NumberOfPages;
            }

            ProgressPercentage = 100;
            output.NextParagraphID = -1;
            yield return output;
        }
        public override void Close()
        {
            try
            {
                PDFReaderObj.Close();
            }
            catch (Exception)
            {
                throw new InternalErrorException();
            }
            finally
            {
                PDFParserObj = null;
                PDFReaderObj = null;
            }
        }
    }
}
