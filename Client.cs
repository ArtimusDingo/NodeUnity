using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PacketHandling;
using System.Net.Sockets;
using System.Net;

namespace UnityClient
{
    public class Client
    {
        public Socket Connection;
        public byte[] ReadBuffer;

        public Client()
        {
            ReadBuffer = new byte[1024];
        }

        public void Start()
        {
            ConnectAsync();  
        }

        private void ConnectAsync()
        {
            IPEndPoint _ep = new IPEndPoint(IPAddress.Parse("192.168.0.20"), 8888);
            Connection = new Socket(_ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Connection.Connect(_ep);
     
        }

        private async Task<Packet> ReadAsync()
        {
            Packet packet = null;
            try
            {
                int bytesRead = await Task.Run(() => Connection.Receive(ReadBuffer));
                if (bytesRead > 0)
                {
                    byte[] message = PacketHandler.GetByteSection(ReadBuffer, bytesRead - 1, 1);
                    packet = new Packet(ReadBuffer[0], message);
                    Array.Clear(ReadBuffer, 0, ReadBuffer.Length);

                }

            }
            catch (Exception ex)
            {
                Array.Clear(ReadBuffer, 0, ReadBuffer.Length);
    
            }
            return packet;
        }

        private async Task<bool> CheckConnected()
        {
            try
            {
                bool x = await Task.Run(() => Connection.Poll(100, SelectMode.SelectWrite));
                return x;
            }
            catch
            {

            }
            return false;
        }

        public async Task RunSession()
        {
            Console.WriteLine("Session Initiated");
            while (Connection != null)
            {
                if (CheckConnected().Result)
                {
                    Packet packet = await Task.Run(() => ReadAsync());
                        try
                        {
                        Console.WriteLine("Chode");
                            Console.WriteLine(packet.value);
                        }
                        catch (Exception ex)
                        {
                            break;
                        }
                    
                }
                else
                {
                    break;
                }
            }
            return;
        }

        private async Task RunAsync()
        {
            await Task.Run(() => RunSession());
            return;
        }

    }
}
