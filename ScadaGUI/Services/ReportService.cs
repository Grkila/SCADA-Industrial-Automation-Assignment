using DataConcentrator;
using System;
using System.IO;
using System.Linq;
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
            var path = Path.Combine(Directory.GetCurrentDirectory(), $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine($"SCADA Report - Generated on {DateTime.Now}");
                writer.WriteLine("Values of analog inputs that were in the ideal range ((high+low)/2 ±5)");
                writer.WriteLine("=====================================================================");

                // Prolazimo samo kroz Analog Input (AI) tagove
                foreach (var tag in _db.GetTags().Where(t => t.Type == TagType.AI))
                {
                    if (tag.LowLimit.HasValue && tag.HighLimit.HasValue)
                    {
                        // Izračunavamo ciljni opseg kao i pre
                        double middleValue = (tag.HighLimit.Value + tag.LowLimit.Value) / 2.0;
                        double lowerBound = middleValue - 5.0;
                        double upperBound = middleValue + 5.0;

                        writer.WriteLine($"\n--- Tag: {tag.Name} (Target Range: {lowerBound:F2} - {upperBound:F2} {tag.Units}) ---");

                        // Pretražujemo istoriju za vrednosti unutar ciljnog opsega
                        var historicalValuesInRange = _db.TagValueHistory
                            .Where(h => h.TagName == tag.Name &&
                                        h.Value >= lowerBound &&
                                        h.Value <= upperBound)
                            .OrderBy(h => h.Timestamp) // Sortiramo po vremenu
                            .ToList();

                        if (historicalValuesInRange.Any())
                        {
                            foreach (var history in historicalValuesInRange)
                            {
                                writer.WriteLine($"{history.Timestamp:dd.MM.yyyy HH:mm:ss} -> Value: {history.Value:F2} {tag.Units}");
                            }
                        }
                        else
                        {
                            writer.WriteLine("No recorded values found in the target range.");
                        }
                    }
                }
            }
            return path;
        }
    }
}
