using System.IO;
using System.Linq;
using DataConcentrator;
namespace ScadaGUI.Services
{
    public class ReportService
    {
        private readonly ContextClass _db;

        public ReportService(ContextClass db)
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
