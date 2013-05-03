using System;
using System.Runtime.Serialization;

namespace Plex.Contracts.Data
{
    public interface IConnection : IComparable<IConnection>, IEquatable<IConnection>
    {
        string Address { get; }
        int Port { get; }
    }

    [DataContract]
    public class Connection : IConnection
    {
        [DataMember(Order = 1)]
        public string Address { get; set; }
        [DataMember(Order = 2)]
        public int Port { get; set; }

        public Connection(){}//for protobuf

        public Connection(string address, int port)
        {
            Address = address;
            Port = port;
        }

        public Connection(IConnection con) : this(con.Address, con.Port){}

        public string FullAddress { get { return Address + ":" + Port; } }

        public override int GetHashCode()
        {
            return FullAddress.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var c = obj as Connection;
            if (c == null)
                return false;
            return c.Address == Address && c.Port == Port;
        }

        public bool Equals(IConnection con)
        {
            return con.Port == Port && con.Address == Address;
        }

        public int CompareTo(IConnection con)
        {
            var ret = String.CompareOrdinal(Address, con.Address);
            if (ret != 0)
                return ret;
            return Port.CompareTo(con.Port);
        }
    }

    [DataContract]
    public class TaskConnection
    {
        [DataMember(Order = 1)]
        public string Address;
       
        [DataMember(Order = 2)]
        public int Port;

        [DataMember(Order = 3)]
        public string TaskName;

        public TaskConnection(string address, int port, string taskName)
        {
            Address = address;
            Port = port;
            TaskName = taskName;
        }

        public string FullAddress { get { return Address + ":" + Port + "/task/" + TaskName; } }

        public override int GetHashCode()
        {
            return FullAddress.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var c = obj as TaskConnection;
            if (c == null)
                return false;
            return c.Address == Address && c.Port == Port;
        }

        public Connection AsConnection{get{ return new Connection(Address, Port);}}
    }
}
