using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileEmulationFramework.Interfaces;
using FileEmulationFramework.Interfaces.Reference;
using FileEmulationFramework.Lib.IO;
using FileEmulationFramework.Lib.Utilities;
using Microsoft.VisualBasic;
using Wad.Stream.Emulator.Wad;

namespace Wad.Stream.Emulator
{
    public class WadEmulator : IEmulator
    {
        public bool DumpFiles { get; set; }
        // Note: Handle->Stream exists because hashing IntPtr is easier; thus can resolve reads faster.
        private readonly WadBuilderFactory _builderFactory = new();
        private Logger _log;
        private Dictionary<string, MultiStream?> _pathToStream = new(StringComparer.OrdinalIgnoreCase);

        public WadEmulator(Logger log, bool dumpFiles)
        {
            _log = log;
            DumpFiles = dumpFiles;
        }
        public bool TryCreateFile(IntPtr handle, string filepath, string route, out IEmulatedFile emulatedFile)
        {
            // Check if we already made a custom WAD for this file.
            emulatedFile = null!;

            if (_pathToStream.TryGetValue(filepath, out var multiStream))
            {
                // Avoid recursion into same file.
                if (multiStream == null)
                    return false;
                //_log.Debug("Returning multistream for " + filepath + "|" + route);
                emulatedFile = new EmulatedFile<MultiStream>(multiStream);
                return true;
            }

            // Check extension.
            if (!filepath.EndsWith(Constants.WadExtension, StringComparison.OrdinalIgnoreCase))
                return false;

 
            // Check file type.
            if (!WadLib.Wad.IsWad(handle))
                return false;
            if (!TryCreateEmulatedFile(handle, filepath, route, ref emulatedFile!, out _))
                return false;

            return true;

        }
        public bool TryCreateEmulatedFile(IntPtr handle, string filepath, string route, ref IEmulatedFile? emulatedFile, out MultiStream? stream)
        {
            // Check if there's a known route for this file
            // Put this before actual file check because I/O.
            stream = null;
            if (!_builderFactory.TryCreateFromPath(filepath, log: _log, out var builder))
                return false;

            if (!WadLib.Wad.IsWad(handle))
                return false;

            _log.Debug(filepath);
            // Make the WAD file.
            _pathToStream[filepath] = null; // Avoid recursion into same file.

            try
            {
                stream = builder!.Build(handle, filepath, _log);

                _pathToStream[filepath] = stream;
                
                emulatedFile = new EmulatedFile<MultiStream>(stream);
                
                if (DumpFiles)
                    DumpFile(filepath, stream);

                return true;
            }
            catch (Exception e)
            {
                _log.Error(e.ToString());
                return false;
            }
        }
        public void OnModLoading(string modFolder)
        {
            var redirectorFolder = $"{modFolder}/{Constants.RedirectorFolder}";
            _log.Debug(redirectorFolder);
            if (Directory.Exists(redirectorFolder))
            {
                _builderFactory.AddFromFolders(redirectorFolder, _log);
            }
        }
        private void DumpFile(string filepath, MultiStream stream)
        {
            var filePath = Path.GetFullPath($"{Constants.DumpFolder}/{Path.GetFileName(filepath)}");
            Directory.CreateDirectory(Constants.DumpFolder);
            _log.Info($"[{nameof(WadEmulator)}] Dumping {filepath}");
            using var fileStream = new FileStream(filePath, FileMode.Create);
            //long oldPos = stream.Position;
            //stream.Position = 0;
            stream.CopyTo(fileStream);
            //stream.Position = oldPos;
            fileStream.Close();
            _log.Info($"[{nameof(WadEmulator)}] Written To {filePath}");
        }
    }
}
