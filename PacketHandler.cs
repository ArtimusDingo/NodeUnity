using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;


namespace PacketHandling
{
    public struct Packet
    {
        public object value;
        byte[] bytes;
        
        public Packet(int _type, byte[] _bytes)
        {
            value = Convert.ChangeType(PacketHandler.Dictionary[_type].DynamicInvoke(_bytes), PacketHandler.TypeDictionary[_type]);
            bytes = _bytes;
        }
    }

    public class PacketHandler
    {
        public static Dictionary<int, Delegate> Dictionary = new Dictionary<int, Delegate>();
        public static Dictionary<int, Type> TypeDictionary = new Dictionary<int, Type>();
     
        public PacketHandler()
        {
            Dictionary[0] = new Func<byte[], int>(GetInt);
            Dictionary[1] = new Func<byte[], float>(GetFloat);
            Dictionary[2] = new Func<byte[], string>(GetString);
            Dictionary[3] = new Func<byte[], string>(GetASCIIString);
            Dictionary[4] = new Func<byte[], byte>(GetUInt8);
            TypeDictionary[0] = typeof(int);
            TypeDictionary[1] = typeof(float);
            TypeDictionary[2] = typeof(string);
            TypeDictionary[3] = typeof(string);
            TypeDictionary[4] = typeof(byte);
        }

        #region Private

        private int GetInt(byte[] _bytes)
        {
            return BitConverter.ToInt32(_bytes, 0);
        }

        private float GetFloat(byte[] _bytes)
        {
            return BitConverter.ToSingle(_bytes, 0);
        }

        private string GetString(byte[] _bytes)
        {
            return BitConverter.ToString(_bytes);
        }

        private string GetASCIIString(byte[] _bytes)
        {
            return ASCIIEncoding.ASCII.GetString(_bytes);
        }

        private byte GetUInt8(byte[] _bytes)
        {
         
            return _bytes[0];
        }

        #endregion

        #region Public

        public byte[] GetByteSection(byte[] _allbytes, int _readsize, int Offset = 0)
        {
            byte[] message = new byte[_readsize];
            Buffer.BlockCopy(_allbytes, Offset, message, 0, _readsize);
            return message;
        }

        public byte[] AddBytes(byte[] _original, byte[] _addition)
        {
            int size = _original.Length + _addition.Length;
            byte[] newbytes = new byte[size];
            Buffer.BlockCopy(_original, 0, newbytes, 0, _original.Length);
            Buffer.BlockCopy(_addition, 0, newbytes, _original.Length, _addition.Length);
            return newbytes;
        }

        #endregion 

    }
}
