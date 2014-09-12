using System;
using System.Reflection;
using System.Drawing;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;

namespace RBCCD
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        static Mutex mutex = new Mutex(true, "{" + ((GuidAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(GuidAttribute), false)).Value.ToUpper() + "}");
        static bool ConsoleWndowVisible = true;

        public static NotifyIcon trayIcon;
        public static ContextMenu trayMenu;
        static void ShowHideWindow()
        {
            const int SW_HIDE = 0;
            const int SW_SHOW = 5;

            if (ConsoleWndowVisible)
            {
                ShowWindow(GetConsoleWindow(), SW_HIDE);
                ConsoleWndowVisible = false;
            }
            else
            {
                ShowWindow(GetConsoleWindow(), SW_SHOW);
                ConsoleWndowVisible = true;
            }
        }

        static void OnShowHide(object sender, EventArgs e)
        {
            ShowHideWindow();
        }

        static void OnExit(object sender, EventArgs e)
        {
            trayIcon.Dispose();
            Application.Exit();
        }

        [STAThread]
        static void Main(string[] args)
        {
            Config config = null;
            Logger logger = null;
            string ShortBookName;
            bool FirstRun = true;

            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                Console.Write("Daemon is already running! Exiting...");
                return;
            }
            Console.Title = Application.ProductName + " v" + Application.ProductVersion;
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

            for (int i = 0; i < config.WaitBeforeStartSeconds; i++)
            {
                Console.Write("Starting after " + (config.WaitBeforeStartSeconds - i) + " seconds... (Press Ctrl-C to close)\r");
                Thread.Sleep(1000);
                Console.Write("".PadRight(79) + "\r");
            }

            Thread notifyThread = new Thread(
                delegate()
                {
                    trayMenu = new ContextMenu();
                    trayMenu.MenuItems.Add("Show/Hide", OnShowHide);
                    trayMenu.MenuItems.Add("Exit", OnExit);
                    trayIcon = new NotifyIcon();
                    trayIcon.Text = Application.ProductName;
                    trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);
                    trayIcon.ContextMenu = trayMenu;
                    trayIcon.Visible = true;
                    Application.Run();
                });
            notifyThread.Start();
            
            ShowHideWindow();
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
            }
            mutex.ReleaseMutex();
        }
    }
}
