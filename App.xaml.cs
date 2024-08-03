using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DocScanForWeb
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly WebSocketServer _webSocketServer;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public App()
        {
            _webSocketServer = new WebSocketServer();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        protected override async void OnStartup(StartupEventArgs evt)
        {
            base.OnStartup(evt);
            await Task.Run(() => _webSocketServer.StartAsync(_cancellationTokenSource.Token));
        }
    }
}
