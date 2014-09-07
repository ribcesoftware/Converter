using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RBCCD
{
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
    }
}
