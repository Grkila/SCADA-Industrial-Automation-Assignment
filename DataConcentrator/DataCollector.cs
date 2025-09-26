using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Data.Entity;
using PLCSimulator;

namespace DataConcentrator
{
    public class DataCollector : IDisposable
    {
        // Fields - Note: _db is now ContextClass, not DataCollector
        private readonly ContextClass _db;
        private readonly PLCSimulatorManager _plc;
        private readonly List<ActiveAlarm> _activeAlarms = new List<ActiveAlarm>();
        private readonly Timer _timer;

        // Events matching MockDataConcentratorService
        public event EventHandler ValuesUpdated;
        public event Action<ActiveAlarm> AlarmTriggered;

        // Additional fields for enhanced functionality
        private volatile bool isRunning = false;
        private static readonly object locker = new object();
        private List<Tag> tags;
        private Dictionary<string, System.Threading.Timer> tagTimers;

        // Constructor - Fixed to accept ContextClass instead of DataCollector
        public DataCollector(ContextClass db, PLCSimulatorManager plc)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _plc = plc ?? throw new ArgumentNullException(nameof(plc));

            // Initialize collections
            _activeAlarms = new List<ActiveAlarm>();
            tagTimers = new Dictionary<string, System.Threading.Timer>();

            _timer = new Timer(1);
            _timer.Elapsed += (s, e) => ReadValuesFromPLC();

            // Load configuration and start
            LoadConfiguration();
            _timer.Start();
            isRunning = true;

            Console.WriteLine("DataCollector initialized and started");
        }

        // Main method matching MockDataConcentratorService functionality
        private void ReadValuesFromPLC()
        {
            if (!isRunning) return;

            try
            {
                lock (locker)
                {
                    foreach (var tag in GetTags())
                    {
                        // The rule is still the same: must be an input tag with scanning enabled
                        if (tag.IsScanning != false && tag.IsInputTag())
                        {
                            // *** NEW LOGIC IS HERE ***
                            // Get the required scan time. Default to 1000ms if not set.
                            double requiredScanTime = tag.ScanTime ?? 1000;

                            // Check if enough time has passed since the last scan
                            if ((DateTime.Now - tag.LastScanned).TotalMilliseconds >= requiredScanTime)
                            {
                                try
                                {
                                    // It's time to scan this tag!
                                    double currentValue = ReadTagValue(tag);
                                    tag.CurrentValue = currentValue;

                                    // CRUCIAL: Update the last scanned time to now
                                    tag.LastScanned = DateTime.Now;
                                    if (tag.Type == TagType.AI)
                                    {
                                        var historyEntry = new TagValueHistory
                                        {
                                            TagName = tag.Name,
                                            Value = currentValue,
                                            Timestamp = tag.LastScanned // Koristimo vreme skeniranja
                                        };
                                        _db.TagValueHistory.Add(historyEntry);
                                    }
                                    SaveCurrentValueToDatabase(tag, currentValue);
                                    CheckAlarmsForTag(tag);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error reading tag {tag.Name}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
                ValuesUpdated?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ReadValuesFromPLC: {ex.Message}");
            }
        }

        // Alarm checking logic matching MockDataConcentratorService
        private void CheckAlarmsForTag(Tag tag)
        {
            if (!tag.CurrentValue.HasValue) return;

            bool isAlarmActiveForTag = false;

            foreach (var alarm in GetAlarms().Where(a => a.TagName == tag.Name))
            {
                bool alarmTriggered = false;

                if (alarm.Type == AlarmType.Above && tag.CurrentValue.Value > alarm.Limit)
                {
                    alarmTriggered = true;
                }
                else if (alarm.Type == AlarmType.Below && tag.CurrentValue.Value < alarm.Limit)
                {
                    alarmTriggered = true;
                }

                if (alarmTriggered)
                {
                    isAlarmActiveForTag = true;

                    // Check if this alarm is already active
                    if (!_activeAlarms.Any(a => a.TagName == tag.Name && a.Message == alarm.Message))
                    {
                        var newActiveAlarm = new ActiveAlarm
                        {
                            Time = DateTime.Now,
                            TagName = tag.Name,
                            Message = alarm.Message
                        };

                        _activeAlarms.Add(newActiveAlarm);

                        // Save to database
                        SaveActivatedAlarm(alarm, tag.Name, newActiveAlarm.Time);

                        // Trigger event
                        AlarmTriggered?.Invoke(newActiveAlarm);

                        Console.WriteLine($"ALARM TRIGGERED: {tag.Name} - {alarm.Message}");
                    }
                }
            }

            // Remove alarms that are no longer active for this tag
            if (!isAlarmActiveForTag)
            {
                var alarmsToRemove = _activeAlarms.Where(a => a.TagName == tag.Name).ToList();
                foreach (var alarm in alarmsToRemove)
                {
                    _activeAlarms.Remove(alarm);
                    Console.WriteLine($"ALARM CLEARED: {tag.Name} - {alarm.Message}");
                }
            }
        }

        // Public methods matching MockDataConcentratorService interface
        public IEnumerable<Tag> GetTags()
        {
            return tags ?? new List<Tag>();
        }

        public IEnumerable<ActiveAlarm> GetActiveAlarms()
        {
            lock (locker)
            {
                return _activeAlarms.ToList();
            }
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            _timer?.Stop();
            _timer?.Dispose();

            StopAllTimers();
            _plc?.Abort();

            Console.WriteLine("DataCollector stopped");
        }

        // Additional public methods for enhanced functionality
        public void SetTagScanning(string tagName, bool enable)
        {
            lock (locker)
            {
                try
                {
                    // Update in database
                    var tag = _db.Tags.FirstOrDefault(t => t.Name == tagName);
                    if (tag != null)
                    {
                        tag.IsScanning = enable;
                        _db.SaveChanges(); // Persists the change

                        // Update in the DataCollector's active memory
                        var memoryTag = tags?.FirstOrDefault(t => t.Name == tagName);
                        if (memoryTag != null)
                        {
                            memoryTag.IsScanning = enable;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating scan state for {tagName}: {ex.Message}");
                    throw;
                }
            }
        }

        public void WriteTagValue(string tagName, double value)
        {
            lock (locker)
            {
                var tag = tags?.FirstOrDefault(t => t.Name == tagName);
                if (tag != null && tag.IsOutputTag())
                {
                    try
                    {
                        // Write value to tag
                        tag.CurrentValue = value;

                        // Send to PLC simulator
                        WriteToPLCSimulator(tag, value);

                        // Save to database
                        SaveCurrentValueToDatabase(tag, value);

                        Console.WriteLine($"Successfully wrote {value} to tag {tagName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error writing to tag {tagName}: {ex.Message}");
                        throw;
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Tag {tagName} not found or is not an output tag");
                }
            }
        }

        public double? GetTagValue(string tagName)
        {
            lock (locker)
            {
                var tag = tags?.FirstOrDefault(t => t.Name == tagName);
                return tag?.CurrentValue;
            }
        }

        // Configuration management methods
        public void SaveConfiguration()
        {
            try
            {
                // Save all tags to database
                _db.SaveChanges();
                Console.WriteLine("Configuration saved to database");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
                throw;
            }
        }

        public void LoadConfiguration()
        {
            try
            {
                lock (locker)
                {
                    // Use GetTags method from ContextClass
                    tags = _db.GetTags().ToList();
                    Console.WriteLine($"Successfully loaded {tags.Count} tags from database");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                tags = new List<Tag>();
            }
        }

        public void AddTag(Tag tag)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            lock (locker)
            {
                try
                {
                    // Use AddTag method from ContextClass
                    _db.AddTag(tag);
                    _db.SaveChanges();

                    // Add to memory collection
                    tags.Add(tag);
                    Console.WriteLine($"Added tag {tag.Name} to DataCollector");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error adding tag {tag.Name}: {ex.Message}");
                    throw;
                }
            }
        }

        public void RemoveTag(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
                throw new ArgumentException("Invalid tag name", nameof(tagName));

            lock (locker)
            {
                try
                {
                    // Find and remove from database
                    var tagToRemove = _db.Tags.FirstOrDefault(t => t.Name == tagName);
                    if (tagToRemove != null)
                    {
                        _db.DeleteTag(tagToRemove);
                        _db.SaveChanges();

                        // Remove from memory collection
                        var memoryTag = tags?.FirstOrDefault(t => t.Name == tagName);
                        if (memoryTag != null)
                        {
                            tags.Remove(memoryTag);
                        }

                        Console.WriteLine($"Removed tag {tagName} from DataCollector");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error removing tag {tagName}: {ex.Message}");
                    throw;
                }
            }
        }

        public void AddAlarm(Alarm alarm)
        {
            if (alarm == null)
                throw new ArgumentNullException(nameof(alarm));

            lock (locker)
            {
                try
                {
                    _db.AddAlarm(alarm);
                    _db.SaveChanges();
                    Console.WriteLine($"Added alarm for tag {alarm.TagName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error adding alarm: {ex.Message}");
                    throw;
                }
            }
        }

        public void RemoveAlarm(Alarm alarm)
        {
            if (alarm == null)
                throw new ArgumentNullException(nameof(alarm));

            lock (locker)
            {
                try
                {
                    _db.DeleteAlarm(alarm);
                    _db.SaveChanges();
                    Console.WriteLine($"Removed alarm for tag {alarm.TagName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error removing alarm: {ex.Message}");
                    throw;
                }
            }
        }

        // Private helper methods
        private double ReadTagValue(Tag tag)
        {
            switch (tag.Type)
            {
                case TagType.AI:
                    return _plc.GetAnalogValue(GetTagAddress(tag));
                case TagType.DI:
                    return _plc.GetDigitalValue(GetTagAddress(tag));
                default:
                    throw new InvalidOperationException($"Cannot read value for tag type {tag.Type}");
            }
        }

        private void WriteToPLCSimulator(Tag tag, double value)
        {
            if (_plc == null) return;

            var address = GetTagAddress(tag);

            switch (tag.Type)
            {
                case TagType.AO:
                    _plc.SetAnalogValue(address, value);
                    break;
                case TagType.DO:
                    _plc.SetDigitalValue(address, value);
                    break;
                default:
                    throw new InvalidOperationException($"Cannot write to tag type {tag.Type}");
            }
        }

        private string GetTagAddress(Tag tag)
        {
            return tag.IOAddress?.StartsWith("ADDR") == true ? tag.IOAddress : $"ADDR{tag.IOAddress}";
        }

        private void SaveCurrentValueToDatabase(Tag tag, double currentValue)
        {
            try
            {
                var dbTag = _db.Tags.FirstOrDefault(t => t.Name == tag.Name);
                if (dbTag != null)
                {
                    dbTag.CurrentValue = currentValue;
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving current value for {tag.Name}: {ex.Message}");
            }
        }

        private void SaveActivatedAlarm(Alarm alarm, string tagName, DateTime time)
        {
            try
            {
                var activatedAlarm = new ActiveAlarm
                {
                    AlarmId = alarm.Id,
                    TagName = tagName,
                    Message = alarm.Message,
                    Time = time
                };

                _db.ActivatedAlarms.Add(activatedAlarm);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving activated alarm: {ex.Message}");
            }
        }

        private IEnumerable<Alarm> GetAlarms()
        {
            try
            {
                return _db.GetAlarms();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting alarms: {ex.Message}");
                return new List<Alarm>();
            }
        }

        private void StopAllTimers()
        {
            lock (locker)
            {
                foreach (var timer in tagTimers.Values)
                {
                    timer?.Dispose();
                }
                tagTimers.Clear();
            }
        }

        // Implement IDisposable pattern
        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Stop();
                    _db?.Dispose();
                }
                disposed = true;
            }
        }

        public void OnTagAdded(Tag tag)
        {
            lock (locker)
            {
                if (!tags.Any(t => t.Name == tag.Name))
                {
                    tags.Add(tag);
                    Console.WriteLine($"DataCollector dynamically added tag: {tag.Name}");
                }
            }
        }

        // Add this new method to dynamically remove a tag from the scanning list
        public void OnTagRemoved(Tag tag)
        {
            lock (locker)
            {
                var tagToRemove = tags.FirstOrDefault(t => t.Name == tag.Name);
                if (tagToRemove != null)
                {
                    tags.Remove(tagToRemove);
                    Console.WriteLine($"DataCollector dynamically removed tag: {tag.Name}");
                }
            }
        }

        ~DataCollector()
        {
            Dispose(false);
        }
    }
}