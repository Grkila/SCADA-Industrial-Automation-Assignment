using PLCSimulator;
using ScadaGUI.Services;
using ScadaGUI.ViewModels; 
using DataConcentrator;
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

            var db = new ContextClass();
            var concentrator = new DataCollector(db, _plc);
            var report = new ReportService(db);

            TagVM = new TagManagementViewModel(db);
            AlarmVM = new AlarmManagementViewModel(db);
            MonitorVM = new MonitorViewModel(concentrator);
            ReportVM = new ReportViewModel(report);
            AlarmHistoryVM = new AlarmHistoryViewModel(concentrator);

            TagVM.TagAdded += concentrator.OnTagAdded;
            TagVM.TagRemoved += concentrator.OnTagRemoved;

            TagVM.TagAdded += MonitorVM.HandleTagAdded;
            TagVM.TagRemoved += MonitorVM.HandleTagRemoved;

            TagVM.TagAdded += AlarmVM.HandleTagAdded;
            TagVM.TagRemoved += AlarmVM.HandleTagRemoved;

        }

        public void Cleanup()
        {
            _plc.Abort();
            (MonitorVM.GetDataConcentrator() as DataCollector)?.Stop();
        }
    }
}