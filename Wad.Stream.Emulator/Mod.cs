using FileEmulationFramework.Interfaces;
using FileEmulationFramework.Lib.Utilities;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using Wad.Stream.Emulator.Configuration;
using Wad.Stream.Emulator.Template;

namespace Wad.Stream.Emulator
{
    /// <summary>
    /// Your mod logic goes here.
    /// </summary>
    public class Mod : ModBase // <= Do not Remove.
    {
        /// <summary>
        /// Provides access to the mod loader API.
        /// </summary>
        private readonly IModLoader _modLoader;

        /// <summary>
        /// Provides access to the Reloaded logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Provides access to this mod's configuration.
        /// </summary>
        private Config _configuration;

        /// <summary>
        /// The configuration of the currently executing mod.
        /// </summary>
        private readonly IModConfig _modConfig;

        private Logger _log;
        private WadEmulator _wadEmulator;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _logger = context.Logger;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;

            _modLoader.ModLoading += OnModLoading;
            _modLoader.OnModLoaderInitialized += OnModLoaderInitialized;
            _log = new Logger(_logger, _configuration.LogLevel);
            _log.Info("Starting WAD.Stream.Emulator");
            _wadEmulator = new WadEmulator(_log, _configuration.DumpWad);

            _modLoader.GetController<IEmulationFramework>().TryGetTarget(out var framework);
            framework!.Register(_wadEmulator);
        }
        private void OnModLoaderInitialized()
        {
            _modLoader.ModLoading -= OnModLoading;
            _modLoader.OnModLoaderInitialized -= OnModLoaderInitialized;
        }

        private void OnModLoading(IModV1 mod, IModConfigV1 modConfig) => _wadEmulator.OnModLoading(_modLoader.GetDirectoryForModId(modConfig.ModId));

        public override void ConfigurationUpdated(Config configuration)
        {
            // Apply settings from configuration.
            // ... your code here.
            _configuration = configuration;
            _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
            _log.LogLevel = configuration.LogLevel;
            _configuration.DumpWad = configuration.DumpWad;
        }
        #region Standard Overrides
        #endregion
        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
    }
}