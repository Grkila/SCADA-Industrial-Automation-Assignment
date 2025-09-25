using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ScadaGUI.Services;
using DataConcentrator;
namespace ScadaGUI.ViewModels
{
    public class MonitorViewModel : BaseViewModel
    {
        private readonly DataCollector _concentrator;
        private Tag _selectedTag;
        private string _valueToWrite;
        private bool _isOutputTagSelected;

        public ObservableCollection<Tag> MonitoredTags { get; set; }
        public ObservableCollection<ActivatedAlarm> ActiveAlarms { get; set; }

        public string ValueToWrite
        {
            get => _valueToWrite;
            set { _valueToWrite = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        public bool IsOutputTagSelected
        {
            get => _isOutputTagSelected;
            set { _isOutputTagSelected = value; OnPropertyChanged(); }
        }

        public ICommand WriteToTagCommand { get; }

        public Tag SelectedTag
        {
            get => _selectedTag;
            set
            {
                _selectedTag = value;
                OnPropertyChanged();

                IsOutputTagSelected = _selectedTag != null && (_selectedTag.Type == TagType.AO || _selectedTag.Type == TagType.DO);

                ValueToWrite = string.Empty;
            }
        }

        public MonitorViewModel(DataCollector concentrator)
        {
            _concentrator = concentrator;
            MonitoredTags = new ObservableCollection<Tag>(_concentrator.GetTags());
            ActiveAlarms = new ObservableCollection<ActivatedAlarm>();
            _concentrator.ValuesUpdated += Concentrator_ValuesUpdated;

            WriteToTagCommand = new RelayCommand(_ => WriteTagValue(), _ => CanWriteTagValue());
        }

        private bool CanWriteTagValue()
        {
            return IsOutputTagSelected && !string.IsNullOrWhiteSpace(ValueToWrite);
        }

        private void WriteTagValue()
        {

            System.Diagnostics.Debug.WriteLine($"FRONTEND: Zahtev za upis vrednosti '{ValueToWrite}' na tag '{SelectedTag.Id}'");
            ValueToWrite = string.Empty;
        }

        private void Concentrator_ValuesUpdated(object sender, EventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var updatedTags = _concentrator.GetTags();
                foreach (var updatedTag in updatedTags)
                {
                    var tagToUpdate = MonitoredTags.FirstOrDefault(t => t.Id == updatedTag.Id);
                    if (tagToUpdate != null)
                    {
                        // Ažuriramo 'CurrentValue', što automatski osvežava prikaz u tabeli
                        tagToUpdate.CurrentValue = updatedTag.CurrentValue;
                    }
                }

                //AŽURIRANJE ALARMA 
                var currentAlarms = _concentrator.GetActiveAlarms().ToList();
                var alarmsToRemove = ActiveAlarms.Where(a => !currentAlarms.Any(ca => ca.AlarmTime == a.AlarmTime)).ToList();
                foreach (var alarm in alarmsToRemove) ActiveAlarms.Remove(alarm);

                foreach (var alarm in currentAlarms)
                {
                    if (!ActiveAlarms.Any(a => a.AlarmTime == alarm.AlarmTime && a.TagName == alarm.TagName))
                    {
                        ActiveAlarms.Add(alarm);
                    }
                }
            });
        }

        public DataCollector GetDataConcentrator() => _concentrator;
    }
}