using System.Windows;

namespace Névjegyzék
{
    public partial class ContactEditWindow : Window
    {
        public ContactEditWindow(ContactEditViewModel viewModel)
        {
            InitializeComponent();
            viewModel.Window = this;
            DataContext = viewModel;
        }
    }
}