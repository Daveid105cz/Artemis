﻿using System;
using System.IO;
using System.Reflection;
using Artemis.Storage.Entities.Plugins;
using McMaster.NETCore.Plugins;
using Newtonsoft.Json;
using Ninject;
using Ninject.Extensions.ChildKernel;
using Stylet;

namespace Artemis.Core
{
    /// <summary>
    ///     Represents basic info about a plugin and contains a reference to the instance of said plugin
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class PluginInfo : PropertyChangedBase
    {
        private string _description;
        private DirectoryInfo _directory;
        private bool _enabled;
        private Guid _guid;
        private string _icon;
        private Plugin _instance;
        private Exception _loadException;
        private string _main;
        private string _name;
        private Version _version;

        internal PluginInfo()
        {
        }

        /// <summary>
        ///     The plugins GUID
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Guid Guid
        {
            get => _guid;
            internal set => SetAndNotify(ref _guid, value);
        }

        /// <summary>
        ///     The name of the plugin
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Name
        {
            get => _name;
            internal set => SetAndNotify(ref _name, value);
        }

        /// <summary>
        ///     A short description of the plugin
        /// </summary>
        [JsonProperty]
        public string Description
        {
            get => _description;
            set => SetAndNotify(ref _description, value);
        }

        /// <summary>
        ///     The plugins display icon that's shown in the settings see <see href="https://materialdesignicons.com" /> for
        ///     available
        ///     icons
        /// </summary>
        [JsonProperty]
        public string Icon
        {
            get => _icon;
            set => SetAndNotify(ref _icon, value);
        }

        /// <summary>
        ///     The version of the plugin
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Version Version
        {
            get => _version;
            internal set => SetAndNotify(ref _version, value);
        }

        /// <summary>
        ///     The main entry DLL, should contain a class implementing Plugin
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Main
        {
            get => _main;
            internal set => SetAndNotify(ref _main, value);
        }

        /// <summary>
        ///     The plugins root directory
        /// </summary>
        public DirectoryInfo Directory
        {
            get => _directory;
            internal set => SetAndNotify(ref _directory, value);
        }

        /// <summary>
        ///     A reference to the type implementing Plugin, available after successful load
        /// </summary>
        public Plugin Instance
        {
            get => _instance;
            internal set => SetAndNotify(ref _instance, value);
        }

        /// <summary>
        ///     Indicates whether the user enabled the plugin or not
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            internal set => SetAndNotify(ref _enabled, value);
        }

        /// <summary>
        ///     Gets the exception thrown while loading
        /// </summary>
        public Exception LoadException
        {
            get => _loadException;
            internal set => SetAndNotify(ref _loadException, value);
        }

        /// <summary>
        ///     The PluginLoader backing this plugin
        /// </summary>
        internal PluginLoader PluginLoader { get; set; }

        /// <summary>
        ///     The assembly the plugin code lives in
        /// </summary>
        public Assembly Assembly { get; internal set; }

        /// <summary>
        /// The Ninject kernel of the plugin
        /// </summary>
        public IKernel Kernel { get; internal set; }

        /// <summary>
        ///     The entity representing the plugin
        /// </summary>
        internal PluginEntity PluginEntity { get; set; }
        
        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Name} v{Version} - {Guid}";
        }

        internal void ApplyToEntity()
        {
            PluginEntity.Id = Guid;
            PluginEntity.IsEnabled = Enabled;
        }

        internal void CreateLockFile()
        {
            File.Create(Path.Combine(Directory.FullName, "artemis.lock")).Close();
        }

        internal void DeleteLockFile()
        {
            if (GetLockFileCreated())
                File.Delete(Path.Combine(Directory.FullName, "artemis.lock"));
        }

        internal bool GetLockFileCreated()
        {
            return File.Exists(Path.Combine(Directory.FullName, "artemis.lock"));
        }

        public string? ResolveRelativePath(string path)
        {
            return path == null ? null : Path.Combine(Directory.FullName, path);
        }
    }
}