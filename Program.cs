using System;
using System.Windows.Forms;
using System.Threading;

namespace RBCCD
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Config config = null;
            Logger logger = null;
            string ShortBookName;
            bool FirstRun = true;
            
            Console.WriteLine(Application.ProductName + " v" + Application.ProductVersion);
            Console.WriteLine("(C) 2014 " + Application.CompanyName);
            Console.Write("Loading config...");
            try
            {
                config = new Config();
                logger = new Logger(config.LogFile, config.LogToConsole);
            }
            catch(Exception)
            {
                Console.WriteLine("FAILED");
                Console.Write("Press any key to exit...");
                Console.ReadKey(true);
                System.Windows.Forms.Application.Exit();
            }
            Console.WriteLine("OK");
            Console.WriteLine();
            logger.WriteToLog(Logger.Level.SUCCESS, "Daemon started successfuly.");
            try
            {
                while (true)
                {
                    if (!FirstRun) Thread.Sleep(config.SleepTimeMilliseconds);
                    FirstRun = false;
                    logger.WriteToLog(Logger.Level.INFO, "Requesting RIBCE server for new book...");
                    ShortBookName = Utils.GetNextBook(config);
                    if (ShortBookName == "")
                    {
                        logger.WriteToLog(Logger.Level.INFO, "No book for conversion. Zzz..");
                        continue;
                    }
                    logger.WriteToLog(Logger.Level.SUCCESS, "New book: \"" + ShortBookName + "\", starting conversion procedure.");
                    logger.WriteToLog(Logger.Level.INFO, "Cleaning temp folder.");
                    Utils.CleanTmpDir(config.TmpDir);
                    logger.WriteToLog(Logger.Level.SUCCESS, "Temp folder cleaned.");
                    logger.WriteToLog(Logger.Level.INFO, "Downloading source file...");
                    if (!Utils.DownloadSourceFile(logger, config, ShortBookName))
                    {
                        logger.WriteToLog(Logger.Level.ERROR, "Error while downloading source file for book \"" + ShortBookName + "\"");
                        logger.WriteToLog(Logger.Level.WARNING, "Check your internet connection!");
                        logger.WriteToLog(Logger.Level.INFO, "Going to the next iteration.");
                        continue;
                    }
                    logger.WriteToLog(Logger.Level.INFO, "Creating Converter instance.");
                    Converter converter = new Converter(config.TmpDir + "\\src.doc", ShortBookName, logger, config);
                    logger.WriteToLog(Logger.Level.INFO, "Starting main sequence.");

                    try
                    {
                        Utils.SendNotification(logger, config, ShortBookName, Utils.STATUS_INPROGRESS);
                        converter.Run();
                        Utils.SendNotification(logger, config, ShortBookName, Utils.STATUS_CONVERTED);

                        if (Utils.UploadRibceFile(logger, config, ShortBookName, config.TmpDir + "\\" + ShortBookName + ".ribce"))
                            logger.WriteToLog(Logger.Level.SUCCESS, "All done. Waiting for next book...");
                        else
                            logger.WriteToLog(Logger.Level.ERROR, "Unable to uplad!");
                    }
                    catch (Reader.UnsupportedFileFormatException)
                    {
                        logger.WriteToLog(Logger.Level.ERROR, "Unsupported input file format!");
                        Utils.SendNotification(logger, config, ShortBookName, Utils.STATUS_FORMATERR);
                    }
                    catch (Reader.InternalErrorException)
                    {
                        logger.WriteToLog(Logger.Level.ERROR, "Internal converter Error!");
                        Utils.SendNotification(logger, config, ShortBookName, Utils.STATUS_CNVINTERR);
                    }
                    catch (Exception e)
                    {
                        logger.WriteToLog(Logger.Level.ERROR, "Internal Error!\n" + e.Message + "\n" + e.StackTrace);
                        Utils.SendNotification(logger, config, ShortBookName, Utils.STATUS_CNVINTERR);
                    }
                }
            }
            catch(Exception e)
            {
                logger.WriteToLog(Logger.Level.ERROR, "Fatal Error! See StackTrace:\n" + e.Message + "\n" + e.StackTrace);
                Console.ReadKey(true);
                System.Windows.Forms.Application.Exit();
            }
        }
    }
}
