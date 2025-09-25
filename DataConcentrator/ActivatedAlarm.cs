using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataConcentrator
{
    [Table("ActivatedAlarms")]
    public class ActivatedAlarm
    {
        [Key]
        public string Id { get; set; }  // Add primary key

        [Required]
        [StringLength(50)]
        [ForeignKey("Alarm")]
        public string AlarmId { get; set; }

        [Required]
        [StringLength(50)]
        public string TagName { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; }

        [Required]
        public DateTime AlarmTime { get; set; }

        // Navigation property
        public virtual Alarm Alarm { get; set; }

        public ActivatedAlarm()
        {
        }

        public ActivatedAlarm(string alarmId, string tagName, string message, DateTime alarmTime)
        {
            AlarmId = alarmId;
            TagName = tagName;
            Message = message;
            AlarmTime = alarmTime;
        }
        public ActivatedAlarm(Alarm alarm, string tagName)
        {
            AlarmId = alarm.Id;
            TagName = tagName;       
            Message = alarm.Message;
            AlarmTime = DateTime.Now;
        }
    }
}
