using System;

namespace RBCCD
{
    class Config
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

        public Config()
        {
            logToConsole = true;
            tmpDir = AppDomain.CurrentDomain.BaseDirectory + "\\tmp";
            logFile = AppDomain.CurrentDomain.BaseDirectory + "\\log.txt";
            ribceSiteBooksURL = "http://ribce.com/books";
            ribceSiteIfaceURL = "http://ribce.com/books/cnv_iface.php";
            sleepTimeMilliseconds = 60000;
            maxDownloadAttempt = 3;
            maxNotificationAttempt = 3;
            maxUploadAttempt = 3;
        }
    }
}
