using System.IO;
using System.Windows;

namespace Névjegyzék
{
    public partial class ContactEditWindow : Window
    {
        public ContactEditViewModel ViewModel { get; private set; }

        public ContactEditWindow()
        {
            InitializeComponent();
            SetViewModel(new ContactEditViewModel());
        }

        public ContactEditWindow(ContactEditViewModel viewModel)
            : this()
        {
            SetViewModel(viewModel);
        }

        public void SetViewModel(ContactEditViewModel viewModel)
        {
            ViewModel = viewModel ?? new ContactEditViewModel();
            ViewModel.Window = this;
            DataContext = ViewModel;
        }

        private void ProfileImageBorder_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0 && ViewModel != null && ViewModel.IsSupportedImageFile(files[0]))
                {
                    e.Effects = DragDropEffects.Copy;
                    e.Handled = true;
                    return;
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void ProfileImageBorder_DragOver(object sender, DragEventArgs e)
        {
            ProfileImageBorder_DragEnter(sender, e);
        }

        private void ProfileImageBorder_Drop(object sender, DragEventArgs e)
        {
            if (ViewModel == null || !e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0)
            {
                return;
            }

            var filePath = files[0];
            if (!ViewModel.IsSupportedImageFile(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("Kérjük, kép fájlt húzzon ide. Engedélyezett formátumok: .jpg, .jpeg, .png, .bmp.", "Érvénytelen fájl", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Handled = true;
                return;
            }

            ViewModel.UploadImageFromFile(filePath);
            e.Handled = true;
        }
    }
}































































































































