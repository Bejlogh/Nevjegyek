using System.Windows;

namespace Névjegyzék
{
    public partial class GroupEditWindow : Window
    {
        public GroupEditViewModel ViewModel { get; private set; }

        public GroupEditWindow()
        {
            InitializeComponent();
            SetViewModel(new GroupEditViewModel());
        }

        public GroupEditWindow(GroupEditViewModel viewModel)
            : this()
        {
            SetViewModel(viewModel);
        }

        public void SetViewModel(GroupEditViewModel viewModel)
        {
            ViewModel = viewModel ?? new GroupEditViewModel();
            ViewModel.Window = this;
            DataContext = ViewModel;
        }
    }
}










































































































































