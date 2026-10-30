using System.Windows;

namespace Névjegyzék
{
    public partial class NoteEditWindow : Window
    {
        public NoteEditViewModel ViewModel { get; private set; }

        public NoteEditWindow()
        {
            InitializeComponent();
            SetViewModel(new NoteEditViewModel());
        }

        public NoteEditWindow(NoteEditViewModel viewModel)
            : this()
        {
            SetViewModel(viewModel);
        }

        public void SetViewModel(NoteEditViewModel viewModel)
        {
            ViewModel = viewModel ?? new NoteEditViewModel();
            ViewModel.Window = this;
            DataContext = ViewModel;
        }
    }
}


















































































































































