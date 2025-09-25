using System.Collections.ObjectModel;
using ScadaGUI.Models;
using ScadaGUI.Services;

namespace ScadaGUI.ViewModels
{
    public class AlarmHistoryViewModel : BaseViewModel
    {
        // Ova lista  čuva kompletnu istoriju
        public ObservableCollection<ActiveAlarm> AlarmHistory { get; set; }

        public AlarmHistoryViewModel(MockDataConcentratorService concentrator)
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