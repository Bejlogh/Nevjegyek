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
            var window = System.Windows.Application.Current.Windows.OfType<ContactsWindow>().FirstOrDefault() ?? new ContactsWindow();
            window.Show();
        }

        private void OpenNotes()
        {
            var window = System.Windows.Application.Current.Windows.OfType<NotesWindow>().FirstOrDefault() ?? new NotesWindow();
            window.Show();
        }

        private void OpenGroups()
        {
            var window = System.Windows.Application.Current.Windows.OfType<GroupsWindow>().FirstOrDefault() ?? new GroupsWindow();
            window.Show();
        }
    }
}