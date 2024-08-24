using System;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using StreamJsonRpc;

namespace docscan4web
{
    internal class WebSocketServer : IDisposable
    {
        public static Dictionary<Guid, Tuple<HttpListenerWebSocketContext, Guid?>> Clients = new Dictionary<Guid, Tuple<HttpListenerWebSocketContext, Guid?>>();
        public readonly string ipaddress = "127.0.0.1";
        public readonly int httpPort = 8181;
        public readonly int httpsPort = 8182;

        private HttpListener _httpListener;
        private CancellationTokenSource _cancellation;

        public WebSocketServer()
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(uriPrefix: $"http://{ipaddress}:{httpPort}/");
            _httpListener.Prefixes.Add(uriPrefix: $"https://{ipaddress}:{httpsPort}/");
            _cancellation = new CancellationTokenSource();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _httpListener.Start();
            Console.WriteLine(string.Format("Server started at ws://{0}:{1}, wss://{0}:{2}", ipaddress, httpPort, httpsPort));
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context = await _httpListener.GetContextAsync();

                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");

                if (context.Request.IsWebSocketRequest)
                {
                    await AcceptClientAsync(context, cancellationToken);
                }
                else
                {
                    context.Response.StatusCode = 204;
                    context.Response.Close();
                }
            }

            _httpListener.Stop();
        }

        private async Task AcceptClientAsync(HttpListenerContext context, CancellationToken cancellation)
        {
            try
            {
                var webSocketContext = await context.AcceptWebSocketAsync(null, new TimeSpan(0, 0, 3));
                Guid connectionId = Guid.NewGuid();
                Clients.Add(connectionId, new Tuple<HttpListenerWebSocketContext, Guid?>(webSocketContext, null));
                Console.WriteLine($"New WebSocket connection established {connectionId}");
                _ = Task.Run(
                    () => HandleConnectionAsync(webSocketContext),
                    CancellationToken.None
                );
            }
            catch (Exception e)
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
                Console.WriteLine("Exception: " + e);
                return;
            }
        }

        private async Task HandleConnectionAsync(HttpListenerWebSocketContext ctx)
        {
            var jsonRpcMessageHandler = new WebSocketMessageHandler(ctx.WebSocket);
            using var jsonRpc = new JsonRpc(jsonRpcMessageHandler, new ScannerService());
            jsonRpc.StartListening();
            await jsonRpc.Completion;
        }

        private async Task HandleConnectionAsyncAlt(WebSocket webSocket, Guid connectionId, CancellationToken cancellation)
        {
             try {
                while (webSocket.State == WebSocketState.Open && !cancellation.IsCancellationRequested)
                {
                    string message = await ReadString(webSocket).ConfigureAwait(false);

                    if (message.Contains("method"))
                    {
                        /*string returnString = await JsonRpcProcessor.Process(message, connectionId);
                        if (returnString.Length != 0)
                        {
                            ArraySegment<byte> outputBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(returnString));
                            if (webSocket.State == WebSocketState.Open)
                            {
                                await webSocket.SendAsync(outputBuffer, WebSocketMessageType.Text, true, cancellation).ConfigureAwait(false);
                            }
                        }*/
                    }
                }
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
            }
            catch (Exception e)
            {
                try
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Done", CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                }
                Console.WriteLine("Exception: " + e);
            }
            finally
            {
                Tuple<HttpListenerWebSocketContext, Guid?> client;
                Clients.TryGetValue(connectionId, out client);
                if (client != null)
                {
                    Clients.Remove(connectionId);
                }
                webSocket.Dispose();
            }
        }

        private static async Task<String> ReadString(WebSocket ws)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new Byte[8192]);

            WebSocketReceiveResult? result;

            using var ms = new MemoryStream();
            do
            {
                result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                ms.Write(buffer.Array, buffer.Offset, result.Count);
            }
            while (!result.EndOfMessage);

            ms.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(ms, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public void Dispose()
        {
            if (_httpListener != null && _cancellation != null)
            {
                try
                {
                    _cancellation.Cancel();
                    _httpListener.Stop();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: " + e);
                }
            }
        }
    }
}
