using System.Windows;

namespace Névjegyzék
{
    public partial class GroupEditWindow : Window
    {
        public GroupEditWindow(GroupEditViewModel viewModel)
        {
            InitializeComponent();
            viewModel.Window = this;
            DataContext = viewModel;
        }
    }
}