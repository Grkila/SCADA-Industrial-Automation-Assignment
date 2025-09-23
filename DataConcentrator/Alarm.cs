using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
/*- - - -
 * davanje i uklanjanje alarma nad ulaznim analognim veličinama sa 
sledećim osobinama: 
 vrednost granice veličine, 
 da li se alarm aktivira kada vrednost veličine pređe iznad ili ispod 
vrednosti granice, 
 poruku o alarmu. 
--- - -*/
namespace DataConcentrator
{
    public enum AlarmTrigger
    {
        Above,
        Below
    }

    internal class Alarm
    {
        private const int MAX_ID_LENGTH = 50;
        private const int MAX_MESSAGE_LENGTH = 1000;

        private AlarmTrigger _trigger;
        private double _threshold;
        private string _message;
        private string _id;
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
        public string TagId { get; set; } // ID of the associated Tag (AI)

        public AlarmTrigger Trigger {
            get => _trigger;
            set
            {
                if (!Enum.IsDefined(typeof(AlarmTrigger), value))
                    throw new InvalidOperationException($"Invalid AlarmTrigger value: {value}");
                _trigger = value;
            }
        }

        public double Threshold 
        {
            get
            {
                return _threshold;
            }
            set
            {
                if (double.IsNaN(value))
                    throw new ArgumentException("Threshold cannot be NaN");
                if (double.IsInfinity(value))
                    throw new ArgumentException("Threshold cannot be Infinity");
                _threshold = value;
            }
        }
        
        public string Message 
        { 
            get
            {
                return _message;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException("Message cannot be null or empty.");
                if (value.Length > MAX_MESSAGE_LENGTH)
                    throw new InvalidOperationException($"Message cannot exceed {MAX_MESSAGE_LENGTH} characters.");

                _message = value;
            }
        }

        public bool IsActive { get; set; } // Da li je alarm trenutno aktivan
        public bool IsAcknowledged { get; private set; }
        public DateTime? ActivationTime { get; private set; }

        public Alarm(string id,AlarmTrigger trigger, double threshold, string message)
        {
            Id = id;
            Trigger = trigger;
            Threshold = threshold;
            Message = message;
        }
        public bool CheckTriggerCondition(double currentValue)
        {
            return Trigger == AlarmTrigger.Above ? currentValue > Threshold : currentValue < Threshold;
        }
        public bool TryActivate(double currentValue)
        {
            bool shouldBeActive = CheckTriggerCondition(currentValue);

            if (!IsActive && shouldBeActive)
            {
                IsActive = true;
                IsAcknowledged = false;
                ActivationTime = DateTime.Now;
                return true; // Alarm je upravo aktiviran
            }

            return false;
        }

        // 2. ACKNOWLEDGE - operater potvrđuje da je video alarm
        public bool Acknowledge()
        {
            if (IsActive && !IsAcknowledged)
            {
                IsAcknowledged = true;
                return true;
            }
            return false;
        }

        // 3. RESET - resetuje alarm kada je potvrđen i vrednost je OK
        public bool Reset(double currentValue)
        {
            if (IsActive && IsAcknowledged && !CheckTriggerCondition(currentValue))
            {
                IsActive = false;
                ActivationTime = null;
                return true;
            }
            return false;
        }
    }
}
