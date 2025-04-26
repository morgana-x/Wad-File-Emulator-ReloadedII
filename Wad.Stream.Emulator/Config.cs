﻿using System.ComponentModel;
using FileEmulationFramework.Lib.Utilities;
using Wad.Stream.Emulator.Template.Configuration;

namespace Wad.Stream.Emulator.Configuration
{
    public class Config : Configurable<Config>
    {
        [DisplayName("Log Level")]
        [Description("Declares which elements should be logged to the console.\nMessages less important than this level will not be logged.")]
        [DefaultValue(LogSeverity.Warning)]
        public LogSeverity LogLevel { get; set; } = LogSeverity.Information;

        [DisplayName("Dump Emulated WAD Files")]
        [Description("Creates a dump of emulated WAD files as they are written.")]
        [DefaultValue(LogSeverity.Information)]
        public bool DumpWad { get; set; } = false;
    }

    /// <summary>
    /// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
    /// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
    /// </summary>
    public class ConfiguratorMixin : ConfiguratorMixinBase
    {
        // 
    }
}
