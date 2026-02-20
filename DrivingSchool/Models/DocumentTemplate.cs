using System;
using System.Collections.Generic;

namespace DrivingSchool.Models
{
    public class DocumentTemplate
    {
        public int Id { get; set; }
        public string TemplateName { get; set; }
        public string DocumentType { get; set; }
        public string CategoryCode { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public long? FileSize { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Для отображения
        public string FileSizeFormatted
        {
            get
            {
                if (!FileSize.HasValue) return "0 Б";
                if (FileSize < 1024) return $"{FileSize} Б";
                if (FileSize < 1024 * 1024) return $"{FileSize / 1024:N1} КБ";
                return $"{FileSize / (1024 * 1024):N1} МБ";
            }
        }
    }

    public class DocumentTemplateCollection
    {
        public List<DocumentTemplate> Templates { get; set; } = new List<DocumentTemplate>();
    }
}