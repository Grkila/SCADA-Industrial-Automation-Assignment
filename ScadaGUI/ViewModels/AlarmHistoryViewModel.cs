using System.Collections.ObjectModel;
using ScadaGUI.Services;
using DataConcentrator;
namespace ScadaGUI.ViewModels
{
    public class AlarmHistoryViewModel : BaseViewModel
    {
        // Ova lista  čuva kompletnu istoriju
        public ObservableCollection<ActiveAlarm> AlarmHistory { get; set; }

        public AlarmHistoryViewModel(DataCollector concentrator)
        {
            
            AlarmHistory = new ObservableCollection<ActiveAlarm>();

            // Svaki put kad se desi novi alarm dodamo ga u  istoriju
            concentrator.AlarmTriggered += (newAlarm) =>
            {
                App.Current.Dispatcher.Invoke(() => AlarmHistory.Add(newAlarm));
            };
        }
    }
}