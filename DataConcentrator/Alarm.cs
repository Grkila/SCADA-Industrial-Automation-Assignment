using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataConcentrator
{
    public enum AlarmType
    {
        Above,
        Below
    }

    [Table("Alarms")]
    public class Alarm
    {
        public const int MAX_ID_LENGTH = 50;
        public const int MAX_MESSAGE_LENGTH = 1000;

        [Key]
        [StringLength(MAX_ID_LENGTH)]
        public string Id { get; set; }

        [Required]
        [StringLength(MAX_ID_LENGTH)]
        [ForeignKey("Tag")]
        public string TagName { get; set; }

        [Required]
        public AlarmType Type { get; set; }

        [Required]
        public double Limit { get; set; }

        [Required]
        [StringLength(MAX_MESSAGE_LENGTH)]
        public string Message { get; set; }

        public bool IsActive { get; set; }
        public bool IsAcknowledged { get; set; }
        public DateTime? ActivationTime { get; set; }

        // Navigation property
        public virtual Tag Tag { get; set; }

        public Alarm()
        {
        }

        public Alarm(string id, AlarmType trigger, double threshold, string message)
        {
            ValidateAndSetId(id);
            ValidateAndSetTrigger(trigger);
            ValidateAndSetThreshold(threshold);
            ValidateAndSetMessage(message);
        }

        public void ValidateAndSetId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Id cannot be null or empty.");
            if (value.Length > MAX_ID_LENGTH)
                throw new InvalidOperationException($"Id cannot exceed {MAX_ID_LENGTH} characters.");
            Id = value;
        }

        public void ValidateAndSetTrigger(AlarmType value)
        {
            if (!Enum.IsDefined(typeof(AlarmType), value))
                throw new InvalidOperationException($"Invalid AlarmType value: {value}");
            Type = value;
        }

        public void ValidateAndSetThreshold(double value)
        {
            if (double.IsNaN(value))
                throw new ArgumentException("Threshold cannot be NaN");
            if (double.IsInfinity(value))
                throw new ArgumentException("Threshold cannot be Infinity");
            Limit = value;
        }

        public void ValidateAndSetMessage(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Message cannot be null or empty.");
            if (value.Length > MAX_MESSAGE_LENGTH)
                throw new InvalidOperationException($"Message cannot exceed {MAX_MESSAGE_LENGTH} characters.");
            Message = value;
        }

        public bool CheckTriggerCondition(double currentValue)
        {
            return (Type==AlarmType.Above) ? currentValue > Limit : currentValue < Limit;
        }

        public bool TryActivate(double currentValue)
        {
            bool shouldBeActive = CheckTriggerCondition(currentValue);

            if (!IsActive && shouldBeActive)
            {
                IsActive = true;
                IsAcknowledged = false;
                ActivationTime = DateTime.Now;
                return true; 
            }

            return false;
        }

        public bool Acknowledge()
        {
            if (IsActive && !IsAcknowledged)
            {
                IsAcknowledged = true;
                return true;
            }
            return false;
        }

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

        public void ValidateConfiguration()
        {
            ValidateAndSetId(Id);
            ValidateAndSetTrigger(Type);
            ValidateAndSetThreshold(Limit);
            ValidateAndSetMessage(Message);
        }
    }
}
