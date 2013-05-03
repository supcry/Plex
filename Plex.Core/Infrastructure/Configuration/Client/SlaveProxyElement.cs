using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Plex.Contracts.Data;

namespace Plex.Infrastructure.Configuration.Client
{
    public class SlaveProxyElement : ConfigurationElement, IConnection
    {
        #region attributes
        [ConfigurationPropertyAttribute("address", IsRequired = true, IsKey = false, IsDefaultCollection = false)]
        public string Address
        {
            get
            {
                return ((string)(base["address"]));
            }
        }

        [ConfigurationPropertyAttribute("port", IsRequired = true, IsKey = false, IsDefaultCollection = false)]
        public int Port
        {
            get
            {
                return ((int)(base["port"]));
            }
        }

        public string FullAddress
        {
            get { return Address + ":" + Port; } 
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
