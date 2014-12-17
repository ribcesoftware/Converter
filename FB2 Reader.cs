using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using FB2Library;
using FB2Library.Elements;
using FB2Library.Elements.Poem;
using System.Xml;
using System.Xml.Linq;

namespace RBCCD
{
    class FB2_Reader : Reader
    {
        FB2File FB2FileObj = null;
        Stream fileStream = null;

        public FB2_Reader(string FileName)
        {
            this.FileName = FileName;
        }
        public override void Open()
        {
            ProgressPercentage = 0;
            try
            {
                FB2FileObj = new FB2File();
                fileStream = File.OpenRead(FileName);
                FB2FileObj.Load(XDocument.Load(XmlReader.Create(fileStream, new XmlReaderSettings { ValidationType = ValidationType.None, ProhibitDtd = false }), LoadOptions.PreserveWhitespace), false);
            }
            catch (Exception)
            {
                throw new UnsupportedFileFormatException();
            }
        }

        public override IEnumerator<BookParagraph> GetEnumerator()
        {
            int index = 2;
            BookParagraph output = null;

            foreach (BodyItem bodyItem in FB2FileObj.Bodies)
                foreach (SectionItem sectionItem in bodyItem.Sections)
                    foreach (var partSectionItem in sectionItem.Content)
                    {
                        if ((partSectionItem is ParagraphItem) && (partSectionItem.ToString().Trim() != ""))
                        {
                            if (output != null) yield return output;
                            output = new BookParagraph(BookParagraph.TYPE_ORDINARY_PAR, partSectionItem.ToString().Trim(), index, ++index);
                        }
                        else if (partSectionItem is ImageItem)
                        {
                            BinaryItem imageData = null;
                            Image imageObj = null;
                            try
                            {
                                imageData = FB2FileObj.Images[((ImageItem)partSectionItem).HRef.Substring(1)];
                                imageObj = Image.FromStream(new MemoryStream(imageData.BinaryData));
                            }
                            catch (Exception) { }
                            if (imageObj != null)
                            {
                                if (output != null) yield return output;
                                output = new BookParagraph(BookParagraph.TYPE_PICTURE, imageObj, index, ++index);
                            }
                        }
                        else if (partSectionItem is PoemItem)
                        {
                            foreach (var poemItem in ((PoemItem)partSectionItem).Content)
                                if (poemItem.ToString().Trim() != "")
                                {
                                    if (output != null) yield return output;
                                    output = new BookParagraph(BookParagraph.TYPE_ORDINARY_PAR, poemItem.ToString().Trim(), index, ++index);
                                }
                        }
                        else if (partSectionItem is EpigraphItem)
                        {
                            foreach (var epigraphItem in ((EpigraphItem)partSectionItem).EpigraphData.Concat(((EpigraphItem)partSectionItem).TextAuthors))
                                if (epigraphItem.ToString().Trim() != "")
                                {
                                    if (output != null) yield return output;
                                    output = new BookParagraph(BookParagraph.TYPE_ORDINARY_PAR, epigraphItem.ToString().Trim(), index, ++index);
                                }
                        }
                        else if (partSectionItem.ToString().Trim() != "")
                        {
                            if (output != null) yield return output;
                            output = new BookParagraph(BookParagraph.TYPE_ORDINARY_PAR, partSectionItem.ToString().Trim(), index, ++index);
                        }
                    }

            ProgressPercentage = 100;
            output.NextParagraphID = -1;
            yield return output;
        }
        public override void Close()
        {
            try
            {
                if (fileStream != null) fileStream.Close();
                fileStream = null;
                FB2FileObj = null;
            }
            catch (Exception)
            {
                throw new InternalErrorException();
            }
        }
    }
}
