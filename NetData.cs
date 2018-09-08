using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyUDP
{
    public class NetData
    {
        byte[] _data;
        int _position;
        int _size;

        public byte[] Data { get { return _data; } }
        public int Position { get { return _position; } }
        public int Size { get { return _size; } }

        public NetData()
        {
            _size = 64;
            _data = new byte[_size];
            _position = 0;
        }

        public NetData(NetData data)
        {
            _data = data.Data;
            _position = 0;
            _size = _data.Length;
        }

        public NetData(byte[] source)
        {
            _data = source;
            _position = 0;
            _size = _data.Length;
        }

        private void Resize(int len)
        {
            if (_size < len)
            {
                while (_size < len)
                {
                    _size *= 2;
                }
                Array.Resize(ref _data, _size);
            }
        }

        public void Put(int i)
        {
            Resize(_position + 4);
            byte[] bytes = BitConverter.GetBytes(i);
            Array.Copy(bytes, 0, _data, _position, 4);
            _position += 4;
        }

        public void Put(uint u)
        {
            Resize(_position + 4);
            byte[] bytes = BitConverter.GetBytes(u);
            Array.Copy(bytes, 0, _data, _position, 4);
            _position += 4;
        }

        public void Put(double d)
        {
            Resize(_position + 8);
            byte[] bytes = BitConverter.GetBytes(d);
            Array.Copy(bytes, 0, _data, _position, 8);
            _position += 8;
        }

        public void Put(byte b)
        {
            Resize(_position + 1);
            byte[] bytes = BitConverter.GetBytes(b);
            Array.Copy(bytes, 0, _data, _position, 1);
            _position += 1;
        }

        public void Put(string str)
        {
            Resize(_position + 4 + str.Length);
            byte[] bytes = Encoding.ASCII.GetBytes(str);
            Put(str.Length);
            Array.Copy(bytes, 0, _data, _position, str.Length);
            _position += str.Length;
        }
        
        public void Put(byte[] data)
        {
            int len = data.Length;
            Put(len);
            Resize(len);
            Array.Copy(data, 0, _data, _position, len);
            _position += len;
        }

        public void Put(UInt16 u)
        {
            Resize(_position + 2);
            byte[] bytes = BitConverter.GetBytes(u);
            Array.Copy(bytes, 0, _data, _position, 2);
            _position += 2;
        }

        public UInt16 GetUInt16()
        {
            byte[] bytes = new byte[2];
            Array.Copy(_data, _position, bytes, 0, 2);
            _position += 2;
            return BitConverter.ToUInt16(bytes, 0);
        }

        public int GetInt()
        {
            byte[] bytes = new byte[4];
            Array.Copy(_data, _position, bytes, 0, 4);
            _position += 4;
            return BitConverter.ToInt32(bytes, 0);
        }

        public uint GetUInt()
        {
            byte[] bytes = new byte[4];
            Array.Copy(_data, _position, bytes, 0, 4);
            _position += 4;
            return BitConverter.ToUInt32(bytes, 0);
        }

        public double GetDouble()
        {
            byte[] bytes = new byte[8];
            Array.Copy(_data, _position, bytes, 0, 8);
            _position += 8;
            return BitConverter.ToDouble(bytes, 0);
        }

        public byte GetByte()
        {
            byte[] bytes = new byte[1];
            Array.Copy(_data, _position, bytes, 0, 1);
            _position += 1;
            return bytes[0];
        }

        public byte[] GetBytes()
        {
            int len = GetInt();
            byte[] data = new byte[len];
            Array.Copy(_data, _position, data, 0, len);
            _position += len;
            return data;
        }

        public string GetString()
        {
            int len = GetInt();
            byte[] strBytes = new byte[len];
            Array.Copy(_data, _position, strBytes, 0, strBytes.Length);
            _position += strBytes.Length;
            return Encoding.ASCII.GetString(strBytes);
        }
    }
}
