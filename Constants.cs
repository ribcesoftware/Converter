using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RBCCD
{
    partial class Config
    {
        private const string CONFIG_FILENAME = "config.txt";
        private const char CONFIG_COMMENT_SYMBOL = '#';
        private const bool CONFIG_DEFAULT_LOG_TO_CONSOLE = true;
        private const string CONFIG_DEFAULT_TMP_DIR = "\\tmp";
        private const string CONFIG_DEFAULT_LOG_FILE = "\\log.txt";
        private const string CONFIG_DEFAULT_RIBCE_BOOK_URL = "http://ribce.com/books";
        private const string CONFIG_DEFAULT_RIBCE_CNV_IFACE_URL = "http://ribce.com/books/cnv_iface.php";
        private const int CONFIG_DEFAULT_SLEEP_TIME = 60000;
        private const int CONFIG_DEFAULT_MAX_DOWNLOAD_ATTEMPT = 3;
        private const int CONFIG_DEFAULT_MAX_UPLOAD_ATTEMPT = 3;
        private const int CONFIG_DEFAULT_MAX_NOTIFICATION_ATTEMPT = 3;
        private const int CONFIG_DEFAULT_WAIT_BEFORE_START_SECONDS = 0;
    }
    partial class BookParagraph
    {
        public const int TYPE_META = 0;
        public const int TYPE_ORDINARY_PAR = 1;
        public const int TYPE_BOOK_TITLE = 2;
        public const int TYPE_CHAPTER_TITLE = 3;
        public const int TYPE_SMALLEST_TITLE = 4;
        public const int TYPE_SMALL_TITLE = 5;
        public const int TYPE_MID_TITLE = 6;
        public const int TYPE_BIG_TITLE = 7;
        public const int TYPE_BIGGEST_TITLE = 8;
        public const int TYPE_FIRST_TITLE = 9;
        public const int TYPE_PICTURE = 12;
        public const int TYPE_VIDEO = 13;
        public const int TYPE_AUDIO = 14;
        public const int TYPE_COMMENT = 15;
    }

    partial class Utils
    {
        public const int STATUS_INQUEUE = 10;
        public const int STATUS_INPROGRESS = 20;
        public const int STATUS_FORMATERR = 30;
        public const int STATUS_CNVINTERR = 40;
        public const int STATUS_CONVERTED = 50;
    }

    partial class Converter
    {
        private byte[] OldMSWordSignature = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
        private byte[] NewMSWordSignature = { 0x50, 0x4B, 0x03, 0x04 };
        private byte[] PDFSignature = { 0x25, 0x50, 0x44, 0x46 };
    }

    partial class Program
    {
        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SC_CLOSE = 0xF060;
        private const int MF_ENABLED = 0;
        private const int MF_GRAYED = 1;
        private const int MF_BYCOMMAND = 0;
    }
}
