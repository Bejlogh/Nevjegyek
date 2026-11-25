using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Névjegyzék
{
    public partial class LoadingWindow : Window
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Action _onCancelled;

        public bool IsCancelled { get; private set; }
        private Window _owner;

        public LoadingWindow(Action onCancelled)
        {
            InitializeComponent();
            _onCancelled = onCancelled ?? (() => { });
        }

        public async Task ShowLoadingAsync(Func<CancellationToken, Task> openingAction)
        {
            _owner = Application.Current.MainWindow;
            if (_owner != null)
            {
                _owner.IsEnabled = false;
            }

            Owner = _owner;
            Show();

            Task progressTask = Task.Run(async () =>
            {
                long start = Environment.TickCount64;
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    long elapsed = Environment.TickCount64 - start;
                    if (elapsed >= 3000)
                    {
                        break;
                    }

                    await Dispatcher.InvokeAsync(() =>
                    {
                        LoadingProgressBar.Value = elapsed;
                    });

                    await Task.Delay(16, _cancellationTokenSource.Token).ContinueWith(_ => { }, TaskContinuationOptions.None);
                }
            }, _cancellationTokenSource.Token);

            try
            {
                await openingAction(_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // A felhasználó megszakította a betöltést.
            }
            catch
            {
                // A nyitás során fellépő hiba nem akadályozza a betöltőablak bezárását.
            }

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            try
            {
                await progressTask;
            }
            catch (OperationCanceledException)
            {
            }

            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_owner != null)
            {
                _owner.IsEnabled = true;
            }

            base.OnClosed(e);
        }

        public void CancelLoading()
        {
            if (IsCancelled)
            {
                return;
            }

            IsCancelled = true;
            _cancellationTokenSource.Cancel();
            _onCancelled();
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelLoading();
        }
    }
}