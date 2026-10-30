using Névjegyzék;
using System.Drawing.Printing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using static System.Net.Mime.MediaTypeNames;

namespace Névjegyzék
{
    public class MainViewModel : ViewModelBase
    {
        public ICommand OpenContactsCommand { get; }
        public ICommand OpenNotesCommand { get; }
        public ICommand OpenGroupsCommand { get; }

        public MainViewModel()
        {
            OpenContactsCommand = new RelayCommand(OpenContacts);
            OpenNotesCommand = new RelayCommand(OpenNotes);
            OpenGroupsCommand = new RelayCommand(OpenGroups);
        }

        private void OpenContacts()
        {
            OpenWindowIfNeeded<ContactsWindow>();
        }

        private void OpenNotes()
        {
            OpenWindowIfNeeded<NotesWindow>();
        }

        private void OpenGroups()
        {
            OpenWindowIfNeeded<GroupsWindow>();
        }

        private void OpenWindowIfNeeded<T>() where T : Window, new()
        {
            var window = System.Windows.Application.Current.Windows.OfType<T>().FirstOrDefault() ?? new T();

            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            if (window.IsVisible == false)
            {
                window.Show();
            }

            window.Activate();
            window.Topmost = true;
            window.Topmost = false;
        }
    }
}