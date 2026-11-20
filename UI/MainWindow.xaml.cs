using System.Linq;
using System.Windows;

namespace Névjegyzék
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool anySubPageOpen = Application.Current.Windows
                .Cast<Window>()
                .Any(window => window != this
                    && window.IsVisible
                    && window is (ContactsWindow or NotesWindow or GroupsWindow
                        or ContactEditWindow or NoteEditWindow or GroupEditWindow or LoadingWindow));

            if (anySubPageOpen)
            {
                e.Cancel = true;
            }
        }
    }
}
