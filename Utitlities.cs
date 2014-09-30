using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace RBCCD
{
    class Logger
    {
        string LogFileName;
        bool LogToConsole;
        public enum Level
        {
            SUCCESS,
            INFO,
            PROGRESS,
            WARNING,
            ERROR,
            FATAL
        }

        public Logger(string LogFileName, bool LogToConsole)
        {
            this.LogFileName = LogFileName;
            this.LogToConsole = LogToConsole;
        }
        public void WriteToLog(Level level, string message)
        {
            string output = "[";
            switch (level)
            {
                case Level.SUCCESS:
                    output += "+";
                    break;
                case Level.INFO:
                    output += "~";
                    break;
                case Level.PROGRESS:
                    output += "~";
                    break;
                case Level.WARNING:
                    output += "*";
                    break;
                case Level.ERROR:
                    output += "-";
                    break;
                case Level.FATAL:
                    output += "!";
                    break;
            }

            output += "] ";
            output += DateTime.Now.ToString("MM.dd.yy HH:mm:ss");
            output += " " + message;
            if ((level == Level.PROGRESS) && LogToConsole)
                Console.Write(output + "\r");
            else
                try
                {
                    if (LogToConsole) Console.WriteLine(output);
                    StreamWriter sw = File.AppendText(LogFileName);
                    sw.WriteLine(output);
                    sw.Close();
                }
                catch (Exception) { }
        }
    }

    partial class Utils
    {
        public static string LastServerResponse;

        [DataContract]
        private class ServerResponse
        {
            [DataMember(Name = "book", IsRequired = true)]
            public string BookShortName;

            [DataMember(Name = "response", IsRequired = true)]
            public string ResponseString;
        }

        public static bool CleanTmpDir(string FullPath)
        {
            try
            {
                if (!Directory.Exists(FullPath))
                    Directory.CreateDirectory(FullPath);
                else
                    new DirectoryInfo(FullPath).GetFiles().ToList().ForEach(file => file.Delete());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetNextBook(Logger logger, Config config)
        {
            WebClient webClient = new WebClient();
            DataContractJsonSerializer JsonSerializer = new DataContractJsonSerializer(typeof(ServerResponse));
            try
            {
                ServerResponse response = (ServerResponse)JsonSerializer.ReadObject(
                       new MemoryStream(webClient.DownloadData(
                       config.RibceSiteIfaceURL + "?g=" +
                       Uri.EscapeDataString(Environment.UserDomainName + "\\" + Environment.UserName) +
                       "&i=" + config.SleepTimeMilliseconds +
                       "&v=" + Application.ProductVersion)));
                if (response.ResponseString == "OK")
                    return response.BookShortName;
                else
                {
                    LastServerResponse = response.ResponseString;
                    return "";
                }
            }
            catch (Exception)
            {
                logger.WriteToLog(Logger.Level.WARNING, "Can't connect to server!");
                return "";
            }
        }

        public static bool DownloadSourceFile(Logger logger, Config config, string BookShortName)
        {
            WebClient webClient = new WebClient();
            int attempt = 0;

            while (attempt < config.MaxDownloadAttempt)
            {
                try
                {
                    attempt++;
                    logger.WriteToLog(Logger.Level.INFO, "Trying to download source file. Attempt #" + attempt + " of " + config.MaxDownloadAttempt);
                    webClient.DownloadFile(config.RibceSiteBooksURL + "/" + BookShortName + "/src", config.TmpDir + "\\src.doc");
                    logger.WriteToLog(Logger.Level.SUCCESS, "File successfuly downloaded!");
                    return true;
                }
                catch (WebException e) { Console.WriteLine(e.Message); }
            }
            return false;
        }
        public static bool SendNotification(Logger logger, Config config, string BookShortName, int NotificationCode)
        {
            WebClient webClient = new WebClient();
            DataContractJsonSerializer JsonSerializer = new DataContractJsonSerializer(typeof(ServerResponse));
            int attempt = 0;

            while (attempt < config.MaxNotificationAttempt)
            {
                try
                {
                    attempt++;
                    logger.WriteToLog(Logger.Level.INFO, "Sending notification to RIBCE server...");
                    ServerResponse response = (ServerResponse)JsonSerializer.ReadObject(
                                   new MemoryStream(webClient.DownloadData(config.RibceSiteIfaceURL + "?b=" + BookShortName + "&n=" + NotificationCode)));

                    if (response.ResponseString == "OK")
                    {
                        logger.WriteToLog(Logger.Level.SUCCESS, "Notification successfuly sent.");
                        return true;
                    }
                    else
                    {
                        LastServerResponse = response.ResponseString;
                        logger.WriteToLog(Logger.Level.ERROR, "Sending notification server side error: " + response.ResponseString);
                        return false;
                    }
                }
                catch (WebException) { }
            }
            return false;
        }

        public static bool UploadRibceFile(Logger logger, Config config, string BookShortName, string RibceFileName)
        {
            WebClient webClient = new WebClient();
            DataContractJsonSerializer JsonSerializer = new DataContractJsonSerializer(typeof(ServerResponse));
            int attempt = 0;

            while (attempt < config.MaxUploadAttempt)
            {
                try
                {
                    attempt++;
                    logger.WriteToLog(Logger.Level.INFO, "Trying to upload " + BookShortName + ".ribce file. Attempt #" + attempt + " of " + config.MaxUploadAttempt);
                    ServerResponse response = (ServerResponse)JsonSerializer.ReadObject(
                                   new MemoryStream(webClient.UploadFile(config.RibceSiteIfaceURL + "?b=" + BookShortName + "&u=true", "POST", RibceFileName)));
                    if (response.ResponseString == "OK")
                    {
                        logger.WriteToLog(Logger.Level.SUCCESS, BookShortName + ".ribce file successfuly uploaded.");
                        return true;
                    }
                    else
                    {
                        LastServerResponse = response.ResponseString;
                        logger.WriteToLog(Logger.Level.ERROR, "Uploading server side error: " + response.ResponseString);
                        return false;
                    }
                }
                catch (WebException) { }
            }
            return false;
        }
    }
}
