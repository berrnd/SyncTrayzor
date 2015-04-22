﻿using NLog;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SyncTrayzor.Services.Config
{
    public class ConfigurationChangedEventArgs : EventArgs
    {
        private readonly Configuration baseConfiguration;
        public Configuration NewConfiguration
        {
            // Ensure we always clone it, so people can modify
            get { return new Configuration(this.baseConfiguration); }
        }

        public ConfigurationChangedEventArgs(Configuration newConfiguration)
        {
            this.baseConfiguration = newConfiguration;
        }
    }

    public interface IConfigurationProvider
    {
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        bool HadToCreateConfiguration { get; }

        void Initialize(Configuration defaultConfiguration);
        Configuration Load();
        void Save(Configuration config);
        void AtomicLoadAndSave(Action<Configuration> setter);
    }

    public class ConfigurationProvider : IConfigurationProvider
    {
        private const string apiKeyChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-";
        private const int apiKeyLength = 40;

        private readonly Func<XDocument, XDocument>[] migrations;

        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly SynchronizedEventDispatcher eventDispatcher;
        private readonly XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
        private readonly IApplicationPathsProvider paths;

        private readonly object currentConfigLock = new object();
        private Configuration currentConfig;

        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        public bool HadToCreateConfiguration { get; private set; }

        public ConfigurationProvider(IApplicationPathsProvider paths)
        {
            this.paths = paths;
            this.eventDispatcher = new SynchronizedEventDispatcher(this);

            this.migrations = new Func<XDocument, XDocument>[]
            {
                this.MigrateV1ToV2,
                this.MigrateV2ToV3,
            };
        }

        public void Initialize(Configuration defaultConfiguration)
        {
            if (defaultConfiguration == null)
                throw new ArgumentNullException("defaultConfiguration");

            if (!File.Exists(Path.GetDirectoryName(this.paths.ConfigurationFilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(this.paths.ConfigurationFilePath));

            this.currentConfig = this.LoadFromDisk(defaultConfiguration);

            bool installCountChanged = false;
            bool updateConfigInstallCount = false;
            int latestInstallCount = 0;
            // Might be portable, in which case this file won't exist
            if (File.Exists(this.paths.InstallCountFilePath))
            {
                latestInstallCount = Int32.Parse(File.ReadAllText(this.paths.InstallCountFilePath).Trim());
                if (latestInstallCount != this.currentConfig.LastSeenInstallCount)
                {
                    installCountChanged = true;
                    updateConfigInstallCount = true;
                }
            }

            // They're the same if we're portable, in which case, nothing to do
            if (this.paths.SyncthingPath != this.paths.SyncthingBackupPath)
            {
                if (!File.Exists(this.paths.SyncthingPath))
                {
                    if (File.Exists(this.paths.SyncthingBackupPath))
                    {
                        logger.Info("Syncthing doesn't exist at {0}, so copying from {1}", this.paths.SyncthingPath, this.paths.SyncthingBackupPath);
                        File.Copy(this.paths.SyncthingBackupPath, this.paths.SyncthingPath);
                    }
                    else
                    {
                        throw new Exception(String.Format("Unable to find Syncthing at {0} or {1}", this.paths.SyncthingPath, this.paths.SyncthingBackupPath));
                    }
                }
                else if (installCountChanged)
                {
                    // If we hit this, then latestInstallCount is set to a real value
                    logger.Info("Install Count changed, so updating Syncthing at {0} from {1}", this.paths.SyncthingPath, this.paths.SyncthingBackupPath);
                    try
                    {
                        File.Copy(this.paths.SyncthingBackupPath, this.paths.SyncthingPath, true);
                    }
                    catch (IOException e)
                    {
                        // Syncthing.exe was probably running. We'll try again next time
                        updateConfigInstallCount = false;
                        logger.Error(String.Format("Failed to copy Syncthing from {0} to {1}", this.paths.SyncthingBackupPath, this.paths.SyncthingPath), e);
                    }
                }
            }

            if (updateConfigInstallCount)
            {
                this.currentConfig.LastSeenInstallCount = latestInstallCount;
                this.SaveToFile(this.currentConfig);
            }
        }

        private Configuration LoadFromDisk(Configuration defaultConfiguration)
        {
            // Merge any updates from app.config / Configuration into the configuration file on disk
            // (creating if necessary)
            logger.Debug("Loaded default configuration: {0}", defaultConfiguration);
            XDocument defaultConfig;
            using (var ms = new MemoryStream())
            {
                this.serializer.Serialize(ms, defaultConfiguration);
                ms.Position = 0;
                defaultConfig = XDocument.Load(ms);
            }

            XDocument loadedConfig;
            if (File.Exists(this.paths.ConfigurationFilePath))
            {
                logger.Debug("Found existing configuration at {0}", this.paths.ConfigurationFilePath);
                loadedConfig = XDocument.Load(this.paths.ConfigurationFilePath);
                loadedConfig = this.MigrationConfiguration(loadedConfig);

                var merged = loadedConfig.Root.Elements().Union(defaultConfig.Root.Elements(), new XmlNodeComparer());
                loadedConfig.Root.ReplaceNodes(merged);
            }
            else
            {
                loadedConfig = defaultConfig;
            }

            var configuration = (Configuration)this.serializer.Deserialize(loadedConfig.CreateReader());
            if (configuration.SyncthingApiKey == null)
                configuration.SyncthingApiKey = this.GenerateApiKey();

            this.SaveToFile(configuration);

            return configuration;
        }

        private XDocument MigrationConfiguration(XDocument configuration)
        {
            var version = (int?)configuration.Root.Attribute("Version");
            if (version == null)
            {
                configuration = this.LegacyMigrationConfiguration(configuration);
                version = 1;
            }

            // Element 0 is the migration from 0 -> 1, etc
            for (int i = version.Value; i < Configuration.CurrentVersion; i++)
            {
                logger.Info("Migration config version {0} to {1}", i, i + 1);

                if (this.paths.ConfigurationFileBackupPath != null)
                {
                    if (!File.Exists(this.paths.ConfigurationFileBackupPath))
                        Directory.CreateDirectory(this.paths.ConfigurationFileBackupPath);
                    var backupPath = Path.Combine(this.paths.ConfigurationFileBackupPath, String.Format("config-v{0}.xml", i));
                    logger.Debug("Backing up configuration to {0}", backupPath);
                    configuration.Save(backupPath);
                }
                
                configuration = this.migrations[i - 1](configuration);
                configuration.Root.SetAttributeValue("Version", i + 1);
            }

            return configuration;
        }

        private XDocument MigrateV1ToV2(XDocument configuration)
        {
            var traceElement = configuration.Root.Element("SyncthingTraceFacilities");
            // No need to remove - it'll be ignored when we deserialize into Configuration, and not written back to file
            if (traceElement != null)
            {
                var envVarsNode = new XElement("SyncthingEnvironmentalVariables",
                    new XElement("Item",
                        new XElement("Key", "STTRACE"),
                        new XElement("Value", traceElement.Value)
                    )
                );
                var existingEnvVars = configuration.Root.Element("SyncthingEnvironmentalVariables");
                if (existingEnvVars != null)
                    existingEnvVars.ReplaceWith(envVarsNode);
                else
                    configuration.Root.Add(envVarsNode);
            }

            return configuration;
        }

        private XDocument MigrateV2ToV3(XDocument configuration)
        {
            bool? visible = (bool?)configuration.Root.Element("ShowSyncthingConsole");
            configuration.Root.Add(new XElement("SyncthingConsoleHeight", visible == true ? Configuration.DefaultSyncthingConsoleHeight : 0.0));
            return configuration;
        }

        private XDocument LegacyMigrationConfiguration(XDocument configuration)
        {
            var address = configuration.Root.Element("SyncthingAddress").Value;

            // We used to store http/https in the config, but we no longer do. A migration is necessary
            if (address.StartsWith("http://"))
                configuration.Root.Element("SyncthingAddress").Value = address.Substring("http://".Length);
            else if (address.StartsWith("https://"))
                configuration.Root.Element("SyncthingAddress").Value = address.Substring("https://".Length);

            return configuration;
        }

        public Configuration Load()
        {
            lock (this.currentConfigLock)
            {
                return new Configuration(this.currentConfig);
            }
        }

        public void Save(Configuration config)
        {
            logger.Debug("Saving configuration: {0}", config);
            lock (this.currentConfigLock)
            {
                this.currentConfig = config;
                this.SaveToFile(config);
            }
            this.OnConfigurationChanged(config);
        }

        public void AtomicLoadAndSave(Action<Configuration> setter)
        {
            // We can just let them modify the current config here - since it's all inside the lock
            Configuration newConfig;
            lock (this.currentConfigLock)
            {
                setter(this.currentConfig);
                this.SaveToFile(this.currentConfig);
                newConfig = this.currentConfig;
            }
            this.OnConfigurationChanged(newConfig);
        }

        private void SaveToFile(Configuration config)
        {
            using (var stream = File.Open(this.paths.ConfigurationFilePath, FileMode.Create))
            {
                this.serializer.Serialize(stream, config);
            }
        }

        private string GenerateApiKey()
        {
            var random = new Random();
            var apiKey = new char[apiKeyLength];
            for (int i = 0; i < apiKeyLength; i++)
            {
                apiKey[i] = apiKeyChars[random.Next(apiKeyChars.Length)];
            }
            return new string(apiKey);
        }

        private void OnConfigurationChanged(Configuration newConfiguration)
        {
            this.eventDispatcher.Raise(this.ConfigurationChanged, new ConfigurationChangedEventArgs(newConfiguration));
        }

        private class XmlNodeComparer : IEqualityComparer<XElement>
        {
            public bool Equals(XElement x, XElement y)
            {
                return x.Name == y.Name;
            }

            public int GetHashCode(XElement obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }

    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message) : base(message)
        { }
    }
}
