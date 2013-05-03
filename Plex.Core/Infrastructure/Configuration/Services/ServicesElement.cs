using System;
using System.Configuration;
using Plex.Contracts.Data;

namespace Plex.Infrastructure.Configuration.Services
{
    public class ServicesElement : ConfigurationElement, IConnection
    {
        #region properties
        [ConfigurationProperty("master")]
        public MasterElement Master
        {
            get { return (MasterElement)this["master"]; }
        }

        [ConfigurationProperty("slave")]
        public SlaveElement Slave
        {
            get { return (SlaveElement)this["slave"]; }
        }
        #endregion properties

        #region attributes
        [ConfigurationPropertyAttribute("address", IsRequired = false, IsKey = false, IsDefaultCollection = false, DefaultValue = "0.0.0.0")]
        public string Address
        {
            get
            {
                return ((string)(base["address"]));
            }
        }

        [ConfigurationPropertyAttribute("port", IsRequired = false, IsKey = false, IsDefaultCollection = false, DefaultValue = "4000")]
        public int Port
        {
            get
            {
                return ((int)(base["port"]));
            }
        }

        [ConfigurationPropertyAttribute("name", IsRequired = false, IsKey = false, IsDefaultCollection = false)]
        public string Name
        {
            get
            {
                var ret = ((string)(base["name"]));
                if (string.IsNullOrEmpty(ret))
                    ret = Environment.MachineName + ":" + Port;
                return ret;
            }
        }
        #endregion attributes

        public bool Equals(IConnection r)
        {
            return r.Address == Address && r.Port == Port;
        }

        public int CompareTo(IConnection r)
        {
            var ret = String.CompareOrdinal(Address, r.Address);
            if (ret != 0)
                return ret;
            return Port.CompareTo(r.Port);
        }
    }
}
