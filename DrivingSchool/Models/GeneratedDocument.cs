using System;
using System.Collections.Generic;

namespace DrivingSchool.Models
{
    public class GeneratedDocument
    {
        public int Id { get; set; }
        public int? TemplateId { get; set; }
        public int StudentId { get; set; }
        public DateTime CreationDate { get; set; }
        public string FilePath { get; set; }
        public string Data { get; set; }

        public string StudentName { get; set; }
        public string TemplateName { get; set; }
        public string FileName => System.IO.Path.GetFileName(FilePath);
    }

    public class GeneratedDocumentCollection
    {
        public List<GeneratedDocument> Documents { get; set; } = new List<GeneratedDocument>();
    }
}