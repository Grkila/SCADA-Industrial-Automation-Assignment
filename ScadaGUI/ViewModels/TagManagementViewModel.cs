using DataConcentrator;
using ScadaGUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScadaGUI.ViewModels
{
    public class TagManagementViewModel : BaseViewModel
    {
        private readonly ContextClass _db;
        private Tag _newTag = new Tag();
        private Tag _selectedTag;
        private string _errorMessage;
        public event Action<Tag> TagAdded;
        public event Action<Tag> TagRemoved;

        #region UI-Specific String Properties for Binding

        private string _newTagName;
        public string NewTagName { get => _newTagName; set { _newTagName = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }

        private string _newTagIoAddress;
        public string NewTagIoAddress { get => _newTagIoAddress; set { _newTagIoAddress = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }

        private string _newTagDescription;
        public string NewTagDescription { get => _newTagDescription; set { _newTagDescription = value; OnPropertyChanged(); } }

        private string _newTagScanTime;
        public string NewTagScanTime { get => _newTagScanTime; set { _newTagScanTime = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }

        private string _newTagLowLimit;
        public string NewTagLowLimit { get => _newTagLowLimit; set { _newTagLowLimit = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }

        private string _newTagHighLimit;
        public string NewTagHighLimit { get => _newTagHighLimit; set { _newTagHighLimit = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }

        private string _newTagUnits;
        public string NewTagUnits { get => _newTagUnits; set { _newTagUnits = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }

        private string _newTagInitialValue;
        public string NewTagInitialValue { get => _newTagInitialValue; set { _newTagInitialValue = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); } }

        private bool _newTagIsScanning;
        public bool NewTagIsScanning { get => _newTagIsScanning; set { _newTagIsScanning = value; OnPropertyChanged(); } }

        #endregion

        private TagType? _selectedTagTypeForComboBox;
        public TagType? SelectedTagTypeForComboBox
        {
            get => _selectedTagTypeForComboBox;
            set
            {
                if (_selectedTagTypeForComboBox != value)
                {
                    _selectedTagTypeForComboBox = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                    OnPropertyChanged(nameof(AnalogFieldsVisibility));
                    OnPropertyChanged(nameof(InputFieldsVisibility));
                    OnPropertyChanged(nameof(OutputFieldsVisibility));
                }
            }
        }

        public ObservableCollection<Tag> Tags { get; set; }
        public string[] TagTypes => System.Enum.GetNames(typeof(TagType));

        public ICommand AddTagCommand { get; }
        public ICommand DeleteTagCommand { get; }

        public Tag SelectedTag
        {
            get => _selectedTag;
            set { _selectedTag = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public Visibility AnalogFieldsVisibility => (SelectedTagTypeForComboBox == TagType.AI || SelectedTagTypeForComboBox == TagType.AO) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility InputFieldsVisibility => (SelectedTagTypeForComboBox == TagType.AI || SelectedTagTypeForComboBox == TagType.DI) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility OutputFieldsVisibility => (SelectedTagTypeForComboBox == TagType.AO || SelectedTagTypeForComboBox == TagType.DO) ? Visibility.Visible : Visibility.Collapsed;

        public TagManagementViewModel(ContextClass db)
        {
            _db = db;
            Tags = new ObservableCollection<Tag>(_db.GetTags());
            AddTagCommand = new RelayCommand(_ => AddTag(), _ => CanAddTag());
            DeleteTagCommand = new RelayCommand(_ => DeleteTag(), _ => _selectedTag != null);
        }

        private bool CanAddTag()
        {
            if (string.IsNullOrWhiteSpace(NewTagName) || !_selectedTagTypeForComboBox.HasValue || string.IsNullOrWhiteSpace(NewTagIoAddress))
            {
                return false;
            }

            switch (_selectedTagTypeForComboBox.Value)
            {
                case TagType.AI:
                    return !string.IsNullOrWhiteSpace(NewTagScanTime) && !string.IsNullOrWhiteSpace(NewTagLowLimit) && !string.IsNullOrWhiteSpace(NewTagHighLimit) && !string.IsNullOrWhiteSpace(NewTagUnits);
                case TagType.AO:
                    return !string.IsNullOrWhiteSpace(NewTagInitialValue) && !string.IsNullOrWhiteSpace(NewTagLowLimit) && !string.IsNullOrWhiteSpace(NewTagHighLimit) && !string.IsNullOrWhiteSpace(NewTagUnits);
                case TagType.DI:
                    return !string.IsNullOrWhiteSpace(NewTagScanTime);
                case TagType.DO:
                    return !string.IsNullOrWhiteSpace(NewTagInitialValue);
                default:
                    return false;
            }
        }

        private void ResetForm()
        {
            NewTagName = string.Empty;
            NewTagIoAddress = string.Empty;
            NewTagDescription = string.Empty;
            NewTagScanTime = string.Empty;
            NewTagLowLimit = string.Empty;
            NewTagHighLimit = string.Empty;
            NewTagUnits = string.Empty;
            NewTagInitialValue = string.Empty;
            NewTagIsScanning = false;
            SelectedTagTypeForComboBox = null;
            ErrorMessage = "";
        }

        private void AddTag()
        {
            if (!CanAddTag())
            {
                ErrorMessage = "All required fields must be filled.";
                return;
            }

            if (Tags.Any(t => t.Name == NewTagName))
            {
                ErrorMessage = "Tag with that name already exists.";
                return;
            }
            if (Tags.Any(t => t.IOAddress == NewTagIoAddress))
            {
                ErrorMessage = "Tag with that IO Address already exists.";
                return;
            }

            _newTag = new Tag
            {
                Name = NewTagName,
                IOAddress = NewTagIoAddress,
                Description = NewTagDescription,
                Type = _selectedTagTypeForComboBox.Value
            };

            try
            {
                if (_newTag.Type == TagType.AI || _newTag.Type == TagType.AO)
                {
                    double lowLimit = double.Parse(NewTagLowLimit, CultureInfo.InvariantCulture);
                    double highLimit = double.Parse(NewTagHighLimit, CultureInfo.InvariantCulture);
                    if (lowLimit >= highLimit)
                    {
                        ErrorMessage = "Low Limit must be less than High Limit.";
                        return;
                    }
                    _newTag.LowLimit = lowLimit;
                    _newTag.HighLimit = highLimit;
                    _newTag.Units = NewTagUnits;
                }

                if (_newTag.Type == TagType.AI || _newTag.Type == TagType.DI)
                {
                    double scanTime = double.Parse(NewTagScanTime, CultureInfo.InvariantCulture);
                    if (scanTime <= 0)
                    {
                        ErrorMessage = "Scan Time must be a positive number.";
                        return;
                    }
                    _newTag.ScanTime = scanTime;
                    _newTag.IsScanning = NewTagIsScanning;
                }

                if (_newTag.Type == TagType.AO || _newTag.Type == TagType.DO)
                {
                    _newTag.InitialValue = double.Parse(NewTagInitialValue, CultureInfo.InvariantCulture);
                }
            }
            catch (FormatException)
            {
                ErrorMessage = "Numeric fields contain invalid characters. Use '.' for decimal point.";
                return;
            }

            ErrorMessage = "";

            _db.AddTag(_newTag);
            Tags.Add(_newTag);
            TagAdded?.Invoke(_newTag);

            ResetForm();
            CommandManager.InvalidateRequerySuggested();
        }

        private void DeleteTag()
        {
            if (_selectedTag != null)
            {
                var tagToDelete = _selectedTag;
                _db.DeleteTag(_selectedTag);
                Tags.Remove(_selectedTag);
                TagRemoved?.Invoke(tagToDelete);

                SelectedTag = null;
                ResetForm();
                CommandManager.InvalidateRequerySuggested();
            }
        }
        
    }
}
