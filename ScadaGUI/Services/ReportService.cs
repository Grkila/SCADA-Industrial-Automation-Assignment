using System.IO;
using System.Linq;
using ScadaGUI.Models;

namespace ScadaGUI.Services
{
    public class ReportService
    {
        private readonly MockDatabaseService _db;

        public ReportService(MockDatabaseService db)
        {
            _db = db;
        }

        public string GenerateReport()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Report.txt");
            using (var writer = new StreamWriter(path))
            {
                foreach (var tag in _db.GetTags().Where(t => t.Type == TagType.AI))
                {
                    if (tag.LowLimit.HasValue && tag.HighLimit.HasValue && tag.InitialValue.HasValue)
                    {
                        var avg = (tag.HighLimit.Value + tag.LowLimit.Value) / 2;
                        if (tag.InitialValue.Value >= avg - 5 && tag.InitialValue.Value <= avg + 5)
                        {
                            writer.WriteLine($"{tag.Name}: {tag.InitialValue} {tag.Units}");
                        }
                    }
                }
            }
            return path;
        }
    }
}
