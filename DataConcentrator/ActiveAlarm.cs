using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataConcentrator
{
    [Table("ActiveAlarms")]
    public class ActiveAlarm 
    {
        [Key]
        public int Id { get; set; }  // Add primary key

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
        public DateTime Time { get; set; }

        // Navigation property
        public virtual Alarm Alarm { get; set; }

        public ActiveAlarm()
        {
        }

        public ActiveAlarm(string alarmId, string tagName, string message, DateTime alarmTime)
        {
            AlarmId = alarmId;
            TagName = tagName;
            Message = message;
            Time = alarmTime;
        }
        public ActiveAlarm(Alarm alarm, string tagName)
        {
            AlarmId = alarm.Id;
            TagName = tagName;       
            Message = alarm.Message;
            Time = DateTime.Now;
        }
    }
}
