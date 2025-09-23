using System;
using System.Collections.Generic;
using System.Linq;
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
        private const int MAX_MESSAGE_LENGTH = 1000;

        private AlarmTrigger _trigger;
        private double _threshold;
        private string _message;

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
        
        Alarm(AlarmTrigger trigger, double threshold, string message)
        {
            Trigger = trigger;
            Threshold = threshold;
            Message = message;
        }
    }
}
