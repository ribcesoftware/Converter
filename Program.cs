using System;
using System.Diagnostics;
using System.Reflection;
using System.Drawing;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;

namespace RBCCD
{
    partial class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);
        [DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        static extern IntPtr RemoveMenu(IntPtr hMenu, uint nPosition, uint wFlags);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        static Mutex mutex = new Mutex(true, "{" + ((GuidAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(GuidAttribute), false)).Value.ToUpper() + "}");

        static NotifyIcon trayIcon;
        static ContextMenu trayMenu;
        static System.Threading.Timer timerObject = null;

        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        public static bool GetMinimized()
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(Process.GetCurrentProcess().MainWindowHandle, ref placement);
            return placement.showCmd == SW_SHOWMINIMIZED;
        }

        public static bool GetVisible()
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(Process.GetCurrentProcess().MainWindowHandle, ref placement);
            return placement.showCmd != SW_HIDE;
        }

        static void ShowConsoleWindow()
        {
            ShowWindow(GetConsoleWindow(), SW_SHOWNORMAL);
            SetForegroundWindow(GetConsoleWindow());
            timerObject = new System.Threading.Timer(new TimerCallback(TimerTick), null, 2000, 100);
        }

        static void HideConsoleWindow()
        {
            ShowWindow(GetConsoleWindow(), SW_HIDE);
            if (timerObject != null)
                timerObject.Dispose();
        }

        static void OnMenuShowLogClick(object sender, EventArgs e)
        {
            ShowConsoleWindow();
        }

        static void OnMenuHideLogClick(object sender, EventArgs e)
        {
            HideConsoleWindow();
        }

        static void OnIconClick(object sender, EventArgs e)
        {
            if (GetVisible())
                HideConsoleWindow();
            else
                ShowConsoleWindow();
        }

        static void OnMenuExit(object sender, EventArgs e)
        {
            trayIcon.Dispose();
            Process.GetCurrentProcess().Kill();
        }

        static void TimerTick(object state)
        {
            if (GetMinimized())
                HideConsoleWindow();
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
                Process.GetCurrentProcess().Kill();
            }
            Console.WriteLine("OK");
            Console.WriteLine();

            for (int i = 0; i < config.WaitBeforeStartSeconds; i++)
            {
                Console.Write("Starting after " + (config.WaitBeforeStartSeconds - i) + " seconds... (Press Ctrl-C to cancel launch)\r");
                Thread.Sleep(1000);
                Console.Write("".PadRight(79) + "\r");
            }

            Thread notifyThread = new Thread(delegate()
            {
                trayMenu = new ContextMenu();
                trayMenu.MenuItems.Add("Show log window", OnMenuShowLogClick);
                trayMenu.MenuItems.Add("Hide log window", OnMenuHideLogClick);
                trayMenu.MenuItems.Add("Shutdown Converter", OnMenuExit);
                trayMenu.MenuItems[0].DefaultItem = true;
                trayIcon = new NotifyIcon();
                trayIcon.Text = Application.ProductName;
                trayIcon.Icon = Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
                trayIcon.ContextMenu = trayMenu;
                trayIcon.Click += OnIconClick;
                trayIcon.DoubleClick += OnIconClick;
                trayIcon.Visible = true;
                Application.Run();
            });

            notifyThread.Start();

            Console.TreatControlCAsInput = true;
            Console.Title = Application.ProductName + " v" + Application.ProductVersion + " | Click min button to hide this window \u2192";
            IntPtr hSystemMenu = GetSystemMenu(Process.GetCurrentProcess().MainWindowHandle, false);
            EnableMenuItem(hSystemMenu, SC_CLOSE, MF_GRAYED);
            RemoveMenu(hSystemMenu, SC_CLOSE, MF_BYCOMMAND);
            HideConsoleWindow();
            
            logger.WriteToLog(Logger.Level.SUCCESS, "Daemon started successfuly.");
            try
            {
                while (true)
                {
                    if (!FirstRun) Thread.Sleep(config.SleepTimeMilliseconds);
                    FirstRun = false;
                    logger.WriteToLog(Logger.Level.INFO, "Requesting RIBCE server for new book...");
                    ShortBookName = Utils.GetNextBook(logger, config);
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
                        if (!Utils.SendNotification(logger, config, ShortBookName, Utils.STATUS_INPROGRESS))
                            if (Utils.LastServerResponse == "NOTINQUEUE")
                            {
                                logger.WriteToLog(Logger.Level.INFO, "Book \"" + ShortBookName + "\" is not in queue, it goes to another converter.");
                                logger.WriteToLog(Logger.Level.INFO, "Going to the next iteration.");
                                continue;
                            }
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
