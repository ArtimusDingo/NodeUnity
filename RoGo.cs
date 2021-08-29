using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using PacketHandling;


namespace ControllerServer
{
    public class RoGo
    {
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

                public ConnectedListener(Socket listener, int _id)
                {
                    Listener = listener;
                    ReadBuffer = new byte[1024];
                    ID = _id;
                }

            }

            private Task<int> GenerateID()
            {
                var _id = new System.Random();
                int val = _id.Next(0, 2000);

                while (ConnectedListeners.ContainsKey(val))
                {
                    val = _id.Next(0, 2000);
                }
                return Task.FromResult(val);
            }

            private async void StartServer()
            {
                IPEndPoint EndPoint = new IPEndPoint(IP, Port);
                MainEntrance = new Socket(IP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    MainEntrance.Bind(EndPoint);
                    MainEntrance.Listen(100);
                    WriteLine($"Server startup was successful, now accepting connections at {EndPoint.ToString()}!");
                    // Main Loop - await a connection and if a listener is valid, start the session
                    while (true)
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
                WriteLine($"Listener created with id {listener.ID.ToString()}");
                WriteLine($"Connection has been attempted from {ListeningSocket.RemoteEndPoint.ToString()}.");
                ConnectedListeners.Add(id, listener);

                return listener;
            }

            private async Task<bool> CheckConnected(Socket Listener)
            {
                bool x;
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

            private Task<Packet> GeneratePacket(byte[] _rawdata)
            {
                byte type = _rawdata[0];
                WriteLine(type.ToString());
                byte[] message = PacketHandler.GetByteSection(_rawdata, _rawdata.Length - 1, 1);
                Packet packet = new Packet(type, message);
                return Task.FromResult(packet);
            }

            private async Task<Packet> ReadAsync(ConnectedListener listener)
            {
                Packet packet = null;
                try
                {
                    int bytesRead = await Task.Run(() => listener.Listener.Receive(listener.ReadBuffer));
                    if (bytesRead > 0)
                    {
                        byte[] message = listener.ReadBuffer;
                        packet = await Task.Run(() => GeneratePacket(message));
                        Array.Clear(listener.ReadBuffer, 0, listener.ReadBuffer.Length);
                        return packet;
                    }
                    else
                    {
                        // Socket is likely dead
                        WriteLine("Connection closed remotely, ditching socket");
                        await Task.Run(() => listener.Listener.Close());
                        listener.Listener = null;

                    }
                }
                catch (SocketException ex)
                {
                    WriteLine(ex.Message);
                    Array.Clear(listener.ReadBuffer, 0, listener.ReadBuffer.Length);
                }
                Array.Clear(listener.ReadBuffer, 0, listener.ReadBuffer.Length);
                return packet;
            }

            private async void RunSession(ConnectedListener Listener)
            {
                WriteLine($"New session started for connection originating from {Listener.Listener.RemoteEndPoint.ToString()}.");

                while (true)
                {
                    if (!CheckConnected(Listener.Listener).Result)
                    {
                        Packet Message = await Task.Run(() => ReadAsync(Listener));
                        try
                        {
                            if (Message.value != null)
                            {
                                try
                                {
                                    WriteLine(Message.value.ToString());
                                    //   if (!task)
                                    //   {
                                    //       Array.Clear(Listener.ReadBuffer, 0, Listener.ReadBuffer.Length);
                                    //       await DisconnectListener(Listener);
                                    //       return;
                                    //   }
                                }
                                catch
                                {
                                    await DisconnectListener(Listener);
                                    return;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await DisconnectListener(Listener);
                                return;
                        }
                    }

                }
            }

            private Task DisconnectListener(ConnectedListener Listener)
            {
                //  CurrentConnections.Remove(Listener.ID);
               Listener.Listener.Close();
                return Task.CompletedTask;
            }

            internal async Task Start()
            {
                StartServer();
                await Task.CompletedTask;
            }

            internal async Task SendAsync(Socket listener, byte[] message)
            {

                await Task.Run(() => listener.Send(message));
            }

        }
    }
}

