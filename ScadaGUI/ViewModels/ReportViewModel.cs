using System.Windows.Input;
using ScadaGUI.Services;

namespace ScadaGUI.ViewModels
{
    public class ReportViewModel : BaseViewModel
    {
        private readonly ReportService _report;
        public string ReportStatus { get; set; }

        public ICommand GenerateReportCommand { get; }

        public ReportViewModel(ReportService report)
        {
            _report = report;
            GenerateReportCommand = new RelayCommand(_ => GenerateReport());
        }

        private void GenerateReport()
        {
            var path = _report.GenerateReport();
            ReportStatus = $"Report generated: {path}";
            OnPropertyChanged(nameof(ReportStatus));
        }
    }
}
