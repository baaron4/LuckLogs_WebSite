using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace LuckLogsApp.Models
{
    public class MemoryFile :HttpPostedFileBase
    {
        Stream stream;
        string contentType;
        string fileName;

        public MemoryFile(Stream stream, string contentType, string fileName)
        {
            this.stream = stream;
            this.contentType = contentType;
            this.fileName = fileName;
        }

        public override int ContentLength
        {
            get { return (int)stream.Length; }
        }

        public override string ContentType
        {
            get { return contentType; }
        }

        public override string FileName
        {
            get { return fileName; }
        }

        public override Stream InputStream
        {
            get { return stream; }
        }

        public override void SaveAs(string filename)
        {
            using (var file = File.Open(filename, FileMode.CreateNew))
                stream.CopyTo(file);
        }
    }
}