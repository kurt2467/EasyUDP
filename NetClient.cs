using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EasyUDP
{
    public class NetClient
    {
        private IPEndPoint _endPoint;
        private long _ping;

        public NetClient()
        {
            _endPoint = new IPEndPoint(0, 0);
            _ping = 0;
        }

        public NetClient(IPEndPoint endpoint)
        {
            _endPoint = endpoint;
            _ping = 0;
        }

        public IPEndPoint EndPoint
        {
            get { return _endPoint; }
            set { _endPoint = value; }
        }

        public long Ping
        {
            get { return _ping; }
            set { _ping = value; }
        }
    }
}
