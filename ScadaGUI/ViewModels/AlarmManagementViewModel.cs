using DataConcentrator;
using DataConcentrator;
using ScadaGUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace ScadaGUI.ViewModels
{
    public class AlarmManagementViewModel : BaseViewModel
    {
        private readonly ContextClass _db;
        private Alarm _newAlarm = new Alarm();
        private Alarm _selectedAlarm;

        private AlarmType? _selectedAlarmType;
        public AlarmType? SelectedAlarmType
        {
            get => _selectedAlarmType;
            set { _selectedAlarmType = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        public ObservableCollection<Alarm> Alarms { get; set; }
        public string[] AlarmTypes => System.Enum.GetNames(typeof(AlarmType));
        public ObservableCollection<string> AvailableAiTags { get; set; }

        public ICommand AddAlarmCommand { get; }
        public ICommand DeleteAlarmCommand { get; }

        public Alarm SelectedAlarm
        {
            get => _selectedAlarm;
            set { _selectedAlarm = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        public Alarm NewAlarm
        {
            get => _newAlarm;
            set { _newAlarm = value; OnPropertyChanged(); }
        }

        public AlarmManagementViewModel(ContextClass db)
        {
            _db = db;
            Alarms = new ObservableCollection<Alarm>(_db.GetAlarms());

            var aiTagNames = _db.GetTags()
                                .Where(tag => tag.Type == TagType.AI)
                                .Select(tag => tag.Name);
            AvailableAiTags = new ObservableCollection<string>(aiTagNames);

            AddAlarmCommand = new RelayCommand(_ => AddAlarm(), _ => CanAddAlarm());
            DeleteAlarmCommand = new RelayCommand(_ => DeleteAlarm(), _ => SelectedAlarm != null);
        }

        private bool CanAddAlarm()
        {
            return !string.IsNullOrEmpty(NewAlarm.TagName) &&
                   _selectedAlarmType.HasValue &&
                   !string.IsNullOrWhiteSpace(NewAlarm.Message); // <-- Add this check
        }
        private void AddAlarm()
        {
            // Preuzimamo izabranu vrednost iz ComboBox-a
            NewAlarm.Type = _selectedAlarmType.Value;

            NewAlarm.Id = Guid.NewGuid().ToString();

            _db.AddAlarm(NewAlarm);

            Alarms.Add(NewAlarm);

            // Resetujemo formu
            NewAlarm = new Alarm();
            SelectedAlarmType = null;
        }

        private void DeleteAlarm()
        {
            if (SelectedAlarm != null)
            {
                _db.DeleteAlarm(SelectedAlarm);
                Alarms.Remove(SelectedAlarm);
            }
        }
        public void HandleTagAdded(Tag tag)
        {
            if (tag.Type == TagType.AI && !AvailableAiTags.Contains(tag.Name))
            {
                App.Current.Dispatcher.Invoke(() => AvailableAiTags.Add(tag.Name));
            }
        }

        public void HandleTagRemoved(Tag tag)
        {
            if (tag.Type == TagType.AI && AvailableAiTags.Contains(tag.Name))
            {
                App.Current.Dispatcher.Invoke(() => AvailableAiTags.Remove(tag.Name));
            }
        }
    }
}