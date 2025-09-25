using System.Collections.Generic;
using System.Linq;
using ScadaGUI.Models;

namespace ScadaGUI.Services
{
    public class MockDatabaseService
    {
        private readonly List<Tag> _tags = new List<Tag>();
        private readonly List<Alarm> _alarms = new List<Alarm>();

        public MockDatabaseService()
        {
            
            _tags.Add(new Tag
            {
                Name = "SINE_WAVE",
                Type = TagType.AI,
                IOAddress = "ADDR001",
                Description = "Sinusni signal",
                Units = "V",
                ScanTime = 100,
                IsScanning = true,
                LowLimit = -100,
                HighLimit = 100,
                CurrentValue = 0 
            });
            _tags.Add(new Tag
            {
                Name = "RAMP_SIGNAL",
                Type = TagType.AI,
                IOAddress = "ADDR002",
                Description = "Rastući signal",
                Units = "%",
                ScanTime = 100,
                IsScanning = true,
                LowLimit = 10,
                HighLimit = 90,
                CurrentValue = 0 
            });
            _tags.Add(new Tag
            {
                Name = "SWITCH_1",
                Type = TagType.DI,
                IOAddress = "ADDR009",
                Description = "Status prekidača 1",
                ScanTime = 1000,
                IsScanning = true,
                CurrentValue = 0 
            });
            _tags.Add(new Tag
            {
                Name = "MOTOR_SPEED",
                Type = TagType.AO,
                IOAddress = "ADDR005",
                Description = "Brzina motora",
                Units = "RPM",
                InitialValue = 0,
                IsScanning = false,
                LowLimit = 10,
                HighLimit = 90,
                CurrentValue = 0 
            });

            _alarms.Add(new Alarm { TagName = "RAMP_SIGNAL", Type = AlarmType.Above, Limit = 85, Message = "UPOZORENJE: Rampa je dostigla gornju granicu!" });
        }

        public IEnumerable<Tag> GetTags() => _tags.ToList();
        public IEnumerable<Alarm> GetAlarms() => _alarms.ToList();

        public void AddTag(Tag tagFromViewModel)
        {
            var newTag = new Tag
            {
                Name = tagFromViewModel.Name,
                Type = tagFromViewModel.Type,
                IOAddress = tagFromViewModel.IOAddress,
                Description = tagFromViewModel.Description,
                CurrentValue = tagFromViewModel.InitialValue ?? 0 // Osiguravamo da i novi tagovi imaju početnu vrednost
            };
            foreach (var characteristic in tagFromViewModel.Characteristics)
            {
                newTag.Characteristics.Add(characteristic.Key, characteristic.Value);
            }
            _tags.Add(newTag);
        }

        public void DeleteTag(Tag tag) => _tags.Remove(tag);
        public void AddAlarm(Alarm alarm) => _alarms.Add(new Alarm { TagName = alarm.TagName, Type = alarm.Type, Limit = alarm.Limit, Message = alarm.Message });
        public void DeleteAlarm(Alarm alarm) => _alarms.Remove(alarm);
    }
}