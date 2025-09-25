
using System.Windows.Controls;
using DataConcentrator;
namespace ScadaGUI.Views
{
    public partial class TagManagementView : UserControl
    {
        public TagManagementView()
        {
            InitializeComponent();
        }

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.DataContext is Tag tag)
            {
                string columnName = e.Column.Header.ToString();

                bool isAnalog = tag.Type == TagType.AI || tag.Type == TagType.AO;
                bool isInput = tag.Type == TagType.AI || tag.Type == TagType.DI;

                switch (columnName)
                {
                    case "Low Limit":
                    case "High Limit":
                    case "Units":
                        if (!isAnalog)
                        {
                            e.Cancel = true;
                        }
                        break;

                    case "ScanTime":
                    case "Scanning On":
                        if (!isInput)
                        {
                            e.Cancel = true;
                        }
                        break;
                }
            }
        }
    }
}