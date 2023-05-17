#region copyright
//  Copyright (C) 2022 Auto Dark Mode
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using AutoDarkModeLib.ComponentSettings;
using AutoDarkModeLib.ComponentSettings.Base;
using AutoDarkModeLib.Configs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;
using YamlDotNet.Serialization.NamingConventions;


namespace AutoDarkModeLib
{
    public class AdmConfigBuilder
    {
        private static AdmConfigBuilder instance;
        public AdmConfig Config { get; private set; }
        public LocationData LocationData { get; private set; }
        public BaseSettings<ScriptSwitchSettings> ScriptConfig { get; private set; }
        public UpdaterData UpdaterData { get; private set; }
        public PostponeData PostponeData { get; private set; }
        public static string ConfigDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoDarkMode");
        public static string ConfigFilePath { get; } = Path.Combine(ConfigDir, "config.yaml");
        public static string LocationDataPath { get; } = Path.Combine(ConfigDir, "location_data.yaml");
        public static string UpdaterDataPath { get; } = Path.Combine(ConfigDir, "update.yaml");
        public static string ScriptConfigPath { get; } = Path.Combine(ConfigDir, "scripts.yaml");
        public static string PostponeDataPath { get; } = Path.Combine(ConfigDir, "postpone_data.yaml");
        public bool Loading { get; private set; }
        private EventHandler<AdmConfig> configUpdatedHandler;
        public event EventHandler<AdmConfig> ConfigUpdatedHandler
        {

            add
            {
                configUpdatedHandler -= value;
                configUpdatedHandler += value;
            }
            remove
            {
                configUpdatedHandler -= value;
            }
        }
        private EventHandler<LocationData> locationDataUpdatedHandler;
        public event EventHandler<LocationData> LocationDataUpdatedHandler
        {

            add
            {
                locationDataUpdatedHandler -= value;
                locationDataUpdatedHandler += value;
            }
            remove
            {
                locationDataUpdatedHandler -= value;
            }
        }

        protected AdmConfigBuilder()
        {
            if (instance == null)
            {
                Config = new();
                LocationData = new();
                UpdaterData = new();
                ScriptConfig = new();
                PostponeData = new();
            }
        }

        public static AdmConfigBuilder Instance()
        {
            if (instance == null)
            {
                instance = new AdmConfigBuilder();
            }
            return instance;
        }

        public void Save()
        {
            SaveConfig(ConfigFilePath, Config);
        }

        public void SaveLocationData()
        {
            SaveConfig(LocationDataPath, LocationData);
        }

        public void SaveUpdaterData()
        {
            SaveConfig(UpdaterDataPath, UpdaterData);
        }

        public void SaveScripts()
        {
            SaveConfig(ScriptConfigPath, ScriptConfig, true, true);
        }

        public void SavePostponeData()
        {
            PostponeData.LastModified = DateTime.Now;
            SaveConfig(PostponeDataPath, PostponeData);
        }

        public static void MakeConfigBackup()
        {
            string backupPath = Path.Combine(ConfigDir, "config_backup.yaml");
            File.Copy(ConfigFilePath, backupPath, true);
        }

        private static void SaveConfig(string path, object obj, bool useFlowStyleList = false, bool omitEmpty = false)
        {
            Directory.CreateDirectory(ConfigDir);
            //string jsonConfig = JsonConvert.SerializeObject(obj, Formatting.Indented);
            SerializerBuilder yamlBuilder;
            yamlBuilder = new SerializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance);
            if (useFlowStyleList) yamlBuilder.WithEventEmitter(next => new FlowStyleStringListEmitter(next));
            if (omitEmpty)
            {
                yamlBuilder.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections);
            }
            ISerializer yamlSerializer = yamlBuilder.Build();

            string yamlConfig = yamlSerializer.Serialize(obj);
            for (int i = 0; i < 10; i++)
            {
                if (IsFileLocked(new FileInfo(path)))
                {
                    Thread.Sleep(500);
                }
                else
                {
                    using StreamWriter writer = new(File.Open(path, FileMode.Create, FileAccess.Write));
                    writer.WriteLine(yamlConfig);
                    writer.Close();
                    return;
                }
            }
            throw new TimeoutException($"Saving to {path} failed after 10 retries");
        }

        private string LoadFile(string path)
        {
            Loading = true;
            Exception readException = new TimeoutException($"Reading from {path} failed after 3 retries");
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    using StreamReader dataReader = new(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                    return dataReader.ReadToEnd();
                }
                catch (Exception ex)
                {
                    readException = ex;
                    Thread.Sleep(500);
                }
            }
            throw readException;
        }

        public void LoadLocationData()
        {
            Loading = true;
            LocationData deser = Deserialize<LocationData>(LocationDataPath, LocationData);
            LocationData = deser ?? LocationData;
            Loading = false;
        }

        public void OnLocationDataUpdated(LocationData old)
        {
            locationDataUpdatedHandler?.Invoke(old, LocationData);
        }

        public void LoadUpdaterData()
        {
            Loading = true;
            UpdaterData deser = Deserialize<UpdaterData>(UpdaterDataPath, UpdaterData);
            UpdaterData = deser ?? UpdaterData;
            Loading = true;
        }

        public void Load(bool createIfNotExists = true)
        {
            Loading = true;
            AdmConfig deser = Deserialize<AdmConfig>(ConfigFilePath, Config, createIfNotExists);
            Config = deser ?? Config;
            Loading = false;
        }

        public void LoadScriptConfig()
        {
            Loading = true;
            BaseSettings<ScriptSwitchSettings> deser = Deserialize<BaseSettings<ScriptSwitchSettings>>(ScriptConfigPath, ScriptConfig, true);
            ScriptConfig = deser ?? ScriptConfig;
            Loading = false;
        }

        public void LoadPostponeData()
        {
            Loading = true;
            PostponeData deser = Deserialize<PostponeData>(PostponeDataPath, PostponeData);
            PostponeData = deser ?? PostponeData;
            Loading = false;
        }

        /// <summary>
        /// Event source tha should trigger whenever the main configuration file has updated
        /// </summary>
        /// <param name="old"></param>
        public void OnConfigUpdated(AdmConfig old)
        {
            configUpdatedHandler?.Invoke(old, Config);
        }

        private T Deserialize<T>(string FilePath, object current, bool useFlowStyleList = false, bool createIfNotExists = true)
        {
            if (!File.Exists(FilePath) && createIfNotExists)
            {
                SaveConfig(FilePath, current, useFlowStyleList);
            }
            var yamlDeserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
            T deserializedConfigYaml = yamlDeserializer.Deserialize<T>(LoadFile(FilePath));
            return deserializedConfigYaml;
        }

        /// <summary>
        /// Checks if the config file is locked
        /// </summary>
        /// <param name="file">the file to be checked</param>
        /// <returns>true if locked; false otherwise</returns>
        public static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            if (!File.Exists(file.FullName))
            {
                return false;
            }
            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {

                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
    }

    class FlowStyleStringListEmitter : ChainedEventEmitter
    {
        public FlowStyleStringListEmitter(IEventEmitter nextEmitter)
            : base(nextEmitter) { }

        public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter)
        {
            if (typeof(List<string>).IsAssignableFrom(eventInfo.Source.Type) || typeof(List<SwitchSource>).IsAssignableFrom(eventInfo.Source.Type))
            {
                eventInfo = new SequenceStartEventInfo(eventInfo.Source)
                {
                    Style = SequenceStyle.Flow
                };
            }
            nextEmitter.Emit(eventInfo, emitter);
        }
    }
}
