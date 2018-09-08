using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace EasyUDP
{
    public class EasyClient
    {
        public delegate void DataReceivedHandler(NetData data);
        public delegate void ConnectionAcceptedHandler();
        public event ConnectionAcceptedHandler ConnectionAcceptedEvent;
        public event DataReceivedHandler DataReceivedEvent;

        UdpClient _client;
        bool _connectedToServer;

        enum Events : byte
        {
            Connect = 1
        }

        public EasyClient()
        {
            _connectedToServer = false;
            _client = new UdpClient();
        }

        public void Connect(string ip, int port, string connectionKey)
        {
            if (!_connectedToServer)
            {
                _client.Connect(ip, port);
                _client.BeginReceive(new AsyncCallback(receiveCallback), null);
                Send((int)Events.Connect, connectionKey);
            }
        }

        public void Send(NetData writer)
        {
            _client.Send(writer.Data, writer.Data.Length);
        }

        public void Send(int protocol, params object[] args)
        {
            NetData writer = new NetData();
            writer.Put(protocol);
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is int)
                {
                    writer.Put((int)args[i]);
                }
                else if (args[i] is uint)
                {
                    writer.Put((uint)args[i]);
                }
                else if (args[i] is double)
                {
                    writer.Put((double)args[i]);
                }
                else if (args[i] is string)
                {
                    writer.Put((string)args[i]);
                }
                else if (args[i] is byte)
                {
                    writer.Put((byte)args[i]);
                }
                else if (args[i] is byte[])
                {
                    writer.Put((byte[])args[i]);
                }
            }
            Send(writer);
        }

        private void receiveCallback(IAsyncResult AR)
        {
            try
            {
                IPEndPoint iPEndPoint = null;
                byte[] data = _client.EndReceive(AR, ref iPEndPoint);
                NetData reader = new NetData(data);
                int cmd = reader.GetInt();
                if (cmd == (int)Events.Connect && !_connectedToServer)
                {
                    int res = reader.GetInt();
                    if (res == 1)
                    {
                        _connectedToServer = true;
                        if (ConnectionAcceptedEvent != null)
                            ConnectionAcceptedEvent();
                        else
                            throw new Exception("ConnectionAcceptedEvent has not been set");
                    }
                }
                else if (_connectedToServer)
                {
                    if (DataReceivedEvent != null)
                        DataReceivedEvent(new NetData(data));
                    else
                        throw new Exception("DataReceivedEvent has not been set");
                }
                _client.BeginReceive(new AsyncCallback(receiveCallback), null);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }
    }
}
