using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using PLCSimulator;
using ScadaGUI.Models;

namespace ScadaGUI.Services
{
    public class MockDataConcentratorService
    {
        private readonly MockDatabaseService _db;
        private readonly PLCSimulatorManager _plc;
        private readonly List<ActiveAlarm> _activeAlarms = new List<ActiveAlarm>();
        private readonly Timer _timer;

        public event EventHandler ValuesUpdated;
        public event Action<ActiveAlarm> AlarmTriggered;

        public MockDataConcentratorService(MockDatabaseService db, PLCSimulatorManager plc)
        {
            _db = db;
            _plc = plc;
            _timer = new Timer(500);
            _timer.Elapsed += (s, e) => ReadValuesFromPLC();
            _timer.Start();
        }

        private void ReadValuesFromPLC()
        {
            foreach (var tag in _db.GetTags())
            {
                if (tag.IsScanning == true)
                {
                    
                    tag.CurrentValue = _plc.GetAnalogValue(tag.IOAddress);
                }

                if (tag.CurrentValue.HasValue)
                {
                    bool isAlarmActiveForTag = false;
                    foreach (var alarm in _db.GetAlarms().Where(a => a.TagName == tag.Name))
                    {
                        
                        if ((alarm.Type == AlarmType.Above && tag.CurrentValue.Value > alarm.Limit) ||
                            (alarm.Type == AlarmType.Below && tag.CurrentValue.Value < alarm.Limit))
                        {
                            isAlarmActiveForTag = true;
                            if (!_activeAlarms.Any(a => a.TagName == tag.Name && a.Message == alarm.Message))
                            {
                                var newActiveAlarm = new ActiveAlarm
                                {
                                    Time = DateTime.Now,
                                    TagName = tag.Name,
                                    Message = alarm.Message
                                };
                                _activeAlarms.Add(newActiveAlarm);
                                AlarmTriggered?.Invoke(newActiveAlarm);
                            }
                        }
                    }

                    if (!isAlarmActiveForTag)
                    {
                        var alarmsToRemove = _activeAlarms.Where(a => a.TagName == tag.Name).ToList();
                        foreach (var alarm in alarmsToRemove)
                        {
                            _activeAlarms.Remove(alarm);
                        }
                    }
                }
            }

            ValuesUpdated?.Invoke(this, EventArgs.Empty);
        }

        public IEnumerable<Tag> GetTags() => _db.GetTags();
        public IEnumerable<ActiveAlarm> GetActiveAlarms() => _activeAlarms;

        public void Stop()
        {
            _timer.Stop();
            _timer.Dispose();
        }
    }
}