using System.Configuration;

namespace Plex.Infrastructure.Configuration.Services
{
    public class SlaveElement : ConfigurationElement
    {
        #region attributes
        [ConfigurationPropertyAttribute("threads", IsRequired = false, IsKey = false, IsDefaultCollection = false, DefaultValue = "-1")]
        public int MaxThreadsCount
        {
            get
            {
                return ((int)(base["threads"]));
            }
        }

        [ConfigurationPropertyAttribute("workDir", IsRequired = false, IsKey = false, IsDefaultCollection = false, DefaultValue = "")]
        public string WorkDir
        {
            get
            {
                return ((string)(base["workDir"]));
            }
        }
        #endregion attributes
    }
}
