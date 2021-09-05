using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using PacketHandling;
using System.Threading;


namespace ControllerServer
{
    public class RoGo
    {
        #region Public

        public RoGoServer Server { get; private set; }

        public RoGo(int _port, IPAddress _ip, int _maxcon)
        {
            Server = new RoGoServer(_port, _ip, _maxcon);
        }

        public async Task<bool> Start()
        {
            await Server.Start();
            return true;
        }

        public static void WriteLine(string msg)
        {
            Console.WriteLine(FormatConsoleMessage(msg));
        }

        public async Task Send(int ID, byte[] _message)
        {
            await Server.SendAsync(Server.ConnectedListeners[ID].Listener, _message);
        }

        #endregion

        private static string FormatConsoleMessage(string msg)
        {
            var FormatMsg = $"[{DateTime.Now.ToString()}]: {msg}";
            return FormatMsg;
        }

        public class RoGoServer
        {
            public Socket MainEntrance { get; private set; } // listens for incoming connections only
            public int Port { get; private set; }
            public IPAddress IP { get; private set; }
            public int MaxConnections { get; private set; }
            internal Dictionary<int, ConnectedListener> ConnectedListeners = new Dictionary<int, ConnectedListener>();
            private bool Run = true;
            private string authkey = "D(G+KaPdSgVkYp3s6v9y$B&E)H@McQfT";
            public RoGoServer(int _port, IPAddress _ip, int _maxconnections)
            {
                Port = _port;
                IP = _ip;
                MaxConnections = _maxconnections;
            }

            internal class ConnectedListener
            {
                public Socket Listener { get; set; }
                public byte[] ReadBuffer { get; set; }
                public int ID { get; private set; }
                public string name { get; private set; }
                public DateTime ConnectTime { get; set; }
                internal bool Auth { get; set; }
                internal bool Run { get; set; }
                internal Task ReadTask { get; set; }
                internal CancellationTokenSource ReadCancel { get; set; }

                public ConnectedListener(Socket listener, int _id)
                {
                    Listener = listener;
                    ReadBuffer = new byte[1024];
                    ID = _id;
                    Auth = false;
                    Run = true;
                    ReadCancel = new CancellationTokenSource();
                }


            }
            private Task DumpConnect(ConnectedListener listener)
            {
                TimeSpan Connected;
                while (!listener.Auth && listener.Run)
                {
                    Connected = DateTime.Now - listener.ConnectTime;
                    if ((Connected > TimeSpan.FromSeconds(10.0)) && !listener.Auth)
                    {
                        listener.ReadCancel.Cancel();
                        break;
                    }
                }
                return Task.CompletedTask;
            }

            private Task<bool> AuthConnection(string password)
            {
                return Task.FromResult(Equals(password, authkey));
            }

            private Task<int> GenerateID()
            {
                var _id = new System.Random();
                int val = _id.Next(0, MaxConnections);

                while (ConnectedListeners.ContainsKey(val))
                {
                    val = _id.Next(0, MaxConnections);
                }
                return Task.FromResult(val);
            }

            private async Task StartServer()
            {
                IPEndPoint EndPoint = new IPEndPoint(IP, Port);
                MainEntrance = new Socket(IP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    MainEntrance.Bind(EndPoint);
                    MainEntrance.Listen(100);
                    WriteLine($"Server startup was successful, now accepting connections at {EndPoint.ToString()}");
                    while (Run)
                    {
                        ConnectedListener Listener = await Task.Run(() => AcceptConnection());
                        if (Listener != null)
                        {
                            await Task.Run(() => RunSession(Listener));
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLine($"Start Error: {ex.Message}");
                }
                finally
                {

                }
                WriteLine("Server Failed: Press enter to shutdown");
                Console.ReadLine();
            }

            private async Task<ConnectedListener> AcceptConnection()
            {
                Socket ListeningSocket = await Task.Run(() => MainEntrance.Accept());
                int id = await GenerateID();
                ConnectedListener listener = new ConnectedListener(ListeningSocket, id);
                listener.ConnectTime = DateTime.Now;
                WriteLine($"Connection has been attempted from {ListeningSocket.RemoteEndPoint.ToString()}.");
                ConnectedListeners.Add(id, listener);
                return listener;
            }

            private async Task<bool> CheckConnected(Socket Listener)
            {
                bool x = true;
                try
                {          
                    if (Listener != null)
                    {
                        x = await Task.Run(() => Listener.Poll(100, SelectMode.SelectRead));
                    }
                    else
                    {
                        x = true;
                    }
                    return x;
                }
                catch(ObjectDisposedException ex)
                {
                    WriteLine($"CheckConnected has thrown an exception: {ex.Message}");
                    return true;
                }
                
            }

            private async Task<Packet> ReadAsync(ConnectedListener Listener)
            {
                Packet packet = null;
                try
                {
                    if (!CheckConnected(Listener.Listener).Result)
                    {
                        int bytesRead = await Task.Run(() => Listener.Listener.Receive(Listener.ReadBuffer)).WaitOrCancel(Listener.ReadCancel.Token);
                        if (Listener.ReadCancel.IsCancellationRequested)
                        {
                            return packet = null;
                        }
                        if (bytesRead > 0)
                        {
                            byte[] message = Listener.ReadBuffer;
                            packet = await Task.Run(() => PacketHandler.GeneratePacket(message, bytesRead));
                            Array.Clear(Listener.ReadBuffer, 0, Listener.ReadBuffer.Length);
                            return packet;
                        }
                        else
                        {
                            WriteLine($"Listener {Listener.ID.ToString()} connection closed from user. Cleaning up...");
                        }
                    }
                    return packet;
                }
                catch
                {
                    Array.Clear(Listener.ReadBuffer, 0, Listener.ReadBuffer.Length);
                    return packet;
                }
            }

            private async void RunSession(ConnectedListener Listener)
            {          
                try
                {
                    Task.Run(() => DumpConnect(Listener)); // Runs the check to dump connection protocol
                    while (!Listener.Auth && Listener.Run)
                    {
                        Packet AuthPacket = await Task.Run(() => ReadAsync(Listener));
                        if (AuthPacket == null)
                        {
                            WriteLine($"Null packet generated for an unauthenticated connection. Either authorization has timed out or an exception has occured. Ending session...");
                            break;
                        }
                        if(AuthConnection(AuthPacket.value.ToString()).Result)
                        {
                            WriteLine($"Connected listener with ID {Listener.ID.ToString()} has been authorized.");
                            Listener.Auth = true;
                        }
                    }
                    if (ConnectedListeners.ContainsValue(Listener) && Listener.Auth)
                    {
                        WriteLine($"New session started for connection originating from {Listener.Listener.RemoteEndPoint.ToString()}.");
                        while (Listener.Run)
                            try
                            {
                                Packet Message = await Task.Run(() => ReadAsync(Listener));
                                WriteLine($"CONNECTION ID {Listener.ID.ToString()}: {Message.value.ToString()}");                    
                            }
                            catch
                            {
                                WriteLine($"Authorized Listener with ID {Listener.ID.ToString()} gone link-dead. Cleaning up...");         
                                break;
                            }
                    }
                }
                catch
                {
                    await DisconnectListener(Listener);
                }
                await DisconnectListener(Listener);
            }

            private Task DisconnectListener(ConnectedListener Listener)
            {
                string ep = Listener.Listener.RemoteEndPoint.ToString();
                try
                {
                    Listener.Listener.Close();
                    ConnectedListeners.Remove(Listener.ID);
                }
                catch (Exception ex)
                {
                    WriteLine($"DisconnectListener has thrown an exception: {ex.Message}");
                }
                WriteLine($"Disconnection complete. Session has ended for {ep}.");
                return Task.CompletedTask;
            }

            internal async Task Start()
            {
                await Task.Run(() => StartServer());
            }

            internal async Task SendAsync(Socket listener, byte[] message)
            {
                await Task.Run(() => listener.Send(message));
            }
        }
    }

    #region Support

    public static class TaskExtensions
    {
        public static async Task<T> WaitOrCancel<T>(this Task<T> task, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await Task.WhenAny(task, token.WhenCanceled());
            token.ThrowIfCancellationRequested();

            return await task;
        }

        public static Task WhenCanceled(this CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }
    }

    public class AuthTimeOutException : Exception
    {
        public AuthTimeOutException()
        {
        }

        public AuthTimeOutException(string message)
            : base(message)
        {
        }

        public AuthTimeOutException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    #endregion
}

