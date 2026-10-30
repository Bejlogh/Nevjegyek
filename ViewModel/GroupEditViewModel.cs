using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Névjegyzék
{
    public class GroupEditViewModel : ViewModelBase, IDataErrorInfo
    {
        private string _groupName;

        public Window Window { get; set; }
        public bool IsSaved { get; private set; } = false;

        public string GroupName
        {
            get => _groupName;
            set => SetProperty(ref _groupName, value);
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

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public GroupEditViewModel(string groupName = "")
        {
            _groupName = groupName;
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        private bool ValidateFields()
        {
            return string.IsNullOrWhiteSpace(this[nameof(GroupName)]) == false;
        }

        private void Save()
        {
            if (!ValidateFields())
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
    }
}