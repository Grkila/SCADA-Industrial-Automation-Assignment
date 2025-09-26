using ScadaGUI.Models;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScadaGUI.Views
{
    public partial class TagManagementView : UserControl
    {
        public TagManagementView()
        {
            InitializeComponent();
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            string decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string pattern = $"^[0-9]+({Regex.Escape(decimalSeparator)}[0-9]*)?$";

            TextBox textBox = sender as TextBox;
            string currentText = textBox.Text;
            string futureText = currentText.Insert(textBox.CaretIndex, e.Text);

            if (Regex.IsMatch(futureText, pattern))
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
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