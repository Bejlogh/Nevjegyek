using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Névjegyzék
{
    public class MainViewModel : ViewModelBase
    {
        private string _selectedLanguageCode;

        public ICommand OpenContactsCommand { get; }
        public ICommand OpenNotesCommand { get; }
        public ICommand OpenGroupsCommand { get; }
        public ICommand SynchronizeAllCommand { get; }

        public ObservableCollection<KeyValuePair<string, string>> AvailableLanguages { get; } =
            new(LanguageManager.SupportedLanguages.Select(x => new KeyValuePair<string, string>(x.Key, x.Value)));

        public string SelectedLanguageCode
        {
            get => _selectedLanguageCode;
            set
            {
                if (SetProperty(ref _selectedLanguageCode, value))
                {
                    LanguageManager.ApplyLanguage(value);
                }
            }
        }

        public MainViewModel()
        {
            _selectedLanguageCode = LanguageManager.CurrentLanguageCode;

            OpenContactsCommand = new RelayCommand(OpenContacts);
            OpenNotesCommand = new RelayCommand(OpenNotes);
            OpenGroupsCommand = new RelayCommand(OpenGroups);
            SynchronizeAllCommand = new RelayCommand(SynchronizeAll);
        }

        private void OpenContacts()
        {
            OpenWindowWithLoading<ContactsWindow>();
        }

        private void OpenNotes()
        {
            OpenWindowWithLoading<NotesWindow>();
        }

        private void OpenGroups()
        {
            OpenWindowWithLoading<GroupsWindow>();
        }

        private void SynchronizeAll()
        {
            try
            {
                XmlFileManager.SynchronizeToSqlite();
                MessageBox.Show(
                    Application.Current.TryFindResource("MainWindow_SyncSucceeded") as string
                        ?? "A szinkronizálás sikeres.",
                    Application.Current.TryFindResource("MainWindow_SyncTitle") as string
                        ?? "Szinkronizálás",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{Application.Current.TryFindResource("MainWindow_SyncFailed") as string ?? "A szinkronizálás sikertelen."}\n\n{ex.Message}",
                    Application.Current.TryFindResource("MainWindow_SyncTitle") as string
                        ?? "Szinkronizálás",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void OpenWindowWithLoading<T>() where T : Window, new()
        {
            var existingWindow = System.Windows.Application.Current.Windows.OfType<T>().FirstOrDefault();
            if (existingWindow != null)
            {
                if (existingWindow.WindowState == WindowState.Minimized)
                {
                    existingWindow.WindowState = WindowState.Normal;
                }

                existingWindow.Activate();
                return;
            }

            var loadingWindow = new LoadingWindow(() => { });

            await loadingWindow.ShowLoadingAsync(async token =>
            {
                try
                {
                    await Task.Delay(3000, token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                if (token.IsCancellationRequested)
                {
                    return;
                }

                var window = new T();
                window.Owner = Application.Current.MainWindow;
                if (window.WindowState == WindowState.Minimized)
                {
                    window.WindowState = WindowState.Normal;
                }

                window.Show();
                window.Activate();
                window.Topmost = true;
                window.Topmost = false;
            });
        }
    }
}