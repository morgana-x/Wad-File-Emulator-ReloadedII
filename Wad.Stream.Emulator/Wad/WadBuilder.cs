using FileEmulationFramework.Lib.IO;
using FileEmulationFramework.Lib.IO.Struct;
using FileEmulationFramework.Lib.Utilities;
using Microsoft.Win32.SafeHandles;
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
            wad.WriteHeader(headerStream);

            long oldWadDataSectionOffset = wad.DataSectionOffset;


            List<WadFileEntry> newFileEntries = new List<WadFileEntry>();

            bool disableCustomFiles = false;
            long fileOffset = 0;

            var pairs = new List<StreamOffsetPair<System.IO.Stream>>()
            {
                // Add Header
                new (headerStream, OffsetRange.FromStartAndLength(0, headerStream.Length))
            };

            foreach (var entry in wad.FileEntries) // SOMETHING IS WRONG WITH THE HEADER PART
            {
                var newEntry = entry;
                //logger.Debug("Before: " + newEntry.FileOffset);
                
                //logger.Debug("After:" + newEntry.FileOffset);
                if (_customFiles.TryGetValue(entry.FileName, out var overwrittenFile) && !disableCustomFiles) // THIS BREAKS STUFF 
                {
                    newEntry.FileSize = overwrittenFile.Length;

                    OffsetRange range = OffsetRange.FromStartAndLength(wad.DataSectionOffset + fileOffset, newEntry.FileSize);
                    var fileSliceStream = new FileSliceStreamW32(overwrittenFile, logger);

                    logger.Debug("Adding custom file " + entry.FileName + " | " + overwrittenFile.FilePath + " | " + overwrittenFile.Length);

                    pairs.Add(new(fileSliceStream, range));
                }
                else
                {
                    OffsetRange range = OffsetRange.FromStartAndLength(wad.DataSectionOffset + fileOffset, entry.FileSize);
                    var originalEntry = new FileSlice(oldWadDataSectionOffset + entry.FileOffset, (int)entry.FileSize, wadFilepath);
                    var fs = new FileSliceStreamW32(originalEntry, logger);
                    pairs.Add(new(fs, range));
                }
                newEntry.FileOffset = fileOffset;
                newFileEntries.Add(newEntry);

                fileOffset += newEntry.FileSize;
            }
            wad.FileEntries = newFileEntries;

            wad.WriteHeader(headerStream);

            //wad.WadStream = null;


          
            return new MultiStream(pairs, logger);
        }
    }
}
