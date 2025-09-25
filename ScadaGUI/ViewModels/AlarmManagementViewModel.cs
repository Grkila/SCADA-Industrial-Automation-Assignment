using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ScadaGUI.Services;
using DataConcentrator;
namespace ScadaGUI.ViewModels
{
    public class AlarmManagementViewModel : BaseViewModel
    {
        private readonly ContextClass _db;
        private Alarm _newAlarm = new Alarm();
        private Alarm _selectedAlarm;

        private AlarmTrigger? _selectedAlarmTrigger;
        public AlarmTrigger? SelectedAlarmTrigger
        {
            get => _selectedAlarmTrigger;
            set { _selectedAlarmTrigger = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        public ObservableCollection<Alarm> Alarms { get; set; }
        public string[] AlarmTypes => System.Enum.GetNames(typeof(AlarmTrigger));
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
                                .Select(tag => tag.Id);
            AvailableAiTags = new ObservableCollection<string>(aiTagNames);

            AddAlarmCommand = new RelayCommand(_ => AddAlarm(), _ => CanAddAlarm());
            DeleteAlarmCommand = new RelayCommand(_ => DeleteAlarm(), _ => SelectedAlarm != null);
        }

        private bool CanAddAlarm()
        {
            return !string.IsNullOrEmpty(NewAlarm.TagId) && _selectedAlarmTrigger.HasValue;
        }

        private void AddAlarm()
        {
            // Preuzimamo izabranu vrednost iz ComboBox-a
            NewAlarm.Trigger = _selectedAlarmTrigger.Value;

            _db.AddAlarm(NewAlarm);
            Alarms.Add(NewAlarm);

            // Resetujemo formu
            NewAlarm = new Alarm();
            SelectedAlarmTrigger = null; // Resetujemo ComboBox na "ništa selektovano"
        }

        private void DeleteAlarm()
        {
            if (SelectedAlarm != null)
            {
                _db.DeleteAlarm(SelectedAlarm);
                Alarms.Remove(SelectedAlarm);
            }
        }
    }
}