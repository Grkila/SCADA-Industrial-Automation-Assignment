using PLCSimulator;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataConcentrator
{
    
    public class DataCollector
    {
        private readonly ContextClass _db;
        private PLCSimulatorManager _plc;
        private volatile bool isRunning = false;
        private static readonly object locker = new object();
        private PLCSimulator.PLCSimulatorManager plcSimulator;
        private readonly List<ActivatedAlarm> _activeAlarms = new List<ActivatedAlarm>();

        private List<Tag> tags;
        
        private Dictionary<string, Timer> tagTimers;
        public event EventHandler ValuesUpdated;
        public event Action<ActivatedAlarm> AlarmTriggered;

        public DataCollector(ContextClass db, PLCSimulatorManager plc)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _plc = plc ?? throw new ArgumentNullException(nameof(plc));
            tagTimers = new Dictionary<string, Timer>();
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            try
            {
                // Učitavamo tagove i njihove vezane alarme odjednom (Eager Loading)
                tags = _db.Tags.Include(t => t.Alarms).ToList();
                Console.WriteLine($"[INFO] Successfully loaded {tags.Count} tags from the database.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to load configuration from database: {ex.Message}");
                // Inicijalizujemo praznu listu ako učitavanje ne uspe
                tags = new List<Tag>();
            }
        }

        public IEnumerable<Tag> GetTags() => _db.GetTags();

        public void AddTag(Tag tag)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            lock (locker)
            {
                try
                {
                    // Check if tag already exists
                    if (_db.Tags.Any(t => t.Id == tag.Id))
                    {
                        throw new InvalidOperationException($"Tag with ID '{tag.Id}' already exists in database.");
                    }

                    _db.Tags.Add(tag);
                    _db.SaveChanges();
                    Console.WriteLine($"Tag {tag.Id} saved to database");

                    // Then add to in-memory collection
                    tags.Add(tag);

                    // Start timer if needed
                    if (isRunning && ShouldScanTag(tag))
                    {
                        StartTimerForTag(tag);
                    }

                    Console.WriteLine($"Added tag {tag.Id} to DataCollector");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error adding tag {tag.Id}: {ex.Message}");
                    throw;
                }
            }   
        }

        public void RemoveTag(string tagId)
        {
            if (string.IsNullOrWhiteSpace(tagId))
                throw new ArgumentException("Invalid tag ID", nameof(tagId));

            lock (locker)
            {
                try
                {
                    // Remove from database first
                    var dbTag = _db.Tags.FirstOrDefault(t => t.Id == tagId);
                    if (dbTag != null)
                    {
                        _db.Tags.Remove(dbTag);
                        _db.SaveChanges();
                        Console.WriteLine($"Tag {tagId} removed from database");
                    }

                    // Remove from in-memory collection
                    var tagToRemove = tags.FirstOrDefault(t => t.Id == tagId);
                    if (tagToRemove != null)
                    {
                        tags.Remove(tagToRemove);
                        StopTimerForTag(tagId);
                        Console.WriteLine($"Removed tag {tagId} from DataCollector");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error removing tag {tagId}: {ex.Message}");
                    throw;
                }
            }
        }

        // Also need methods for updating existing tags
        public void UpdateTag(Tag updatedTag)
        {
            if (updatedTag == null)
                throw new ArgumentNullException(nameof(updatedTag));

            lock (locker)
            {
                try
                {
                    // Update in database
                    var dbTag = _db.Tags.FirstOrDefault(t => t.Id == updatedTag.Id);
                    if (dbTag == null)
                    {
                        throw new InvalidOperationException($"Tag {updatedTag.Id} not found in database");
                    }

                    // Update properties
                    dbTag.Description = updatedTag.Description;
                    dbTag.IOAddress = updatedTag.IOAddress;
                    dbTag.CharacteristicsJson = updatedTag.CharacteristicsJson;
                    
                    _db.SaveChanges();
                    Console.WriteLine($"Tag {updatedTag.Id} updated in database");

                    // Update in-memory collection
                    var existingTag = tags.FirstOrDefault(t => t.Id == updatedTag.Id);
                    if (existingTag != null)
                    {
                        // Stop current timer
                        StopTimerForTag(updatedTag.Id);
                        
                        // Replace tag
                        tags.Remove(existingTag);
                        tags.Add(updatedTag);
                        
                        // Restart timer if needed
                        if (isRunning && ShouldScanTag(updatedTag))
                        {
                            StartTimerForTag(updatedTag);
                        }
                    }

                    Console.WriteLine($"Tag {updatedTag.Id} updated in DataCollector");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating tag {updatedTag.Id}: {ex.Message}");
                    throw;
                }
            }
        }

        // Method for adding alarms to existing tags
        public IEnumerable<ActivatedAlarm> GetActiveAlarms() => _activeAlarms;

        public void AddAlarmToTag(string tagId, Alarm alarm)
        {
            if (string.IsNullOrWhiteSpace(tagId))
                throw new ArgumentException("Invalid tag ID", nameof(tagId));
            if (alarm == null)
                throw new ArgumentNullException(nameof(alarm));

            lock (locker)
            {
                try
                {
                    var dbTag = _db.Tags.Include(t => t.Alarms)
                        .FirstOrDefault(t => t.Id == tagId);
                    
                    if (dbTag == null)
                    {
                        throw new InvalidOperationException($"Tag {tagId} not found");
                    }

                    if (!dbTag.IsAnalogInputTag())
                    {
                        throw new InvalidOperationException("Alarms can only be added to AI tags");
                    }

                    alarm.TagId = tagId;
                    _db.Alarms.Add(alarm);
                    _db.SaveChanges();
                    
                    Console.WriteLine($"Alarm {alarm.Id} added to tag {tagId} in database");

                    // Update in-memory tag
                    var memoryTag = tags.FirstOrDefault(t => t.Id == tagId);
                    if (memoryTag != null)
                    {
                        memoryTag.Alarms.Add(alarm);
                        Console.WriteLine($"Alarm {alarm.Id} added to tag {tagId} in memory");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error adding alarm to tag {tagId}: {ex.Message}");
                    throw;
                }
            }
        }

        // Method for removing alarms from tags
        public void RemoveAlarmFromTag(string tagId, string alarmId)
        {
            if (string.IsNullOrWhiteSpace(tagId))
                throw new ArgumentException("Invalid tag ID", nameof(tagId));
            if (string.IsNullOrWhiteSpace(alarmId))
                throw new ArgumentException("Invalid alarm ID", nameof(alarmId));

            lock (locker)
            {
                try
                {
                    // Remove from database
                    var alarm = _db.Alarms.FirstOrDefault(a => a.Id == alarmId && a.TagId == tagId);
                    if (alarm != null)
                    {
                        _db.Alarms.Remove(alarm);
                        _db.SaveChanges();
                        Console.WriteLine($"Alarm {alarmId} removed from database");
                    }

                    // Remove from in-memory tag
                    var tag = tags.FirstOrDefault(t => t.Id == tagId);
                    if (tag != null)
                    {
                        var alarmToRemove = tag.Alarms.FirstOrDefault(a => a.Id == alarmId);
                        if (alarmToRemove != null)
                        {
                            tag.Alarms.Remove(alarmToRemove);
                            Console.WriteLine($"Alarm {alarmId} removed from tag {tagId} in memory");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error removing alarm {alarmId} from tag {tagId}: {ex.Message}");
                    throw;
                }
            }
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

        // Updated SetTagScanning method to persist configuration changes
        public void SetTagScanning(string tagId, bool enable)
        {
            lock (locker)
            {
                try
                {
                    // Update in database first
                    var dbTag = _db.Tags.FirstOrDefault(t => t.Id == tagId);
                    if (dbTag != null)
                    {
                        dbTag.OnOffScan = enable;
                        _db.SaveChanges();
                        Console.WriteLine($"Tag {tagId} scan state updated in database: {enable}");
                    }

                    // Update in memory
                    var tag = tags.FirstOrDefault(t => t.Id == tagId);
                    if (tag != null && tag.IsInputTag())
                    {
                        tag.ValidateAndSetOnOffScan(enable);

                        if (enable && isRunning)
                        {
                            StartTimerForTag(tag);
                        }
                        else
                        {
                            StopTimerForTag(tagId);
                        }

                        Console.WriteLine($"Tag {tagId} scanning: {(enable ? "ENABLED" : "DISABLED")}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating scan state for {tagId}: {ex.Message}");
                    throw;
                }
            }
        }
        
        public void Start()
        {
            if (isRunning)
                return;

            _plc.StartPLCSimulator();

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
            // Now IOAddress is already a string, so we can use it directly
            // or format it if needed
            return tag.IOAddress.StartsWith("ADDR") ? tag.IOAddress : $"ADDR{tag.IOAddress}";
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
                ValuesUpdated?.Invoke(this, EventArgs.Empty);

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
        
        // Updated ProcessTagValue method to persist current values
        private void ProcessTagValue(Tag tag, double currentValue)
        {
            tag.CurrentValue = currentValue;
            
            // Save current value to database periodically
            SaveCurrentValueToDatabase(tag, currentValue);

            var timestamp = DateTime.Now;
            var scanTime = tag.ScanTime ?? 1000;

            Console.WriteLine($"[{timestamp:HH:mm:ss.fff}] {tag.Id}: {currentValue:F2} {tag.Units} (scan: {scanTime}ms)");

            // Check alarms for AI tags
            if (tag.Type == TagType.AI)
            {
                var triggeredAlarms = tag.CheckAlarms(currentValue);

                if (triggeredAlarms.Any())
                {
                    Console.WriteLine($"ALARM on tag {tag.Id}:");

                    foreach (var alarm in triggeredAlarms)
                    {
                        Console.WriteLine($"   - {alarm.Id}: {alarm.Message}");
                        SaveActivatedAlarm(alarm, tag.Id);
                        var newActiveAlarm = new ActivatedAlarm 
                        {
                            AlarmTime = DateTime.Now,
                            Id = tag.Id,
                            Message = alarm.Message
                        };
                    }
                }
            }
        }

        private void SaveActivatedAlarm(Alarm alarm, string tagId)
        {
            try
            {
                var activatedAlarm = new ActivatedAlarm(alarm, tagId);
                _activeAlarms.Add(activatedAlarm);
                _db.ActivatedAlarms.Add(activatedAlarm);
                _db.SaveChanges();

                Console.WriteLine($"Alarm {alarm.Id} saved to database");
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
            if (plcSimulator == null) return;

            var address = GetTagAddress(tag); // Koristi postojeću helper metodu

            switch (tag.Type)
            {
                case TagType.AO:
                    // Za analog output - pošalji u PLC simulator
                    plcSimulator.SetAnalogValue(address, value); 
                    break;
                    
                case TagType.DO:
                    // Za digital output - pošalji u PLC simulator  
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

        // Method for persisting current values
        private void SaveCurrentValueToDatabase(Tag tag, double currentValue)
        {
            try
            {
                var dbTag = _db.Tags.FirstOrDefault(t => t.Id == tag.Id);
                if (dbTag != null)
                {
                    dbTag.CurrentValue = currentValue;
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving current value for {tag.Id}: {ex.Message}");
            }
        }
    }
}
