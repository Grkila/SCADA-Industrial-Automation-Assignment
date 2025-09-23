using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConcentrator
{
    internal class ActivatedAlarm
    {

        public string AlarmId { get; set; }          
        public string TagName { get; set; }          
        public string Message { get; set; }          
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
