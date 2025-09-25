using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DataConcentrator;
using ScadaGUI.Services;
using DataConcentrator;
namespace ScadaGUI.ViewModels
{
    public class TagManagementViewModel : BaseViewModel
    {
        private readonly ContextClass _db;
        private Tag _newTag = new Tag();
        private Tag _selectedTag;
        private string _errorMessage;

        private TagType? _selectedTagTypeForComboBox;
        public TagType? SelectedTagTypeForComboBox
        {
            get => _selectedTagTypeForComboBox;
            set
            {
                _selectedTagTypeForComboBox = value;
                OnPropertyChanged();

                if (value.HasValue)
                {
                    UpdateTagType(value.Value);
                }

                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<Tag> Tags { get; set; }
        public string[] TagTypes => System.Enum.GetNames(typeof(TagType));

        public ICommand AddTagCommand { get; }
        public ICommand DeleteTagCommand { get; }

        public Tag SelectedTag
        {
            get => _selectedTag;
            set
            {
                _selectedTag = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public Tag NewTag
        {
            get => _newTag;
            set { _newTag = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        private void UpdateTagType(TagType newType)
        {
            if (NewTag.Type != newType)
            {
                var name = NewTag.Name;
                var description = NewTag.Description;
                var ioAddress = NewTag.IOAddress;

                NewTag = new Tag { Type = newType }; 
                NewTag.Name = name;
                NewTag.Description = description;
                NewTag.IOAddress = ioAddress;

                OnPropertyChanged(nameof(NewTag));
                OnPropertyChanged(nameof(AnalogFieldsVisibility));
                OnPropertyChanged(nameof(InputFieldsVisibility));
                OnPropertyChanged(nameof(OutputFieldsVisibility));
            }
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
            // Dugme "Add" je aktivno samo ako je uneto ime I ako je izabran tip taga
            return !string.IsNullOrWhiteSpace(NewTag.Name) && _selectedTagTypeForComboBox.HasValue;
        }

        private void ResetForm()
        {
            NewTag = new Tag();
            SelectedTagTypeForComboBox = null; 
            ErrorMessage = "";
            OnPropertyChanged(nameof(NewTag));
        }

        private void AddTag()
        {
           
            if (!CanAddTag())
            {
                ErrorMessage = "Tag Name and Type are required.";
                return;
            }
            if (Tags.Any(t => t.Name == NewTag.Name)) { ErrorMessage = "Tag with that name already exists."; return; }

           
            NewTag.Type = _selectedTagTypeForComboBox.Value;

            _db.AddTag(NewTag);

            var tagToAdd = new Tag();
            tagToAdd.Name = NewTag.Name;
            tagToAdd.Description = NewTag.Description;
            tagToAdd.IOAddress = NewTag.IOAddress;
            tagToAdd.Type = NewTag.Type;
            tagToAdd.LowLimit = NewTag.LowLimit;
            tagToAdd.HighLimit = NewTag.HighLimit;
            tagToAdd.Units = NewTag.Units;
            tagToAdd.ScanTime = NewTag.ScanTime;
            tagToAdd.IsScanning = NewTag.IsScanning;
            tagToAdd.InitialValue = NewTag.InitialValue;
            Tags.Add(tagToAdd);

            ResetForm();
        }

        private void DeleteTag()
        {
            if (_selectedTag != null)
            {
                _db.DeleteTag(_selectedTag);
                Tags.Remove(_selectedTag);
                ResetForm();
            }
        }
    }
}