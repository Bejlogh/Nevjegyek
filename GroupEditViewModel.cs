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

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(GroupName))
            {
                MessageBox.Show("A csoport nevét meg kell adni!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
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