using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class ReportField
{
    public string Title { get; set; } = "";
    public string InternalName { get; set; } = "";
    public string Content { get; set; } = "";
    public string Type { get; set; } = "";
    public List<string> Choices { get; set; } = [];
}

namespace WriteNippoLocally.Model
{
    class DailyReportModel
    {
        public DateTime Date { get; set; }
        public string FilePath { get; set; } = "";
        public List<ReportField> Fields { get; set; } = new();
        public int? Id { get; set; } = null;
        public bool IsSubmitted { get; set; }=false;
        public DailyReportModel()
        { }
    }
}
