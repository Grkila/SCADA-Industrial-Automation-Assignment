using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
/*- - - - 
dodavanje i uklanjanje analognih i digitalnih veličina (tags) sa sledećim 
osobinama: 
 Tip taga (enumeracija: DI, DO, AI ili AO) 
 Tag name (id) 
 Description 
 I/O addres 
 Scan time (moguće uneti samo za input tagove) 
 On/off scan (moguće uneti samo za input tagove) 
 Low limit (moguće uneti samo za analogne tagove) 
 High Limit (moguće uneti samo za analogne tagove) 
 Units (moguće uneti samo za analogne tagove) 
 Initial value (moguće uneti samo za output tagove) 
 Alarms (ne unosi se prilikom pravljenja taga nego se prilikom 
pravljenja alarma on veže za određeni AI) 
Sve zajedničke karakteristike tagova neka budu posebno bolje. Ostale 
karakteristike smestiti u rečnik. 
Izvršiti validaciju unesenih vrednosti i onemogućiti korisnika da unese 
neadekvatne podatke (npr. ne može se uneti units za digitalne tagove)
- - - -*/
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

        private string _id;
        private string _description;
        private int _ioAddress;
        private TagType _type;

        // Direktna svojstva umesto Dictionary-ja
        private double? _scanTime;
        private bool? _onOffScan;
        private double? _lowLimit;
        private double? _highLimit;
        private string _units;
        private double? _initialValue;

        [Key]
        [StringLength(MAX_ID_LENGTH)]
        public string Id
        {
            get { return _id; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException("Id cannot be null or empty.");
                if (value.Length > MAX_ID_LENGTH)
                    throw new InvalidOperationException($"Id cannot exceed {MAX_ID_LENGTH} characters.");
                _id = value;
            }
        }

        [Required]
        [StringLength(MAX_DESCRIPTION_LENGTH)]
        public string Description
        {
            get { return _description; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException("Description cannot be null or empty.");
                if (value.Length > MAX_DESCRIPTION_LENGTH)
                    throw new InvalidOperationException($"Description cannot exceed {MAX_DESCRIPTION_LENGTH} characters.");
                _description = value;
            }
        }

        [Required]
        public int IOAddress
        {
            get { return _ioAddress; }
            set
            {
                if (value < MIN_IO_ADDRESS)
                    throw new InvalidOperationException($"IO Address cannot be less than {MIN_IO_ADDRESS}.");
                if (value > MAX_IO_ADDRESS)
                    throw new InvalidOperationException($"IO Address cannot exceed {MAX_IO_ADDRESS}.");
                _ioAddress = value;
            }
        }

        [Required]
        public TagType Type
        {
            get { return _type; }
            set
            {
                if (!Enum.IsDefined(typeof(TagType), value))
                    throw new InvalidOperationException($"Invalid TagType value: {value}");
                _type = value;
            }
        }

        // Navigation property za alarme
        public virtual ICollection<Alarm> Alarms { get; set; }

        public double? ScanTime
        {
            get
            {
                if (IsInputTag())
                    return _scanTime;
                else
                    throw new InvalidOperationException("ScanTime can only be accessed for input tags (AI, DI).");
            }
            set
            {
                if (IsInputTag())
                    _scanTime = value;
                else
                    throw new InvalidOperationException("ScanTime can only be set for input tags (AI, DI).");
            }
        }
        public bool? OnOffScan
        {
            get
            {
                if (IsInputTag())
                    return _onOffScan;
                else
                    throw new InvalidOperationException("OnOffScan can only be accessed for input tags (AI, DI).");
            }
            set
            {
                if (IsInputTag())
                    _onOffScan = value;
                else
                    throw new InvalidOperationException("OnOffScan can only be set for input tags (AI, DI).");
            }
        }
        public double? LowLimit
        {
            get
            {
                if (IsAnalogTag())
                    return _lowLimit;
                else
                    throw new InvalidOperationException("LowLimit can only be accessed for analog tags (AI, AO).");
            }
            set
            {
                if (IsAnalogTag())
                    _lowLimit = value;
                else
                    throw new InvalidOperationException("LowLimit can only be set for analog tags (AI, AO).");
            }
        }
        public double? HighLimit
        {
            get
            {
                if (IsAnalogTag())
                    return _highLimit;
                else
                    throw new InvalidOperationException("HighLimit can only be accessed for analog tags (AI, AO).");
            }
            set
            {
                if (IsAnalogTag())
                    _highLimit = value;
                else
                    throw new InvalidOperationException("HighLimit can only be set for analog tags (AI, AO).");
            }
        }
        public string Units
        {
            get
            {
                if (IsAnalogTag())
                    return _units;
                else
                    throw new InvalidOperationException("Units can only be accessed for analog tags (AI, AO).");
            }
            set
            {
                if (IsAnalogTag())
                    _units = value;
                else
                    throw new InvalidOperationException("Units can only be set for analog tags (AI, AO).");
            }
        }
        public double? InitialValue
        {
            get
            {
                if (IsOutputTag())
                    return _initialValue;
                else
                    throw new InvalidOperationException("InitialValue can only be accessed for output tags (AO, DO).");
            }
            set
            {
                if (IsOutputTag())
                    _initialValue = value;
                else
                    throw new InvalidOperationException("InitialValue can only be set for output tags (AO, DO).");
            }
        }

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

        private bool IsInputTag()
        {
            return Type == TagType.DI || Type == TagType.AI;
        }
        private bool IsOutputTag()
        {
            return Type == TagType.DO || Type == TagType.AO;
        }
        private bool IsAnalogTag()
        {
            return Type == TagType.AI || Type == TagType.AO;
        }
        private bool IsDigitalTag()
        {
            return Type == TagType.DI || Type == TagType.DO;
        }
        private bool IsAnalogInputTag()
        {
            return IsInputTag() && IsAnalogTag();
        }
        public void AddAlarm(Alarm alarm)
        {
            if (!IsAnalogInputTag() )
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

            // Postavi TagId na alarm
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
                // TryActivate vraća true samo ako je alarm upravo aktiviran (prelazi iz neaktivnog u aktivno)
                if (alarm.TryActivate(currentValue))
                {
                    newlyTriggeredAlarms.Add(alarm);
                }
            }

            return newlyTriggeredAlarms;
        }

        public IReadOnlyList<Alarm> GetAlarms()
        {
            return Alarms.AsReadOnly();
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

    }
}

