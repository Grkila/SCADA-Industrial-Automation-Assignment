// In MonitorViewModel.cs
using DataConcentrator;
using ScadaGUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization; // <-- ADD THIS
using System.Linq;
using System.Windows.Input;
using System.Windows.Media; // <-- ADD THIS

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

        // --- START: Properties for User Feedback ---
        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private Brush _statusMessageColor;
        public Brush StatusMessageColor
        {
            get => _statusMessageColor;
            set { _statusMessageColor = value; OnPropertyChanged(); }
        }
        // --- END: Properties for User Feedback ---

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
                StatusMessage = string.Empty; // Clear status when selection changes
            }
        }

        public MonitorViewModel(DataCollector concentrator)
        {
            _concentrator = concentrator;
            ActiveAlarms = new ObservableCollection<ActiveAlarm>();
            _concentrator.ValuesUpdated += Concentrator_ValuesUpdated;

            // Load tags and subscribe to the event for each one
            MonitoredTags = new ObservableCollection<Tag>();
            foreach (var tag in _concentrator.GetTags())
            {
                tag.ScanStateChanged += OnTagScanStateChanged; // Subscribe to the event
                MonitoredTags.Add(tag);
            }

            WriteToTagCommand = new RelayCommand(_ => WriteTagValue(), _ => CanWriteTagValue());
        }

        // This is the event handler that will be called when a checkbox is clicked
        private void OnTagScanStateChanged(Tag changedTag, bool isScanning)
        {
            try
            {
                // Call the method on your DataCollector to persist the change
                _concentrator.SetTagScanning(changedTag.Name, isScanning);
                System.Diagnostics.Debug.WriteLine($"VIEWMODEL: Instructed DataCollector to set scanning for '{changedTag.Name}' to {isScanning}");
            }
            catch (Exception ex)
            {
                // Handle potential errors (e.g., display a message to the user)
                System.Diagnostics.Debug.WriteLine($"ERROR: Failed to update scan state. {ex.Message}");
            }
        }

        private bool CanWriteTagValue()
        {
            // As soon as the user can write, clear any old status messages
            if (IsOutputTagSelected && !string.IsNullOrWhiteSpace(ValueToWrite))
            {
                StatusMessage = string.Empty;
                return true;
            }
            return false;
        }

        // --- REPLACED: The old WriteTagValue method is replaced with this ---
        private void WriteTagValue()
        {
            if (SelectedTag == null)
            {
                StatusMessage = "Error: No tag selected.";
                StatusMessageColor = Brushes.Red;
                return;
            }

            if (!double.TryParse(ValueToWrite, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
            {
                StatusMessage = "Error: Invalid number format.";
                StatusMessageColor = Brushes.Red;
                return;
            }

            if (SelectedTag.Type == TagType.DO && value != 0 && value != 1)
            {
                StatusMessage = "Error: Digital Output (DO) tags only accept 0 or 1.";
                StatusMessageColor = Brushes.Red;
                return;
            }

            try
            {
                // This is the crucial call to the background service
                _concentrator.WriteTagValue(SelectedTag.Name, value);

                StatusMessage = $"Successfully wrote '{value}' to tag '{SelectedTag.Name}'.";
                StatusMessageColor = Brushes.Green;
                ValueToWrite = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageColor = Brushes.Red;
                System.Diagnostics.Debug.WriteLine($"[WRITE FAILED]: {ex.Message}");
            }
        }

        private void Concentrator_ValuesUpdated(object sender, EventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var currentAlarms = _concentrator.GetActiveAlarms().ToList();
                var alarmsToRemove = ActiveAlarms.Where(a => !currentAlarms.Any(ca => ca.Time == a.Time && ca.TagName == a.TagName)).ToList();
                foreach (var alarm in alarmsToRemove)
                {
                    ActiveAlarms.Remove(alarm);
                }
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
            App.Current.Dispatcher.Invoke(() => {
                if (!MonitoredTags.Any(t => t.Name == tag.Name))
                {
                    MonitoredTags.Add(tag);
                }
            });
        }

        public void HandleTagRemoved(Tag tag)
        {
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