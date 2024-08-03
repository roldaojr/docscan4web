using System;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows;

namespace DocScanForWeb
{
    internal class WebSocketServer
    {
        private readonly HttpListener _httpListener;

        static string hostname = "127.0.0.1";
        static int httpPort = 8181;
        static int httpsPort = 8182;

        public WebSocketServer(string? certPath = null, string? certPass = null)
        {
            string scheme = "http";
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(uriPrefix: string.Format("http://{1}:{2}/", scheme, hostname, httpPort));
            if (certPath != null)
            {
                scheme = "https";
                _httpListener.Prefixes.Add(uriPrefix: string.Format("https://{1}:{2}/", scheme, hostname, httpsPort));
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _httpListener.Start();
            Console.WriteLine(string.Format("WebSocket server started at ws://{0}:{1}/", hostname, httpPort));
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context = await _httpListener.GetContextAsync();
                if (context.Request.HttpMethod == "OPTIONS")
                {
                    HandleCorsPreflight(context);
                }
                if (context.Request.IsWebSocketRequest)
                {
                    await ProcessRequest(context);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }

            _httpListener.Stop();
        }

        private void HandleCorsPreflight(HttpListenerContext context)
        {
            context.Response.StatusCode = 200;
            context.Response.AddHeader("Access-Control-Allow-Origin", "*");
            context.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
            context.Response.Close();
        }

        private async Task ProcessRequest(HttpListenerContext context)
        {
            context.Response.AddHeader("Access-Control-Allow-Origin", "*");

            WebSocketContext? webSocketContext;
            try
            {
                webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
                Console.WriteLine("WebSocket connection established");
            }
            catch (Exception e)
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
                Console.WriteLine("Exception: " + e);
                return;
            }

            WebSocket webSocket = webSocketContext.WebSocket;

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    ArraySegment<byte> buffer = new(new byte[1024]);
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }
                    else
                    {
                        string receivedMessage = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                        Console.WriteLine("Received: " + receivedMessage);

                        if (receivedMessage == "1100")
                        {
                            List<byte[]> pages = WIAScanner.Scan();
                            foreach (var page in pages)
                            {
                                await webSocket.SendAsync(page, WebSocketMessageType.Binary, true, CancellationToken.None);
                            }
                        }
                        else
                        {
                            string responseMessage = "Echo: " + receivedMessage;
                            buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(responseMessage));
                            await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e);
            }
            finally
            {
                webSocket.Dispose();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
