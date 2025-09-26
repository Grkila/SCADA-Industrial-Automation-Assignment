using DataConcentrator;
using DataConcentrator;
using ScadaGUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
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
            return !string.IsNullOrWhiteSpace(NewTag.Name) &&
                   _selectedTagTypeForComboBox.HasValue &&
                   !string.IsNullOrWhiteSpace(NewTag.IOAddress);
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
            // 1. Perform validation (your existing code is perfect)
            if (!CanAddTag())
            {
                ErrorMessage = "Tag Name and Type are required.";
                return;
            }
            if (Tags.Any(t => t.Name == NewTag.Name))
            {
                ErrorMessage = "Tag with that name already exists.";
                return;
            }

            // 2. Finalize the NewTag object
            NewTag.Type = _selectedTagTypeForComboBox.Value;

            // 3. Save it to the database
            _db.AddTag(NewTag);


            // 4. Add the *exact same object* to the UI's collection.
            //    This is the key simplification. The UI will update instantly.
            Tags.Add(NewTag);
            TagAdded?.Invoke(NewTag);

            // 5. Reset the form for the next entry. This is a crucial step
            //    as it points NewTag to a new, empty object.
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
                ResetForm();
                TagRemoved?.Invoke(tagToDelete);

                // Optional: Clear selection after deletion
                SelectedTag = null;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}