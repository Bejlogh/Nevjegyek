using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Névjegyzék
{
    public class GroupEditViewModel : ViewModelBase, IDataErrorInfo
    {
        private string _groupName;

        public Window Window { get; set; }
        public bool IsSaved { get; private set; }

        public string GroupName
        {
            get => _groupName;
            set
            {
                if (SetProperty(ref _groupName, value))
                {
                    SaveCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(GroupName) && string.IsNullOrWhiteSpace(GroupName))
                {
                    return "A csoport nevét meg kell adni!";
                }

                return null;
            }
        }

        public RelayCommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public GroupEditViewModel(string groupName = "")
        {
            _groupName = groupName;
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(GroupName);
        }

        private void Save()
        {
            if (!CanSave())
            {
                return;
            }

            IsSaved = true;
            Window?.Close();
        }

        private void Cancel()
        {
            IsSaved = false;
            Window?.Close();
        }

        public void UpdateGroupModel(ContactGroup group)
        {
            if (group == null)
            {
                return;
            }

            group.Name = GroupName;
        }
    }
}
