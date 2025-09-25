using System;

namespace ScadaGUI.Models
{
    public enum AlarmType { Above, Below }

    public class Alarm
    {
        public string TagName { get; set; }
        public double Limit { get; set; }
        public AlarmType Type { get; set; }
        public string Message { get; set; }
    }

    public class ActiveAlarm
    {
        public DateTime Time { get; set; }
        public string TagName { get; set; }
        public string Message { get; set; }
    }
}
