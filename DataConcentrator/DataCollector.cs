using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace DataConcentrator
{
    
    public class DataCollector
    {
        
        private volatile bool isRunning = false;
        private static readonly object locker = new object();
        private PLCSimulator.PLCSimulatorManager plcSimulator;

        private List<Tag> tags;
        
        private Dictionary<string, Timer> tagTimers;
        
        public DataCollector()
        {
            tags = new List<Tag>();
            tagTimers = new Dictionary<string, Timer>();
        }



        public void AddTag(Tag tag)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            lock (locker)
            {
                tags.Add(tag);

                // Ako je sistem pokrenut i tag treba skenirati, pokreni timer
                if (isRunning && ShouldScanTag(tag))
                {
                    StartTimerForTag(tag);
                }
            }

            Console.WriteLine($" Added tag {tag.Id}");
        }

        public void RemoveTag(string tagId)
        {
            if (string.IsNullOrWhiteSpace(tagId))
                throw new ArgumentException("Invalid tag ID", nameof(tagId));

            lock (locker)
            {
                var tagToRemove = tags.FirstOrDefault(t => t.Id == tagId);
                if (tagToRemove != null)
                {
                    tags.Remove(tagToRemove);

                    // Zaustavi timer za ovaj tag
                    StopTimerForTag(tagId);
                }
            }

            Console.WriteLine($" Removed tag {tagId}");
        }

        private void StopTimerForTag(string tagId)
        {
            if (tagTimers.TryGetValue(tagId, out Timer timer))
            {
                timer.Dispose();
                tagTimers.Remove(tagId);
                Console.WriteLine($" Stopped timer for tag {tagId}");
            }
        }

        public void SetTagScanning(string tagId, bool enable)
        {
            lock (locker)
            {
                var tag = tags.FirstOrDefault(t => t.Id == tagId);
                if (tag != null && tag.IsInputTag())
                {
                    tag.ValidateAndSetOnOffScan(enable);

                    if (enable && isRunning)
                    {
                        // Uključi skeniranje - pokreni timer
                        StartTimerForTag(tag);
                    }
                    else
                    {
                        // Isključi skeniranje - zaustavi timer
                        StopTimerForTag(tagId);
                    }

                    Console.WriteLine($"🔄 Tag {tagId} scanning: {(enable ? "ENABLED" : "DISABLED")}");
                }
            }
        }
        public void Start(PLCSimulator.PLCSimulatorManager plcSimulator)
        {
            if (isRunning)
                return;

            this.plcSimulator = plcSimulator ?? throw new ArgumentNullException(nameof(plcSimulator));
            plcSimulator.StartPLCSimulator();

            isRunning = true;

            // Inicijalizuj output tagove sa initial values
            InitializeOutputTags();
            
            StartAllTagTimers();

            Console.WriteLine("DataCollector started with individual timers");
        }
        private double ReadTagValue(Tag tag)
        {
            switch (tag.Type)
            {
                case TagType.AI:
                    return plcSimulator.GetAnalogValue(GetTagAddress(tag));
                case TagType.DI:
                    return plcSimulator.GetDigitalValue(GetTagAddress(tag));
                default:
                   throw new InvalidOperationException($"Cannot read value for tag type {tag.Type}");
            }
        }
        private string GetTagAddress(Tag tag)
        {
           int IOadress = tag.IOAddress;
              return $"ADDR{IOadress:D3}";
        }
        private void StartAllTagTimers()
        {

            lock (locker)
            {
                foreach (var tag in tags.Where(t => ShouldScanTag(t)))
                {
                    StartTimerForTag(tag);
                }
            }

            Console.WriteLine($"Started {tagTimers.Count} tag timers");
        }
        private void StartTimerForTag(Tag tag)
        {
            if (tagTimers.ContainsKey(tag.Id))
            {
                // Timer već postoji, ne pravi duplikat
                return;
            }

            var scanIntervalMs = (int)(tag.ScanTime ?? 1000); // Default 1000ms ako nije postavljen

            var timer = new Timer(
                callback: (state) => ScanSingleTag(tag),  // Šta da radi kad se aktivira
                state: null,                              // Dodatni podaci (ne trebaju nam)
                dueTime: 100,                            // Prvi put nakon 100ms (kratka pauza)
                period: scanIntervalMs                    // Zatim svakih scanIntervalMs milisekundi
            );

            tagTimers[tag.Id] = timer;
            Console.WriteLine($" Started timer for tag {tag.Id} - scanning every {scanIntervalMs}ms");
        }

        private bool ShouldScanTag(Tag tag)
        {
            // Skeniramo samo input tagove (DI, AI) koji imaju OnOffScan = true
            return tag.IsInputTag() && tag.OnOffScan == true;
        }
        private void ScanSingleTag(Tag tag)
        {
            // Proveri da li sistem još uvek radi i da li tag treba skenirati
            if (!isRunning || !ShouldScanTag(tag))
                return;

            try
            {
                // Čitaj vrednost iz PLC simulatora
                double currentValue = ReadTagValue(tag);

                // Obradi vrednost (proveri alarme, ispiši, sačuvaj u bazu)
                ProcessTagValue(tag, currentValue);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning tag {tag.Id}: {ex.Message}");
            }
        }
        public void Stop()
        {
            if (!isRunning)
                return;

            isRunning = false;

            // Zaustavi sve timer-e
            StopAllTimers();

            // Zaustavi PLC simulator
            plcSimulator?.Abort();

            Console.WriteLine("DataCollector stopped");
        }

        private void StopAllTimers()
        {
            lock (locker)
            {
                foreach (var timer in tagTimers.Values)
                {
                    timer?.Dispose(); // Dispose oslobađa resurse timer-a
                }
                tagTimers.Clear();
            }

            Console.WriteLine($"Stopped all tag timers");
        }
        private void ProcessTagValue(Tag tag, double currentValue)
        {
            var timestamp = DateTime.Now;
            var scanTime = tag.ScanTime ?? 1000;

            Console.WriteLine($"[{timestamp:HH:mm:ss.fff}]  {tag.Id}: {currentValue:F2} {tag.Units} (scan: {scanTime}ms)");

            // Proveri alarme samo za AI tagove
            if (tag.Type == TagType.AI)
            {
                var triggeredAlarms = tag.CheckAlarms(currentValue);

                if (triggeredAlarms.Any())
                {
                    Console.WriteLine($"ALARM na tag {tag.Id}:");

                    foreach (var alarm in triggeredAlarms)
                    {
                        Console.WriteLine($"   - {alarm.Id}: {alarm.Message}");

                        // Sačuvaj aktivirani alarm u bazu podataka
                        SaveActivatedAlarm(alarm, tag.Id);
                    }
                }
            }
        }

        private void SaveActivatedAlarm(Alarm alarm, string tagId)
        {
            try
            {
                using (var context = new ContextClass())
                {
                    var activatedAlarm = new ActivatedAlarm(alarm, tagId);
                    context.ActivatedAlarms.Add(activatedAlarm);
                    context.SaveChanges();

                    Console.WriteLine($"Alarm {alarm.Id} saved to database");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error saving alarm: {ex.Message}");
            }
        }

        // Metoda za pisanje u output tagove
        public void WriteTagValue(string tagId, double value)
        {
            lock (locker)
            {
                var tag = tags.FirstOrDefault(t => t.Id == tagId);
                if (tag != null && tag.IsOutputTag())
                {
                    try
                    {
                        // Upiši vrednost u tag
                        tag.WriteValue(value);
                        
                        // Pošalji vrednost u PLC simulator
                        WriteToPLCSimulator(tag, value);
                        
                        Console.WriteLine($" Successfully wrote {value} to tag {tagId}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($" Error writing to tag {tagId}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Tag {tagId} not found or is not an output tag");
                }
            }
        }

        private void WriteToPLCSimulator(Tag tag, double value)
        {
            // Ovde možeš dodati mapiranje ili direktno pisanje
            if (plcSimulator == null) return; // Add this check
            string address = "ADR"+tag.IOAddress.ToString("D3");
            switch (tag.Type)
            {
                case TagType.AO:
                    // Za analog output - pošalji u PLC simulator
                    plcSimulator.SetAnalogValue(address, value);
                    break;
                    
                case TagType.DO:
                    // Za digital output - pošalji u PLC simulator
                    if (value != 0 && value != 1)
                        throw new InvalidOperationException("Digital output value must be 0 or 1.");
                    plcSimulator.SetDigitalValue(address, value);
                    break;
            }
        }

        // Metoda za čitanje trenutne vrednosti output tag-a
        public double? GetTagValue(string tagId)
        {
            lock (locker)
            {
                var tag = tags.FirstOrDefault(t => t.Id == tagId);
                return tag?.GetCurrentValue();
            }
        }

        // Metoda za postavljanje initial values na početku
        public void InitializeOutputTags()
        {
            lock (locker)
            {
                foreach (var tag in tags.Where(t => t.IsOutputTag()))
                {
                    if (tag.InitialValue.HasValue)
                    {
                        WriteTagValue(tag.Id, tag.InitialValue.Value);
                        Console.WriteLine($"🔧 Initialized tag {tag.Id} with value {tag.InitialValue.Value}");
                    }
                }
            }
        }

    }
}
