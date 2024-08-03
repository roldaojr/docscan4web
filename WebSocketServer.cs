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
        public readonly string ipaddress = "127.0.0.1";
        public readonly int httpPort = 8181;

        public WebSocketServer()
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(uriPrefix: string.Format("http://{0}:{1}/", ipaddress, httpPort));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _httpListener.Start();
            Console.WriteLine(string.Format("Server started at ws://{0}:{1}", ipaddress, httpPort));
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context = await _httpListener.GetContextAsync();

                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");

                if (context.Request.IsWebSocketRequest)
                {
                    await ProcessRequest(context);
                }
                else
                {
                    context.Response.StatusCode = 204;
                    context.Response.Close();
                }
            }

            _httpListener.Stop();
        }

        private async Task ProcessRequest(HttpListenerContext context)
        {
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
