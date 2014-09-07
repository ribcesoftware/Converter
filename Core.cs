using System;
using System.IO;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.GZip;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace RBCCD
{
    abstract class Reader : IEnumerable<BookParagraph>
    {
        protected string FileName;
        public class UnsupportedFileFormatException : Exception { }
        public class InternalErrorException : Exception { }
        public Reader() { }
        public Reader(string FileName) { }
        abstract public void Open();
        public abstract IEnumerator<BookParagraph> GetEnumerator();
        public int ProgressPercentage;
        IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
        abstract public void Close();
        ~Reader() { Close(); }
    }

    partial class Converter
    {
        string SourceFileName;
        string BookShortName;
        Config config;
        Logger logger;

        private enum SourceFileType
        {
            MSWORD_OLD,
            MSWORD_NEW,
            UNKNOWN
        }

        private SourceFileType DetectFileType()
        {
            byte[] buffer = new byte[512];
            try
            {
                FileStream fileStream = new FileStream(SourceFileName, FileMode.Open, FileAccess.Read);
                fileStream.Read(buffer, 0, buffer.Length);
                fileStream.Close();
            }
            catch (Exception)
            {
                return SourceFileType.UNKNOWN;
            }

            if (OldMSWordSignature.SequenceEqual(buffer.Take(OldMSWordSignature.Length)))
                return SourceFileType.MSWORD_OLD;
            else if (NewMSWordSignature.SequenceEqual(buffer.Take(NewMSWordSignature.Length)))
                return SourceFileType.MSWORD_NEW;
            else
                return SourceFileType.UNKNOWN;
        }

        public Converter(string SourceFileName, string BookShortName, Logger logger, Config config)
        {
            this.SourceFileName = SourceFileName;
            this.BookShortName = BookShortName;
            this.logger = logger;
            this.config = config;
        }

        public void Run()
        {
            Reader reader;
            SourceFileType filetype;

            logger.WriteToLog(Logger.Level.INFO, "Detetcting type of source file...");
            filetype = DetectFileType();
            switch (filetype)
            {
                case SourceFileType.MSWORD_OLD:
                    logger.WriteToLog(Logger.Level.INFO, "Old MS Word *.doc file!");
                    reader = new MSWord_Reader(SourceFileName);
                    break;
                case SourceFileType.MSWORD_NEW:
                    logger.WriteToLog(Logger.Level.INFO, "New MS Word *.docx file!");
                    reader = new MSWord_Reader(SourceFileName);
                    break;
                case SourceFileType.UNKNOWN:
                default:
                    throw new Reader.UnsupportedFileFormatException();
            }
            logger.WriteToLog(Logger.Level.INFO, "Converting book " + BookShortName);

            logger.WriteToLog(Logger.Level.INFO, "Creating " + BookShortName + ".ribce file.");
            FileStream fileStream = new FileStream(config.TmpDir + "\\" + BookShortName + ".ribce", FileMode.Create, FileAccess.Write, FileShare.None);
            logger.WriteToLog(Logger.Level.INFO, "Creating GZip stream.");
            Stream gzipStream = new GZipOutputStream(fileStream);
            logger.WriteToLog(Logger.Level.INFO, "Creating Tar stream.");
            TarArchive tarArchive = TarArchive.CreateOutputTarArchive(gzipStream);
            logger.WriteToLog(Logger.Level.INFO, "Creting output \"book\" file.");
            StreamWriter streamWriter = new StreamWriter(config.TmpDir + "\\book", true, new System.Text.UTF8Encoding(false));
            logger.WriteToLog(Logger.Level.INFO, "Opening source file.");
            reader.Open();
            logger.WriteToLog(Logger.Level.INFO, "Starting conversion");
            foreach (BookParagraph paragraph in reader)
            {
                if (paragraph.ParagraphType == BookParagraph.TYPE_PICTURE)
                {
                    paragraph.ImageFilePath = config.TmpDir + "\\img" + paragraph.ParagraphID + ".jpg";
                    if (paragraph.SaveImage())
                    {
                        Directory.SetCurrentDirectory(config.TmpDir);
                        TarEntry tarEntry = TarEntry.CreateEntryFromFile(Path.GetFileName(paragraph.ImageFilePath));
                        tarArchive.WriteEntry(tarEntry, false);
                    }
                }
                streamWriter.WriteLine(paragraph.GetFormattedLine());
                logger.WriteToLog(Logger.Level.PROGRESS, reader.ProgressPercentage + "%");
            }
            logger.WriteToLog(Logger.Level.SUCCESS, "100%");
            logger.WriteToLog(Logger.Level.SUCCESS, "Main sequence done!");
            logger.WriteToLog(Logger.Level.INFO, "Closing source file.");
            reader.Close();
            logger.WriteToLog(Logger.Level.INFO, "Closing \"book\" file.");
            streamWriter.Close();
            logger.WriteToLog(Logger.Level.INFO, "Adding \"book\" to " + BookShortName + ".ribce file.");
            Directory.SetCurrentDirectory(config.TmpDir);
            TarEntry BooktarEntry = TarEntry.CreateEntryFromFile("book");
            tarArchive.WriteEntry(BooktarEntry, false);
            logger.WriteToLog(Logger.Level.SUCCESS, "Done!");
            logger.WriteToLog(Logger.Level.INFO, "Closing " + BookShortName + ".ribce file.");
            tarArchive.Close();
            logger.WriteToLog(Logger.Level.INFO, "Closing streams.");
            gzipStream.Close();
            logger.WriteToLog(Logger.Level.INFO, "Closing streams.");
            fileStream.Close();
            logger.WriteToLog(Logger.Level.SUCCESS, "Done!");
        }
    }
}
