using System.Windows;

namespace Névjegyzék
{
    public partial class NoteEditWindow : Window
    {
        public NoteEditWindow(NoteEditViewModel viewModel)
        {
            InitializeComponent();
            viewModel.Window = this;
            DataContext = viewModel;
        }
    }
}