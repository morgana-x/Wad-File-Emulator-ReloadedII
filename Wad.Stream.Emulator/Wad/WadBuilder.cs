using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FileEmulationFramework.Lib.IO;
using FileEmulationFramework.Lib.IO.Interfaces;
using FileEmulationFramework.Lib.IO.Struct;
using FileEmulationFramework.Lib.Utilities;
using Microsoft.Win32.SafeHandles;
using Reloaded.Mod.Interfaces;
using WadLib;

namespace Wad.Stream.Emulator.Wad
{
    internal class WadBuilder
    {
        private readonly Dictionary<string, FileSlice> _customFiles = new();

        /// <summary>
        /// Adds a file to the Virtual WAD builder.
        /// </summary>
   

        /// <summary>
        /// Adds a file to the Virtual WAD builder.
        /// </summary>
        /// <param name="filePath">Full path to the file.</param>
        public void AddOrReplaceFile(string filePath)
        {
            string[] filePathSplit = filePath.Split(Constants.WadExtension + Path.DirectorySeparatorChar);
            _customFiles[filePathSplit[^1].Replace("\\", "/")] = new (filePath);
        }
        public unsafe MultiStream Build(IntPtr handle, string wadFilepath, Logger? logger = null)
        {
            logger?.Info($"[{nameof(WadBuilder)}] Building Wad File | {{0}}", wadFilepath);

            var stream = new FileStream(new SafeFileHandle(handle, false), FileAccess.Read);


            WadLib.Wad wad = new WadLib.Wad(stream);

            System.IO.Stream headerStream = new MemoryStream();


            long oldWadDataSectionOffset = wad.DataSectionOffset;


            List<WadFileEntry> newFileEntries = new List<WadFileEntry>();
            Dictionary<WadFileEntry, WadFileEntry> oldNewEntryDict = new Dictionary<WadFileEntry, WadFileEntry>();

            bool disableCustomFiles = true;
            long offset = 0;
            foreach (var entry in wad.FileEntries) // SOMETHING IS WRONG WITH THE HEADER PART
            {
                var newEntry = entry;
                var oldEntry = entry;

                newEntry.FileOffset = offset;
                if (_customFiles.ContainsKey(oldEntry.FileName) && !disableCustomFiles) // THIS BREAKS STUFF 
                {
                    newEntry.FileSize = (long)_customFiles[oldEntry.FileName].Length;
                }

                newFileEntries.Add(newEntry);
                //oldFileEntries.Add(oldEntry);
                oldNewEntryDict.Add(oldEntry, newEntry);

                offset += newEntry.FileSize;
            }
            wad.FileEntries = newFileEntries;

            wad.WriteHeader(headerStream);

            var pairs = new List<StreamOffsetPair<System.IO.Stream>>()
            {
                // Add Header
                new (headerStream, OffsetRange.FromStartAndLength(0, headerStream.Length))
            };
            wad.DataSectionOffset = headerStream.Length;

            //wad.WadStream = null;

            
            foreach ( var pair in oldNewEntryDict) // THIS ALL WORKS!
            {
                var oldEntry = pair.Key;
                var newEntry = pair.Value;

                OffsetRange range = OffsetRange.FromStartAndLength(wad.DataSectionOffset + newEntry.FileOffset, newEntry.FileSize);

                if (_customFiles.TryGetValue(oldEntry.FileName, out var overwrittenFile))
                {
                    //var s = new MemoryStream();
                    var fileSliceStream = new FileSliceStreamW32(overwrittenFile, logger);

                    logger.Debug("Adding custom file " + oldEntry.FileName + " | " + overwrittenFile.FilePath + " | " + overwrittenFile.Length);

                    pairs.Add(new(fileSliceStream, range));
                }
                else
                {
                    var originalEntry = new FileSlice(oldWadDataSectionOffset + oldEntry.FileOffset, (int)oldEntry.FileSize, wadFilepath);
                    var fs = new FileSliceStreamW32(originalEntry, logger);
                    pairs.Add(new(fs, range));
                }
            }
            return new MultiStream(pairs, logger);
        }
    }
}
