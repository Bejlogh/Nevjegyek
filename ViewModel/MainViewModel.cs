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
        private string _selectedSyncSource;

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

        public string SelectedSyncSource
        {
            get => _selectedSyncSource;
            set
            {
                string normalizedSource = value == "sqlite" ? "sqlite" : "xml";
                if (SetProperty(ref _selectedSyncSource, normalizedSource))
                {
                    AppConfig.SaveSynchronizationSource(normalizedSource);
                }
            }
        }

        public MainViewModel()
        {
            _selectedLanguageCode = LanguageManager.CurrentLanguageCode;
            AppSettingsData settings = AppConfig.Load();
            _selectedSyncSource = settings.SynchronizationSource == "sqlite" ? "sqlite" : "xml";

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
                if (SelectedSyncSource == "sqlite")
                {
                    if (!SqliteDataManager.DatabaseExists)
                    {
                        throw new InvalidOperationException(LanguageManager.GetString("MainWindow_SyncSqliteMissing"));
                    }

                    XmlFileManager.SynchronizeFromSqlite();
                }
                else
                {
                    XmlFileManager.SynchronizeToSqlite();
                }

                MessageBox.Show(
                    LanguageManager.GetString("MainWindow_SyncSucceeded"),
                    LanguageManager.GetString("MainWindow_SyncTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LanguageManager.GetString("MainWindow_SyncFailed")}\n\n{LanguageManager.GetString("Dialogs_DetailsLabel")} {ex.Message}",
                    LanguageManager.GetString("MainWindow_SyncTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void OpenWindowWithLoading<T>() where T : Window, new()
        {
            T? existingWindow = System.Windows.Application.Current.Windows.OfType<T>().FirstOrDefault();
            if (existingWindow != null)
            {
                if (existingWindow.WindowState == WindowState.Minimized)
                {
                    existingWindow.WindowState = WindowState.Normal;
                }

                existingWindow.Activate();
                return;
            }

            LoadingWindow loadingWindow = new LoadingWindow(() => { });

            await loadingWindow.ShowLoadingAsync(async token =>
            {
                try
                {
                    await Task.Delay(0, token); //delay hosszanak beallitasa
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                if (token.IsCancellationRequested)
                {
                    return;
                }

                T window = new T();
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