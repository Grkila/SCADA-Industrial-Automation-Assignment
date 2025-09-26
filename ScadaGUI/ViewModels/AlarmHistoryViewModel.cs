using System.Collections.ObjectModel;
using ScadaGUI.Services;
using DataConcentrator;
namespace ScadaGUI.ViewModels
{
    public class AlarmHistoryViewModel : BaseViewModel
    {
        public ObservableCollection<ActiveAlarm> AlarmHistory { get; set; }

        public AlarmHistoryViewModel(DataCollector concentrator)
        {
            
            AlarmHistory = new ObservableCollection<ActiveAlarm>();

            concentrator.AlarmTriggered += (newAlarm) =>
            {
                App.Current.Dispatcher.Invoke(() => AlarmHistory.Add(newAlarm));
            };
        }
    }
}