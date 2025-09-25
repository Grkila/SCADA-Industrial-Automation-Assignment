// In MonitorViewModel.cs
using DataConcentrator;
using ScadaGUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace ScadaGUI.ViewModels
{
    public class MonitorViewModel : BaseViewModel
    {
        private readonly DataCollector _concentrator;
        private Tag _selectedTag;
        private string _valueToWrite;
        private bool _isOutputTagSelected;

        public ObservableCollection<Tag> MonitoredTags { get; set; }
        public ObservableCollection<ActiveAlarm> ActiveAlarms { get; set; }

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

            // This is correct. You are getting a reference to the "live" list of tags
            // that the DataCollector is actively updating.
            MonitoredTags = new ObservableCollection<Tag>(_concentrator.GetTags());

            ActiveAlarms = new ObservableCollection<ActiveAlarm>();

            // Subscribe to the event.
            _concentrator.ValuesUpdated += Concentrator_ValuesUpdated;

            WriteToTagCommand = new RelayCommand(_ => WriteTagValue(), _ => CanWriteTagValue());
        }

        private bool CanWriteTagValue()
        {
            return IsOutputTagSelected && !string.IsNullOrWhiteSpace(ValueToWrite);
        }

        private void WriteTagValue()
        {
            System.Diagnostics.Debug.WriteLine($"FRONTEND: Zahtev za upis vrednosti '{ValueToWrite}' na tag '{SelectedTag.Name}'");
            ValueToWrite = string.Empty;
        }

        // *** THIS IS THE SIMPLIFIED METHOD ***
        private void Concentrator_ValuesUpdated(object sender, EventArgs e)
        {
            // The UI thread is required for updating collections like ActiveAlarms.
            App.Current.Dispatcher.Invoke(() =>
            {
                var currentAlarms = _concentrator.GetActiveAlarms().ToList();
var alarmsToRemove = ActiveAlarms.Where(a => !currentAlarms.Any(ca => ca.Time == a.Time && ca.TagName == a.TagName)).ToList();
                foreach (var alarm in alarmsToRemove)
                {
                    ActiveAlarms.Remove(alarm);
                }

                // Add new alarms
                foreach (var alarm in currentAlarms)
                {
                    if (!ActiveAlarms.Any(a => a.Time == alarm.Time && a.TagName == alarm.TagName))
                    {
                        ActiveAlarms.Add(alarm);
                    }
                }
            });
        }
        public void HandleTagAdded(Tag tag)
        {
            // Ensure UI updates happen on the main thread
            App.Current.Dispatcher.Invoke(() => {
                if (!MonitoredTags.Any(t => t.Name == tag.Name))
                {
                    MonitoredTags.Add(tag);
                }
            });
        }

        // Add this new method to handle the TagRemoved event
        public void HandleTagRemoved(Tag tag)
        {
            // Ensure UI updates happen on the main thread
            App.Current.Dispatcher.Invoke(() => {
                var tagToRemove = MonitoredTags.FirstOrDefault(t => t.Name == tag.Name);
                if (tagToRemove != null)
                {
                    MonitoredTags.Remove(tagToRemove);
                }
            });
        }
        public DataCollector GetDataConcentrator() => _concentrator;
    }
}