using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace DataConcentrator
{
    public enum TagType
    {
        DI,
        DO,
        AI,
        AO
    }

    [Table("Tags")]
    public class Tag
    {
        public const int MAX_ID_LENGTH = 50;
        public const int MAX_DESCRIPTION_LENGTH = 300;
        public const int MAX_IOADDRESS_LENGTH = 50;
        public const int MIN_IO_ADDRESS = 0;
        public const int MAX_IO_ADDRESS = 65535;

        [Key]
        [StringLength(MAX_ID_LENGTH)]
        public string Id { get; set; }

        [Required]
        [StringLength(MAX_DESCRIPTION_LENGTH)]
        public string Description { get; set; }

        [Required]
        [StringLength(MAX_IOADDRESS_LENGTH)]
        public string IOAddress { get; set; }

        [Required]
        public TagType Type { get; set; }

        // This is the property your application code will use - not mapped to database
        [NotMapped]
        public Dictionary<string, object> _characteristics = new Dictionary<string, object>();

        // This is the backing property that EF maps to the database
        public string CharacteristicsJson
        {
            get
            {
                return _characteristics == null || _characteristics.Count == 0 
                    ? null 
                    : JsonConvert.SerializeObject(_characteristics);
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _characteristics = new Dictionary<string, object>();
                }
                else
                {
                    try
                    {
                        _characteristics = JsonConvert.DeserializeObject<Dictionary<string, object>>(value) 
                                         ?? new Dictionary<string, object>();
                    }
                    catch (JsonException)
                    {
                        // If JSON is invalid, initialize with empty dictionary
                        _characteristics = new Dictionary<string, object>();
                    }
                }
            }
        }

        [NotMapped]
        public double? ScanTime
        {
            get => GetCharacteristic<double?>("ScanTime");
            set => SetCharacteristic("ScanTime", value);
        }

        [NotMapped]
        public bool? OnOffScan
        {
            get => GetCharacteristic<bool?>("OnOffScan");
            set => SetCharacteristic("OnOffScan", value);
        }

        [NotMapped]
        public double? LowLimit
        {
            get => GetCharacteristic<double?>("LowLimit");
            set => SetCharacteristic("LowLimit", value);
        }

        [NotMapped]
        public double? HighLimit
        {
            get => GetCharacteristic<double?>("HighLimit");
            set => SetCharacteristic("HighLimit", value);
        }

        [NotMapped]
        public string Units
        {
            get => GetCharacteristic<string>("Units");
            set => SetCharacteristic("Units", value);
        }

        [NotMapped]
        public double? InitialValue
        {
            get => GetCharacteristic<double?>("InitialValue");
            set => SetCharacteristic("InitialValue", value);
        }

        // Helper metode za rad sa dictionary
        public T GetCharacteristic<T>(string key)
        {
            return _characteristics.TryGetValue(key, out var value) ? (T)value : default(T);
        }
        public void SetCharacteristic(string key, object value)
        {
            if (value == null)
            {
                _characteristics.Remove(key);
            }
            else
            {
                _characteristics[key] = value;
            }
        }

        // Dodaj ovo svojstvo u Tag klasu
        [NotMapped]
        public double? CurrentValue { get; set; }

         public ICollection<Alarm> Alarms { get; set; }

        public Tag()
        {
            Alarms = new List<Alarm>();
        }

        public Tag(TagType type, string id, string description, string ioAddress) : this()
        {
            Type = type;
            Id = id;
            Description = description;
            IOAddress = ioAddress;
        }

        // Validacione metode
        public void ValidateAndSetScanTime(double? value)
        {
            if (!IsInputTag())
                throw new InvalidOperationException("ScanTime can only be set for input tags (AI, DI).");
            ScanTime = value;
        }       

        public void ValidateAndSetOnOffScan(bool? value)
        {
            if (!IsInputTag())
                throw new InvalidOperationException("OnOffScan can only be set for input tags (AI, DI).");
            OnOffScan = value;
        }

        public void ValidateAndSetLowLimit(double? value)
        {
            if (!IsAnalogTag())
                throw new InvalidOperationException("LowLimit can only be set for analog tags (AI, AO).");
            LowLimit = value;
        }

        public void ValidateAndSetHighLimit(double? value)
        {
            if (!IsAnalogTag())
                throw new InvalidOperationException("HighLimit can only be set for analog tags (AI, AO).");
            HighLimit = value;
        }

        public void ValidateAndSetUnits(string value)
        {
            if (!IsAnalogTag())
                throw new InvalidOperationException("Units can only be set for analog tags (AI, AO).");
            Units = value;
        }

        public void ValidateAndSetInitialValue(double? value)
        {
            if (!IsOutputTag())
                throw new InvalidOperationException("InitialValue can only be set for output tags (AO, DO).");
            InitialValue = value;
        }

        // Dodaj metodu za pisanje vrednosti
        public void WriteValue(double value)
        {
            if (!IsOutputTag())
                throw new InvalidOperationException("WriteValue can only be called on output tags (AO, DO).");
            
            if (IsDigitalTag() && value != 0 && value != 1)
                throw new ArgumentException("Digital tags can only accept values 0 or 1.");
            
            CurrentValue = value;
            Console.WriteLine($" Written {value} to tag {Id}");
        }

        // Dodaj metodu za čitanje trenutne vrednosti
        public double GetCurrentValue()
        {
            return CurrentValue ?? InitialValue ?? 0;
        }

        // Helper metode za validaciju tipova
        public bool IsInputTag()
        {
            return Type == TagType.DI || Type == TagType.AI;
        }

        public bool IsOutputTag()
        {
            return Type == TagType.DO || Type == TagType.AO;
        }

        public bool IsAnalogTag()
        {
            return Type == TagType.AI || Type == TagType.AO;
        }

        public bool IsDigitalTag()
        {
            return Type == TagType.DI || Type == TagType.DO;
        }

        public bool IsAnalogInputTag()
        {
            return IsInputTag() && IsAnalogTag();
        }

        // Metode za upravljanje alarmima
        public void AddAlarm(Alarm alarm)
        {
            if (!IsAnalogInputTag())
            {
                throw new InvalidOperationException("Alarms can only be added to AI (Analog Input) tags.");
            }

            if (alarm == null)
            {
                throw new ArgumentNullException(nameof(alarm));
            }

            if (Alarms.Any(a => a.Id == alarm.Id))
            {
                throw new InvalidOperationException($"Alarm with ID '{alarm.Id}' already exists for this tag.");
            }

            try
            {
                using (var context = new ContextClass())
                {
                    // Check if alarm ID already exists in database
                    if (context.Alarms.Any(a => a.Id == alarm.Id))
                    {
                        throw new InvalidOperationException($"Alarm with ID '{alarm.Id}' already exists in database.");
                    }

                    // Set the foreign key
                    alarm.TagId = this.Id;
                    
                    // Save to database
                    context.Alarms.Add(alarm);
                    context.SaveChanges();
                    
                    Console.WriteLine($"Alarm {alarm.Id} saved to database");
                }
                
                // Add to in-memory collection only after successful database save
                Alarms.Add(alarm);
                Console.WriteLine($"Alarm {alarm.Id} added to tag {this.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding alarm {alarm.Id}: {ex.Message}");
                throw;
            }
        }

        public bool RemoveAlarm(string alarmId)
        {
            if (!IsAnalogInputTag())
            {
                throw new InvalidOperationException("Alarms can only be managed on AI (Analog Input) tags.");
            }

            try
            {
                // Remove from database first
                using (var context = new ContextClass())
                {
                    var dbAlarm = context.Alarms.FirstOrDefault(a => a.Id == alarmId && a.TagId == this.Id);
                    if (dbAlarm != null)
                    {
                        context.Alarms.Remove(dbAlarm);
                        context.SaveChanges();
                        Console.WriteLine($"Alarm {alarmId} removed from database");
                    }
                }

                // Remove from in-memory collection
                var alarm = Alarms.FirstOrDefault(a => a.Id == alarmId);
                if (alarm != null)
                {
                    bool removed = Alarms.Remove(alarm);
                    if (removed)
                    {
                        Console.WriteLine($"Alarm {alarmId} removed from tag {this.Id}");
                    }
                    return removed;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing alarm {alarmId}: {ex.Message}");
                throw;
            }
        }

        public Alarm GetAlarm(string alarmId)
        {
            return Alarms.FirstOrDefault(a => a.Id == alarmId);
        }

        public List<Alarm> CheckAlarms(double currentValue)
        {
            if (!IsAnalogInputTag())
            {
                return new List<Alarm>();
            }

            var newlyTriggeredAlarms = new List<Alarm>();

            foreach (var alarm in Alarms)
            {
                if (alarm.TryActivate(currentValue))
                {
                    newlyTriggeredAlarms.Add(alarm);
                }
            }

            return newlyTriggeredAlarms;
        }

        public IReadOnlyList<Alarm> GetAlarms()
        {
            return (Alarms as List<Alarm>)?.AsReadOnly() ?? Alarms.ToList().AsReadOnly();
        }

        public List<Alarm> ResetAlarms(double currentValue)
        {
            if (!IsAnalogInputTag())
            {
                return new List<Alarm>();
            }

            var resetAlarms = new List<Alarm>();

            foreach (var alarm in Alarms.Where(a => a.IsActive && a.IsAcknowledged))
            {
                if (alarm.Reset(currentValue))
                {
                    resetAlarms.Add(alarm);
                }
            }

            return resetAlarms;
        }

        public bool AcknowledgeAlarm(string alarmId)
        {
            if (!IsAnalogInputTag())
            {
                throw new InvalidOperationException("Alarms can only be managed on AI (Analog Input) tags.");
            }

            var alarm = GetAlarm(alarmId);
            if (alarm != null)
            {
                return alarm.Acknowledge();
            }
            return false;
        }

        public List<Alarm> GetActiveAlarms()
        {
            return Alarms.Where(a => a.IsActive).ToList();
        }

        public List<Alarm> GetUnacknowledgedAlarms()
        {
            return Alarms.Where(a => a.IsActive && !a.IsAcknowledged).ToList();
        }

        public void UpdateAlarm(Alarm updatedAlarm)
        {
            if (!IsAnalogInputTag())
            {
                throw new InvalidOperationException("Alarms can only be managed on AI (Analog Input) tags.");
            }

            if (updatedAlarm == null)
            {
                throw new ArgumentNullException(nameof(updatedAlarm));
            }

            try
            {
                // Update in database first
                using (var context = new ContextClass())
                {
                    var dbAlarm = context.Alarms.FirstOrDefault(a => a.Id == updatedAlarm.Id && a.TagId == this.Id);
                    if (dbAlarm == null)
                    {
                        throw new InvalidOperationException($"Alarm {updatedAlarm.Id} not found in database");
                    }

                    // Update properties
                    dbAlarm.Trigger = updatedAlarm.Trigger;
                    dbAlarm.Threshold = updatedAlarm.Threshold;
                    dbAlarm.Message = updatedAlarm.Message;
                    
                    context.SaveChanges();
                    Console.WriteLine($"Alarm {updatedAlarm.Id} updated in database");
                }

                // Update in-memory collection
                var existingAlarm = Alarms.FirstOrDefault(a => a.Id == updatedAlarm.Id);
                if (existingAlarm != null)
                {
                    existingAlarm.Trigger = updatedAlarm.Trigger;
                    existingAlarm.Threshold = updatedAlarm.Threshold;
                    existingAlarm.Message = updatedAlarm.Message;
                    Console.WriteLine($"Alarm {updatedAlarm.Id} updated in memory");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating alarm {updatedAlarm.Id}: {ex.Message}");
                throw;
            }
        }

        // Validacija celokupnog objekta
        public void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new InvalidOperationException("Id cannot be null or empty.");
            if (Id.Length > MAX_ID_LENGTH)
                throw new InvalidOperationException($"Id cannot exceed {MAX_ID_LENGTH} characters.");
            if (string.IsNullOrWhiteSpace(Description))
                throw new InvalidOperationException("Description cannot be null or empty.");
            if (Description.Length > MAX_DESCRIPTION_LENGTH)
                throw new InvalidOperationException($"Description cannot exceed {MAX_DESCRIPTION_LENGTH} characters.");
            
            if (string.IsNullOrWhiteSpace(IOAddress))
                throw new InvalidOperationException("IOAddress cannot be null or empty.");
            if (IOAddress.Length > MAX_IOADDRESS_LENGTH)
                throw new InvalidOperationException($"IOAddress cannot exceed {MAX_IOADDRESS_LENGTH} characters.");
        }
    }
}

