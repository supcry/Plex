using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.Contracts;

namespace Plex.Infrastructure.Configuration.Client
{
    [ConfigurationCollectionAttribute(typeof(SlaveProxyElement), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate, AddItemName = "slaveProxy")]
    public class SlaveProxyElementList : ConfigurationElementCollection, IEnumerable<SlaveProxyElement>
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new SlaveProxyElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            Contract.Assume(element is SlaveProxyElement);
            return ((SlaveProxyElement)element).FullAddress;
        }

        public SlaveProxyElement this[int index]
        {
            get { return (SlaveProxyElement)BaseGet(index); }
        }

        new public SlaveProxyElement this[string name]
        {
            get { return (SlaveProxyElement)BaseGet(name); }
        }

        IEnumerator<SlaveProxyElement> IEnumerable<SlaveProxyElement>.GetEnumerator()
        {
            foreach (SlaveProxyElement ci in this)
                yield return ci;
        }
    }
}
