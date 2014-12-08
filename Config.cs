using System;
using System.IO;
using System.Collections.Generic;

namespace RBCCD
{
    partial class Config
    {
        private string logFile;
        private bool logToConsole;
        private string tmpDir;
        private string ribceSiteBooksURL;
        private string ribceSiteIfaceURL;
        private int sleepTimeMilliseconds;
        private int maxDownloadAttempt;
        private int maxNotificationAttempt;
        private int maxUploadAttempt;
        private int waitBeforeStartSeconds;

        public class InvalidConfigFileRecord : Exception {};

        public string LogFile
        {
            get { return logFile; }
        }

        public bool LogToConsole
        {
            get { return logToConsole; }
        }

        public string TmpDir
        {
            get { return tmpDir; }
        }

        public string RibceSiteBooksURL
        {
            get { return ribceSiteBooksURL; }
        }

        public string RibceSiteIfaceURL
        {
            get { return ribceSiteIfaceURL; }
        }

        public int SleepTimeMilliseconds
        {
            get { return sleepTimeMilliseconds; }
        }

        public int MaxDownloadAttempt
        {
            get { return maxDownloadAttempt; }
        }

        public int MaxNotificationAttempt
        {
            get { return maxNotificationAttempt; }
        }

        public int MaxUploadAttempt
        {
            get { return maxUploadAttempt; }
        }

        public int WaitBeforeStartSeconds
        {
            get { return waitBeforeStartSeconds; }
        }
            
    
        public Config()
        {
            Dictionary<string, string> configDictionary = new Dictionary<string,string>();
            FileInfo config_file = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "\\" + CONFIG_FILENAME);
            if (config_file.Exists)
            {
                StreamReader reader = new StreamReader(config_file.FullName);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string clearLine = (line.IndexOf(CONFIG_COMMENT_SYMBOL) != -1 ? line.Substring(0, line.IndexOf(CONFIG_COMMENT_SYMBOL)) : line).Trim();
                    if (clearLine.Length != 0)
                    {
                        string[] keyValue = clearLine.Split(new Char [] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (keyValue.Length == 2)
                            configDictionary[keyValue[0]] = keyValue[1];
                        else
                            throw new InvalidConfigFileRecord();
                    }
                    
                }
                reader.Close();
            }

            if (configDictionary.ContainsKey("logToConsole"))
                if (Array.IndexOf(new string[] { "true", "false" }, configDictionary["logToConsole"]) != -1)
                    logToConsole = (configDictionary["logToConsole"] == "true"?true:false);
                else
                    throw new InvalidConfigFileRecord();
            else
                logToConsole = CONFIG_DEFAULT_LOG_TO_CONSOLE;

            if (configDictionary.ContainsKey("tmpDir"))
                tmpDir = configDictionary["tmpDir"].Replace("APP_EXE_PATH", AppDomain.CurrentDomain.BaseDirectory).Replace("CUR_DIR", Directory.GetCurrentDirectory());
            else
                tmpDir = AppDomain.CurrentDomain.BaseDirectory + CONFIG_DEFAULT_TMP_DIR;

            if (configDictionary.ContainsKey("logFile"))
                logFile = configDictionary["logFile"].Replace("APP_EXE_PATH", AppDomain.CurrentDomain.BaseDirectory).Replace("CUR_DIR", Directory.GetCurrentDirectory());
            else
                logFile = AppDomain.CurrentDomain.BaseDirectory + CONFIG_DEFAULT_LOG_FILE;

            if (configDictionary.ContainsKey("ribceSiteBooksURL"))
                ribceSiteBooksURL = configDictionary["ribceSiteBooksURL"];
            else
                ribceSiteBooksURL = CONFIG_DEFAULT_RIBCE_BOOK_URL;

            if (configDictionary.ContainsKey("ribceSiteIfaceURL"))
                ribceSiteIfaceURL = configDictionary["ribceSiteIfaceURL"];
            else
                ribceSiteIfaceURL = CONFIG_DEFAULT_RIBCE_CNV_IFACE_URL;

            if (configDictionary.ContainsKey("sleepTimeMilliseconds"))
                if (!Int32.TryParse(configDictionary["sleepTimeMilliseconds"], out sleepTimeMilliseconds))
                    throw new InvalidConfigFileRecord();
                else
                {
                    if ((sleepTimeMilliseconds < 0) || (sleepTimeMilliseconds > 10))
                        throw new InvalidConfigFileRecord();
                }
            else
                sleepTimeMilliseconds = CONFIG_DEFAULT_SLEEP_TIME;

            if (configDictionary.ContainsKey("maxDownloadAttempt"))
                if (!Int32.TryParse(configDictionary["maxDownloadAttempt"], out maxDownloadAttempt))
                    throw new InvalidConfigFileRecord();
                else
                {
                    if ((maxDownloadAttempt < 0) || (maxDownloadAttempt > 10))
                        throw new InvalidConfigFileRecord();
                }
            else
                maxDownloadAttempt = CONFIG_DEFAULT_MAX_DOWNLOAD_ATTEMPT;

            if (configDictionary.ContainsKey("maxNotificationAttempt"))
                if (!Int32.TryParse(configDictionary["maxNotificationAttempt"], out maxNotificationAttempt))
                    throw new InvalidConfigFileRecord();
                else
                {
                    if ((maxNotificationAttempt < 0) || (maxNotificationAttempt > 10))
                        throw new InvalidConfigFileRecord();
                }
            else
                maxNotificationAttempt = CONFIG_DEFAULT_MAX_NOTIFICATION_ATTEMPT;

            if (configDictionary.ContainsKey("maxUploadAttempt"))
                if (!Int32.TryParse(configDictionary["maxUploadAttempt"], out maxUploadAttempt))
                    throw new InvalidConfigFileRecord();
                else
                {
                    if ((maxUploadAttempt < 0) || (maxUploadAttempt > 1000))
                        throw new InvalidConfigFileRecord();
                }
            else
                maxUploadAttempt = CONFIG_DEFAULT_MAX_UPLOAD_ATTEMPT;

            if (configDictionary.ContainsKey("waitBeforeStartSeconds"))
                if (!Int32.TryParse(configDictionary["waitBeforeStartSeconds"], out waitBeforeStartSeconds))
                    throw new InvalidConfigFileRecord();
                else
                {
                    if (waitBeforeStartSeconds < 0)
                        throw new InvalidConfigFileRecord();
                }
            else
                waitBeforeStartSeconds = CONFIG_DEFAULT_WAIT_BEFORE_START_SECONDS;
        }
    }
}
