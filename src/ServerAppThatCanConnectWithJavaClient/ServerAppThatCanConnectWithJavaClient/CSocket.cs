using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerAppThatCanConnectWithJavaClient
{
    class CSocket
    {
        Socket _client;

        public CSocket(Socket client)
        {
            _client = client;
        }

        #region Bytes Operations

        public int ByteArrayToInt32(byte[] data)
        {
            if (data.Length != 4) return 0;
            if (BitConverter.IsLittleEndian) Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }

        public byte[] Int32ToByteArray(int data) 
        {
            byte[] byteData = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian) Array.Reverse(byteData);
            return byteData;
        }

        #endregion

        #region Receive
        public int ReceiveInt32() 
        {
            (DataType type, byte[] data) info = Receive();
            if (info.type != DataType.Integer) return 0;
            return ByteArrayToInt32(info.data);
        }

        public string ReceiveString() 
        {
            (DataType type, byte[] data) info = Receive();
            if (info.type != DataType.String) return null;
            return Encoding.UTF8.GetString(info.data);
        }

        public bool ReceiveBoolean() 
        {
            (DataType type, byte[] data) info = Receive();
            if (info.type != DataType.Boolean) return false;
            return BitConverter.ToBoolean(info.data, 0);
        }

        public (DataType, byte[]) Receive() 
        {
            DataType type = (DataType)ByteArrayToInt32(Listen(4));
            switch (type) 
            {
                case DataType.String:
                    int messageLength = ByteArrayToInt32(Listen(4));
                    return (type, Listen(messageLength));
                case DataType.Integer:
                    return (type, Listen(4));
                case DataType.Boolean:
                    return (type, Listen(1));
                default:
                    return (type, null);
            }
        }


        byte[] Listen(int size) 
        {
            byte[] data = new byte[size];
            int currentDataIndex = 0;
            while(currentDataIndex < size) 
            {
                byte[] tmp = new byte[size - currentDataIndex];
                int recvd = currentDataIndex;
                try 
                {
                    recvd = _client.Receive(tmp);
                }
                catch 
                {
                    return null;
                }
                Array.Copy(tmp, 0, data, currentDataIndex, recvd);
                currentDataIndex += recvd;
            }
            return data;
        }
        #endregion

        #region Send

        public void Send(DataType type, byte[] data) 
        {
            int intType = (int)type;
            byte[] byteType = Int32ToByteArray(intType);
            _client.Send(byteType);
            if(type == DataType.String)
            {
                byte[] length = Int32ToByteArray(data.Length);
                _client.Send(length);
            }
            _client.Send(data);
        }

        public void SendString(string data) 
        {
            if (String.IsNullOrEmpty(data)) return;
            byte[] byteData = Encoding.UTF8.GetBytes(data);
            Send(DataType.String, byteData);
        }

        public void SendInt32(int data) 
        {
            byte[] byteData = Int32ToByteArray(data);
            Send(DataType.Integer, byteData);
        }

        public void SendBoolean(bool data) 
        {
            byte[] byteData = new byte[1];
            byteData[0] = Convert.ToByte(data);
            Send(DataType.Boolean, byteData);
        }

        public void Close() 
        {
            _client.Close();
        }

        #endregion
    }

    public enum DataType 
    {
        Null,
        String,
        Integer,
        Boolean,
    }
}
