using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Plex.Infrastructure.Configuration.Client;
using Plex.Infrastructure.Configuration.Services;

namespace Plex.Infrastructure.Configuration
{
    public class Settings : ConfigurationSection
    {
        #region xml
        [ConfigurationPropertyAttribute("services", IsRequired = true, IsKey = false, IsDefaultCollection = false)]
        public ServicesElement Services
        {
            get
            {
                return ((ServicesElement)(base["services"]));
            }
        }

        [ConfigurationPropertyAttribute("slaveProxies", IsRequired = false, IsKey = false, IsDefaultCollection = false)]
        public  SlaveProxyElementList SlaveProxies
        {
            get
            {
                return ((SlaveProxyElementList)(base["slaveProxies"]));
            }
        }
        #endregion xml



        public static Settings Get()
        {
            if (_instance == null)
                Load();

            return _instance;
        }

        public static string GetConnectionString(string connectionString, string connectionStringName, string defaultConnectionStringName = null)
        {
            if (!string.IsNullOrEmpty(connectionString))
                return connectionString;

            if (!string.IsNullOrEmpty(connectionStringName))
                return _configuration.ConnectionStrings.ConnectionStrings[connectionStringName].ConnectionString;

            if (!string.IsNullOrEmpty(defaultConnectionStringName))
                return _configuration.ConnectionStrings.ConnectionStrings[defaultConnectionStringName].ConnectionString;

            return null;
        }

        public static void Load(string fileName = null)
        {
            if (fileName != null)
            {
                _configuration = ConfigurationManager.OpenMappedExeConfiguration(
                    new ExeConfigurationFileMap { ExeConfigFilename = Path.Combine(Directory.GetCurrentDirectory(), fileName) },
                    ConfigurationUserLevel.None);
            }
            else
                _configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            _instance = (Settings)_configuration.GetSection("plex");
            Debug.Assert(_instance != null);
        }

        private static Settings _instance;
        private static System.Configuration.Configuration _configuration;

        public IEnumerable<ConfigurationElement> GetConfiguredServiceElements()
        {
            return GetAllServiceElements().Where(element => element.ElementInformation.IsPresent);
        }

        private IEnumerable<ConfigurationElement> GetAllServiceElements()
        {
            yield return Services.Master;
            yield return Services.Slave;
        }

        private IEnumerable<SlaveProxyElement> GetAllSlaveProxyElements()
        {
            if(!SlaveProxies.ElementInformation.IsPresent)
                yield break;
            foreach (var p in SlaveProxies)
                yield return (SlaveProxyElement)p;
        }

        public bool IsLocalMasterEnabled()
        {
            return Services.Master.ElementInformation.IsPresent;
        }

        public bool IsLocalSlaveEnabled()
        {
            return Services.Slave.ElementInformation.IsPresent;
        }
    }
}
