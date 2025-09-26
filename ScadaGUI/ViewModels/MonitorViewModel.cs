
using DataConcentrator;
using ScadaGUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization; 
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media; 

namespace ScadaGUI.ViewModels
{
    public class MonitorViewModel : BaseViewModel, IDisposable
    {
        private readonly DataCollector _concentrator;
        private Tag _selectedTag;
        private string _valueToWrite;
        private bool _isOutputTagSelected;
        private string _statusMessage;
        private Brush _statusMessageColor;

        public ObservableCollection<Tag> MonitoredTags { get; set; }
        public ObservableCollection<ActiveAlarm> ActiveAlarms { get; set; }


        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public Brush StatusMessageColor
        {
            get => _statusMessageColor;
            set { _statusMessageColor = value; OnPropertyChanged(); }
        }

        public string ValueToWrite
        {
            get => _valueToWrite;
            set
            {
                _valueToWrite = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
                StatusMessage = string.Empty;
            }
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
                StatusMessage = string.Empty; 
            }
        }

        public MonitorViewModel(DataCollector concentrator)
        {
            _concentrator = concentrator;
            ActiveAlarms = new ObservableCollection<ActiveAlarm>();
            _concentrator.ValuesUpdated += Concentrator_ValuesUpdated;
            MonitoredTags = new ObservableCollection<Tag>();
            foreach (var tag in _concentrator.GetTags())
            {
                tag.ScanStateChanged += OnTagScanStateChanged; 
                MonitoredTags.Add(tag);
            }

            WriteToTagCommand = new RelayCommand(_ => WriteTagValue(), _ => CanWriteTagValue());
        }

        public void Dispose()
        {
            _concentrator.ValuesUpdated -= Concentrator_ValuesUpdated;
            foreach (var tag in MonitoredTags)
            {
                tag.ScanStateChanged -= OnTagScanStateChanged;
            }
        }

        private void OnTagScanStateChanged(Tag changedTag, bool isScanning)
        {
            try
            { 
                _concentrator.SetTagScanning(changedTag.Name, isScanning);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: Failed to update scan state. {ex.Message}");
            }
        }

        private bool CanWriteTagValue()
        {
            return IsOutputTagSelected && !string.IsNullOrWhiteSpace(ValueToWrite);
        }

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

            if (SelectedTag.Type == TagType.AO)
            {
                if (value < SelectedTag.LowLimit || value > SelectedTag.HighLimit)
                {
                    StatusMessage = $"Error: Value must be between {SelectedTag.LowLimit} and {SelectedTag.HighLimit}.";
                    StatusMessageColor = Brushes.Red;
                    return;
                }
            }

            if (SelectedTag.Type == TagType.DO && value != 0 && value != 1)
            {
                StatusMessage = "Error: Digital Output (DO) tags only accept 0 or 1.";
                StatusMessageColor = Brushes.Red;
                return;
            }

            try
            {
                _concentrator.WriteTagValue(SelectedTag.Name, value);
                StatusMessage = $"Successfully wrote '{value}' to tag '{SelectedTag.Name}'.";
                StatusMessageColor = Brushes.Green;
                ValueToWrite = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageColor = Brushes.Red;
            }
        }

        private void Concentrator_ValuesUpdated(object sender, EventArgs e)
        {
            if (Application.Current?.Dispatcher == null) return;

            Application.Current.Dispatcher.Invoke(() =>
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
            if (Application.Current?.Dispatcher == null) return;
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!MonitoredTags.Any(t => t.Name == tag.Name))
                {
                    tag.ScanStateChanged += OnTagScanStateChanged;
                    MonitoredTags.Add(tag);
                }
            });
        }

        public void HandleTagRemoved(Tag tag)
        {
            if (Application.Current?.Dispatcher == null) return;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var tagToRemove = MonitoredTags.FirstOrDefault(t => t.Name == tag.Name);
                if (tagToRemove != null)
                {
                    tagToRemove.ScanStateChanged -= OnTagScanStateChanged;
                    MonitoredTags.Remove(tagToRemove);
                }
            });
        }

        public DataCollector GetDataConcentrator() => _concentrator;
    }
}