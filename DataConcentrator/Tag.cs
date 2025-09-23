using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
    public class Tag
    {

        private const int MAX_ID_LENGTH = 50;
        private const int MAX_DESCRIPTION_LENGTH = 300;
        private const int MIN_IO_ADDRESS = 0;
        private const int MAX_IO_ADDRESS = 65535; 

        private string _id;
        private string _description;
        private int _ioAddress;
        private TagType _type;

        public TagType Type
        {
            get => _type;
            set
            {
                if (!Enum.IsDefined(typeof(TagType), value))
                    throw new InvalidOperationException($"Invalid TagType value: {value}");
                _type = value;
            }
        }

        public string Id
        {
            get
            {
                return _id;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException("Id cannot be null or empty.");
                if (value.Length > MAX_ID_LENGTH)
                    throw new InvalidOperationException($"Id cannot exceed {MAX_ID_LENGTH} characters.");

                _id = value;
            }
        }

        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException("Description cannot be null or empty.");
                if (value.Length > MAX_DESCRIPTION_LENGTH)
                    throw new InvalidOperationException($"Description cannot exceed {MAX_DESCRIPTION_LENGTH} characters.");

                _description = value;
            }
        }

        public int IOAddress
        {
            get
            {
                return _ioAddress;
            }
            set
            {
                if (value < MIN_IO_ADDRESS)
                    throw new InvalidOperationException($"IO Address cannot be less than {MIN_IO_ADDRESS}.");
                if (value > MAX_IO_ADDRESS)
                    throw new InvalidOperationException($"IO Address cannot exceed {MAX_IO_ADDRESS}.");

                _ioAddress = value;
            }
        }

        private Dictionary<string, object> properties;
        // public List<Alarm> Alarms { get; set; } //TODO: implementirati klasu Alarm

        public Tag()
        {
            properties = new Dictionary<string, object>();
            Alarms = new List<Alarm>();
        }

        public Tag(TagType type, string id, string description, int ioAddress) : this()
        {
            Type = type;
            Id = id;
            Description = description;
            IOAddress = ioAddress;
        }

        public double? ScanTime
        {
            get
            {
                if (IsInputTag())
                {
                    return (double?)properties["ScanTime"];
                }
                else
                {
                    throw new InvalidOperationException("ScanTime can only be set for input tags (AI, DI).");
                }
            }
            set
            {
                if (IsInputTag())
                {
                    properties["ScanTime"] = value;
                }
                else
                {
                    throw new InvalidOperationException("ScanTime can only be set for input tags (AI, DI).");
                }
            }
        }
        public bool? OnOffScan
        {
            get
            {
                if (IsInputTag())
                {
                    return (bool?)properties["OnOffScan"];
                }
                else
                {
                    throw new InvalidOperationException("OnOffScan can only be set for input tags (AI, DI).");
                }
            }
            set
            {
                if (IsInputTag())
                {
                    properties["OnOffScan"] = value;
                }
                else
                {
                    throw new InvalidOperationException("OnOffScan can only be set for input tags (AI, DI).");
                }
            }
        }
        public double? LowLimit
        {
            get
            {
                if (IsAnalogTag())
                {
                    return (double?)properties["LowLimit"];
                }
                else
                {
                    throw new InvalidOperationException("LowLimit can only be set for analog tags (AI, AO).");
                }
            }
            set
            {
                if (IsAnalogTag())
                {
                    properties["LowLimit"] = value;
                }
                else
                {
                    throw new InvalidOperationException("LowLimit can only be set for analog tags (AI, AO).");
                }
            }
        }
        public double? HighLimit
        {
            get
            {
                if (IsAnalogTag())
                {
                    return (double?)properties["HighLimit"];
                }
                else
                {
                    throw new InvalidOperationException("HighLimit can only be set for analog tags (AI, AO).");
                }
            }
            set
            {
                if (IsAnalogTag())
                {
                    properties["HighLimit"] = value;
                }
                else
                {
                    throw new InvalidOperationException("HighLimit can only be set for analog tags (AI, AO).");
                }
            }
        }
        public string Units
        {
            get
            {
                if (IsAnalogTag())
                {
                    return (string)properties["Units"];
                }
                else
                {
                    throw new InvalidOperationException("Units can only be set for analog tags (AI, AO).");
                }
            }
            set
            {
                if (IsAnalogTag())
                {
                    properties["Units"] = value;
                }
                else
                {
                    throw new InvalidOperationException("Units can only be set for analog tags (AI, AO).");
                }
            }
        }
        public double? InitialValue
        {
            get
            {
                if (IsOutputTag())
                {
                    return (double?)properties["InitialValue"];
                }
                else
                {
                    throw new InvalidOperationException("InitialValue can only be set for output tags (AO, DO).");
                }
            }
            set
            {
                if (IsOutputTag())
                {
                    properties["InitialValue"] = value;
                }
                else
                {
                    throw new InvalidOperationException("InitialValue can only be set for output tags (AO, DO).");
                }
            }
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


    }
}
