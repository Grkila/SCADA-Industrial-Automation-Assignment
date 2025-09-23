using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public const int MIN_IO_ADDRESS = 0;
        public const int MAX_IO_ADDRESS = 65535;

        [Key]
        [StringLength(MAX_ID_LENGTH)]
        public string Id { get; set; }

        [Required]
        [StringLength(MAX_DESCRIPTION_LENGTH)]
        public string Description { get; set; }

        [Required]
        public int IOAddress { get; set; }

        [Required]
        public TagType Type { get; set; }

        // Direktna svojstva koja se mapiraju u bazu
        public double? ScanTime { get; set; }
        public bool? OnOffScan { get; set; }
        public double? LowLimit { get; set; }
        public double? HighLimit { get; set; }
        public string Units { get; set; }
        public double? InitialValue { get; set; }

        // Navigation property za alarme
        public virtual ICollection<Alarm> Alarms { get; set; }

        public Tag()
        {
            Alarms = new List<Alarm>();
        }

        public Tag(TagType type, string id, string description, int ioAddress) : this()
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

            alarm.TagId = this.Id;
            Alarms.Add(alarm);
        }

        public bool RemoveAlarm(string alarmId)
        {
            if (!IsAnalogInputTag())
            {
                throw new InvalidOperationException("Alarms can only be managed on AI (Analog Input) tags.");
            }

            var alarm = Alarms.FirstOrDefault(a => a.Id == alarmId);
            if (alarm != null)
            {
                return Alarms.Remove(alarm);
            }
            return false;
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
            if (IOAddress < MIN_IO_ADDRESS || IOAddress > MAX_IO_ADDRESS)
                throw new InvalidOperationException($"IO Address must be between {MIN_IO_ADDRESS} and {MAX_IO_ADDRESS}.");
        }
    }
}

