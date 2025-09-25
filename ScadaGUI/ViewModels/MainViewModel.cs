using PLCSimulator;
using ScadaGUI.Services;
using ScadaGUI.ViewModels; 

namespace ScadaGUI.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly PLCSimulatorManager _plc;

        public TagManagementViewModel TagVM { get; }
        public AlarmManagementViewModel AlarmVM { get; }
        public MonitorViewModel MonitorVM { get; }
        public ReportViewModel ReportVM { get; }
        public AlarmHistoryViewModel AlarmHistoryVM { get; }

        public MainViewModel()
        {
            _plc = new PLCSimulatorManager();
            _plc.StartPLCSimulator();

            var db = new MockDatabaseService();
            var concentrator = new MockDataConcentratorService(db, _plc);
            var report = new ReportService(db);

            TagVM = new TagManagementViewModel(db);
            AlarmVM = new AlarmManagementViewModel(db);
            MonitorVM = new MonitorViewModel(concentrator);
            ReportVM = new ReportViewModel(report);
            AlarmHistoryVM = new AlarmHistoryViewModel(concentrator);
        }

        public void Cleanup()
        {
            _plc.Abort();
            (MonitorVM.GetDataConcentrator() as MockDataConcentratorService)?.Stop();
        }
    }
}