using System;
using System.IO;
using System.Drawing;
using System.Text;
using System.Net;

namespace RBCCD
{
    partial class BookParagraph
    {
        private int ParID;
        private int NextID;
        private int ChildID;
        private int Level;
        private int ParType;
        private string ParText;
        private string ImgFilePath;
        private Image GraphicsContent;
        private bool ImageSavedFlag;

        class UnsupportedParagraphTypeException : Exception { }

        public BookParagraph(int ParagraphType, object Content, int ParID = 1, int NextID = -1, int ChildID = -1, int Level = 0)
        {
            switch (ParagraphType)
            {
                case TYPE_ORDINARY_PAR:
                    ParText = (string)Content;
                    ParType = TYPE_ORDINARY_PAR;
                    break;
                case TYPE_PICTURE:
                    GraphicsContent = (Image)Content;
                    ParType = TYPE_PICTURE;
                    break;
                default:
                    throw new UnsupportedParagraphTypeException();
            }
            this.ParID = ParID;
            this.NextID = NextID;
            this.ChildID = ChildID;
            this.Level = Level;
            ImageSavedFlag = false;
        }

        private byte[] DoEncryption(byte[] data)
        {
            return data;
        }

        private string PrepareText(string text)
        {
            return Convert.ToBase64String(
                   DoEncryption(
                   UTF8Encoding.UTF8.GetBytes(
                   WebUtility.HtmlEncode(text).
                   Replace("\r\n", "&lt;br /&gt;").
                   Replace("\n\r", "&lt;br /&gt;").
                   Replace("\r", "&lt;br /&gt;").
                   Replace("\n", "&lt;br /&gt;").
                   Replace("'", "&#39;").
                   ToCharArray())));
        }

        private string GetParJson()
        {
            switch (ParType)
            {
                case TYPE_ORDINARY_PAR:
                    return "{}";
                case TYPE_PICTURE:
                    return "{\"image_file\":\"" + Path.GetFileName(ImgFilePath) + "\"}";
            }
            return "";
        }

        private string GetParText()
        {
            switch (ParType)
            {
                case TYPE_ORDINARY_PAR:
                    return PrepareText(ParText);
                case TYPE_PICTURE:
                    return PrepareText("Picture");
            }
            return "";
        }

        public int ParagraphType
        {
            get
            {
                return ParType;
            }
        }

        public int ParagraphID
        {
            get
            {
                return ParID;
            }
            set
            {
                if (value >= 0) ParID = value;
            }
        }

        public int NextParagraphID
        {
            get
            {
                return NextID;
            }
            set
            {
                if (value >= 0)
                    NextID = value;
                else
                    NextID = -1;
            }
        }

        public int ChildParagraphID
        {
            get
            {
                return ChildID;
            }
            set
            {
                if (value >= 0)
                    ChildID = value;
                else
                    ChildID = -1;
            }
        }

        public int ParagraphLevel
        {
            get
            {
                return Level;
            }
            set
            {
                if (value >= 0) Level = value;
            }
        }

        public string ImageFilePath
        {
            get
            {
                return ImgFilePath;
            }
            set
            {
                ImgFilePath = value;
            }
        }

        public bool ImageSaved
        {
            get
            {
                return ImageSavedFlag;
            }
        }

        public bool SaveImage()
        {
            if (GraphicsContent != null)
            {
                if (!ImageSavedFlag)
                {
                    try
                    {
                        GraphicsContent.Save(ImgFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                        ImageSavedFlag = true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        public string GetFormattedLine()
        {
            return ParID.ToString() + ">" +
                   ((NextID != -1) ? NextID.ToString() : "N") + ">" +
                   ((ChildID != -1) ? ChildID.ToString() : "N") + ">" +
                   Level.ToString() + ">" +
                   ParType.ToString() + ">" +
                   GetParJson() + ">" +
                   GetParText();
        }
    }
}
