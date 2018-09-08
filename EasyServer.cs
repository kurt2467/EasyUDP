using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace EasyUDP
{
    public class EasyServer
    {
        public delegate void DataReceivedHandler(NetClient client, NetData data);
        public delegate void ClientConnectedHandler(NetClient client);
        public delegate void ClientDisconnectedHandler(NetClient client);
        public event DataReceivedHandler DataReceivedEvent;
        public event ClientConnectedHandler ClientConnectedEvent;
        public event ClientDisconnectedHandler ClientDisconnectedEvent;

        private UdpClient _udpServer;
        private int _port;
        private string _connectionKey;
        private NetClient[] _connectedClients;
        private int _clientCount;
        private int _maxClients;
        private Thread _updatePingThread;

        public NetClient[] ConnectedClients { get { return _connectedClients; } }
        public int MaxClients { set { _maxClients = value; } }

        private enum Events : byte
        {
            Connect = 1
        }

        public EasyServer(int port, string connKey, int maxClients = 4)
        {
            _maxClients = maxClients;
            _port = port;
            _connectionKey = connKey;
            _clientCount = 0;
            _connectedClients = new NetClient[_maxClients];
        }
        
        private NetClient GetNetClientFromEndPoint(IPEndPoint endpoint)
        {
            for (int i = 0; i < _connectedClients.Length; i++)
            {
                if (_connectedClients[i] != null)
                {
                    if (_connectedClients[i].EndPoint.Equals(endpoint))
                    {
                        return _connectedClients[i];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        public void Start()
        {
            _udpServer = new UdpClient(_port);
            _udpServer.BeginReceive(new AsyncCallback(receiveCallback), null);
            _updatePingThread = new Thread(UpdateClientPing);
            _updatePingThread.Name = "UpdatePing";
            _updatePingThread.IsBackground = true;
            _updatePingThread.Start();
        }

        private long PingClient(NetClient client)
        {
            Ping ping = new Ping();
            PingReply reply = ping.Send(client.EndPoint.Address, 999);
            return reply.Status == IPStatus.Success ? reply.RoundtripTime : 999;
        }

        private void UpdateClientPing()
        {
            while (true)
            {
                for (int i = 0; i < _connectedClients.Length; i++)
                {
                    if (_connectedClients[i] != null)
                    {
                        NetClient client = _connectedClients[i];
                        long ping = PingClient(client);
                        client.Ping = ping;
                    }
                }
                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Send data to a specified client
        /// </summary>
        /// <param name="client">The client to send to</param>
        /// <param name="data">The data to send</param>
        public void SendToClient(NetClient client, NetData data)
        {
            try
            {
                _udpServer.Send(data.Data, data.Data.Length, client.EndPoint);
            }
            catch
            {
                throw new Exception("An error has occured in SendToClient");
            }
        }

        /// <summary>
        /// Send data to a specific client
        /// </summary>
        /// <param name="client">The client to send to</param>
        /// <param name="protocol">The packet protocol number</param>
        /// <param name="args">Variable arguments to send to the client</param>
        public void SendToClient(NetClient client, int protocol, params object[] args)
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
            SendToClient(client, writer);
        }

        /// <summary>
        /// Send data to all connected clients
        /// </summary>
        /// <param name="data">The data to send</param>
        /// <param name="exclude">A client to exclude if needed</param>
        public void Broadcast(NetData data, NetClient exclude = null)
        {
            foreach (NetClient client in _connectedClients)
                if (client != null)
                    if (!client.Equals(exclude))
                        SendToClient(client, data);
        }

        /// <summary>
        /// Send data to all connected clients
        /// </summary>
        /// <param name="protocol">The packet protocol number</param>
        /// <param name="args">Variable arguments to send to the clients</param>
        public void Broadcast(int protocol, params object[] args)
        {
            foreach (NetClient client in _connectedClients)
                if (client != null)
                    SendToClient(client, protocol, args);
        }

        /// <summary>
        /// Send data to all connected clients
        /// </summary>
        /// <param name="exclude">A client to exclude</param>
        /// <param name="protocol">The packet protocol number</param>
        /// <param name="args">Variable arguments to send to the clients</param>
        public void Broadcast(NetClient exclude, int protocol, params object[] args)
        {
            foreach (NetClient client in _connectedClients)
                if (client != null)
                    if (!client.Equals(exclude))
                        SendToClient(client, protocol, args);
        }

        private int GetOpenClientSlot()
        {
            for (int i = 0; i < _connectedClients.Length; i++)
            {
                if (_connectedClients[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }

        private void AcceptClient(NetClient client)
        {
            if (_clientCount < _maxClients)
            {
                if (!_connectedClients.Contains(client))
                {
                    _connectedClients[GetOpenClientSlot()] = client;
                    _clientCount++;
                    SendToClient(client, (int)Events.Connect, 1);
                    if (ClientConnectedEvent != null)
                        ClientConnectedEvent(client);
                    else
                        throw new Exception("ClientConnectedEvent has not been set");
                }
            }
        }

        private void DeclineClient(NetClient client)
        {
            SendToClient(client, (int)Events.Connect, 2);
        }

        /// <summary>
        /// Remove a client from the server
        /// </summary>
        /// <param name="client">The client to remove</param>
        public void RemoveClient(NetClient client)
        {
            for (int i = 0; i < _connectedClients.Length; i++)
            {
                if (_connectedClients[i].Equals(client))
                {
                    _connectedClients[i] = null;
                    _clientCount--;
                }
            }
        }

        public void RemoveClient(int index)
        {
            _connectedClients[index] = null;
            _clientCount--;
        }
        
        private void receiveCallback(IAsyncResult AR)
        {
            NetClient client = null;
            try
            {
                IPEndPoint fromIP = null;
                byte[] data = _udpServer.EndReceive(AR, ref fromIP);

                client = GetNetClientFromEndPoint(fromIP);
                if (client == null)
                    client = new NetClient(fromIP);

                NetData reader = new NetData(data);
                int cmd = reader.GetInt();
                if (cmd == (int)Events.Connect && !_connectedClients.Contains(client))
                {
                    string key = reader.GetString();
                    if (key == _connectionKey)
                    {
                        AcceptClient(client);
                    }
                    else
                    {
                        DeclineClient(client);
                    }
                }
                else
                {
                    if (DataReceivedEvent != null)
                        DataReceivedEvent(client, new NetData(data));
                    else
                    {
                        throw new Exception("DataReceivedEvent has not been set");
                    }
                }
                _udpServer.BeginReceive(new AsyncCallback(receiveCallback), null);
            }
            catch (Exception ex)
            {
                if (client != null)
                {
                    if (ClientDisconnectedEvent != null)
                        ClientDisconnectedEvent(client);
                }
            }
        }
    }
}
