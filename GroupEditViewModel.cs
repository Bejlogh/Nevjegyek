using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Névjegyzék
{
    public class GroupEditViewModel : ViewModelBase
    {
        private string _groupName;

        public Window Window { get; set; }
        public bool IsSaved { get; private set; } = false;

        public string GroupName
        {
            get => _groupName;
            set => SetProperty(ref _groupName, value);
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
            List<string> errors = new List<string>();

            if (string.IsNullOrWhiteSpace(GroupName))
            {
                errors.Add("A csoport nevét meg kell adni!");
            }

            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join(Environment.NewLine, errors), "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
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