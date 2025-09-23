using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataConcentrator
{
    [Table("ActivatedAlarms")]
    public class ActivatedAlarm
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(Alarm.MAX_ID_LENGTH)]
        public string AlarmId { get; set; }

        [Required]
        [StringLength(Tag.MAX_ID_LENGTH)]
        public string TagName { get; set; }

        [Required]
        [StringLength(Alarm.MAX_MESSAGE_LENGTH)]
        public string Message { get; set; }

        [Required]
        public DateTime AlarmTime { get; set; }

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
