using FileEmulationFramework.Lib.IO;
using FileEmulationFramework.Lib.IO.Interfaces;
using FileEmulationFramework.Lib.IO.Struct;
using FileEmulationFramework.Lib.Utilities;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
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

        private void WriteHeader(System.IO.Stream stream)
        {
            stream.Write(WadLib.Wad.WadIdentifier); // Write WAD Identifier (AGAR)
            stream.Write(BitConverter.GetBytes((int)1)); // Major Version
            stream.Write(BitConverter.GetBytes((int)1)); // Minor Version, Needs to be 1 or Danganronpa 1 crashes?
            stream.Write(BitConverter.GetBytes((int)0)); // Header Size, set to zero to tell it to skip
        }

        private void WriteEntries(System.IO.Stream stream, List<NewWadFileEntry> files, List<WadDir> dirs)
        {
            stream.Write(BitConverter.GetBytes(files.Count));
            foreach (var entry in files)
                WadFile.WriteEntry(stream, entry.Entry.FileName, entry.Entry.FileSize, entry.Entry.FileOffset);

            stream.Write(BitConverter.GetBytes((long)dirs.Count));
            foreach (var entry in dirs)
            {
                WadDir.WriteEntry(stream, entry.DirectoryName, entry.SubDirectories.Count);
                foreach (var sub in entry.SubDirectories)
                    WadSubEntry.WriteEntry(stream, sub.SubFileName, sub.IsDirectory);
            }
        }

        private void WriteStreams(List<StreamOffsetPair<System.IO.Stream>> streampairs, long datasectionoffset, List<NewWadFileEntry> files)
        {
            long offset = 0;
            foreach (var fileSlice in files)
            {
                OffsetRange range = OffsetRange.FromStartAndLength(datasectionoffset + offset, fileSlice.Slice.Length);

                streampairs.Add(new StreamOffsetPair<System.IO.Stream>(fileSlice.Slice, range));

                offset += fileSlice.Slice.Length;
            }
        }

 
        class NewWadFileEntry
        {
            public WadFile Entry;
            public FileSliceStreamW32 Slice;

            public NewWadFileEntry() { }
            public NewWadFileEntry(WadFile entry, FileSliceStreamW32 slice)
            {
                Entry = entry;
                Slice = slice;
            }
        }

        public unsafe MultiStream Build(IntPtr handle, string wadFilepath, Logger? logger = null)
        {
            logger?.Info($"[{nameof(WadBuilder)}] Building Wad File | {{0}}", wadFilepath);

            var stream = new FileStream(new SafeFileHandle(handle, false), FileAccess.Read);


            WadLib.Wad wad = new WadLib.Wad(stream);

            System.IO.Stream headerStream = new MemoryStream();
            WriteHeader(headerStream);

            long oldWadDataSectionOffset = wad.DataSectionOffset;


            List<NewWadFileEntry> newFileEntries = new List<NewWadFileEntry>();

            bool disableCustomFiles = false;
            long fileOffset = 0;

            foreach (var entry in wad.FileEntries) // SOMETHING IS WRONG WITH THE HEADER PART
            {

                if (_customFiles.TryGetValue(entry.FileName, out var overwrittenFile) && !disableCustomFiles) // THIS BREAKS STUFF 
                {
                    entry.FileOffset = fileOffset;
                    entry.FileSize = overwrittenFile.Length;
                    newFileEntries.Add(new (entry, new FileSliceStreamW32(overwrittenFile, logger)));

                    logger.Debug("Overriding file " + entry.FileName + " | " + overwrittenFile.FilePath + " | " + overwrittenFile.Length);
                    _customFiles.Remove(entry.FileName);
                }
                else
                {
                    var originalEntry = new FileSlice(oldWadDataSectionOffset + entry.FileOffset, (int)entry.FileSize, wadFilepath);
                    var fs = new FileSliceStreamW32(originalEntry, logger);

                    entry.FileOffset = fileOffset;
                    newFileEntries.Add(new(entry, fs));
                }

                fileOffset += entry.FileSize;
            }


            foreach ( var entry in _customFiles)
            {
                NewWadFileEntry newEntry = new(new WadFile() { FileName = entry.Key, FileOffset = fileOffset, FileSize = entry.Value.Length }, new FileSliceStreamW32(entry.Value, logger));
               
                
                fileOffset += entry.Value.Length;
              
                logger.Debug("Adding new file " + entry.Key + " | " + entry.Value.FilePath + " | " + entry.Value.Length);

     
                var parentFolder = entry.Key.Substring(0, entry.Key.LastIndexOf('/') != -1 ? entry.Key.LastIndexOf('/') : entry.Key.Length );

                var startindex = 0;
                var endindex = 0;
                for (int i = 0; i < newFileEntries.Count; i++)
                {
                    if (newFileEntries[i].Entry.FileName.StartsWith(parentFolder))
                    {
                        startindex = i;
                        break;
                    }    
                }
                
                for (int i = startindex; i < newFileEntries.Count; i++)
                {
                    if (!newFileEntries[i].Entry.FileName.StartsWith(parentFolder))
                    {
                        endindex = i != 0 ?i-1 : i;
                        break;
                    }
                }
                string[] names = new string[(endindex - startindex) + 1];
                for (int i = 0; i< names.Length-1; i++)
                    names[i] = newFileEntries[startindex + i].Entry.FileName;

                names[names.Length - 1] = entry.Key;

                var insert = names.Order().ToList().IndexOf(entry.Key);



                newFileEntries.Insert(startindex + insert, newEntry);

                for (int i = startindex + insert; i < newFileEntries.Count; i++)
                    newFileEntries[i].Entry.FileOffset += entry.Value.Length;
                logger.Debug("Finding entry " + parentFolder);
                var dirEntry = wad.FindDirEntry(parentFolder);
                if (dirEntry == null) continue;
                logger.Debug("Found entry, adding sub entry to it's sub entries");
                logger.Debug(entry.Key.Substring(entry.Key.LastIndexOf('/') + 1));
                logger.Debug(dirEntry.DirectoryName);
                dirEntry.SubDirectories.Add(
                    new WadSubEntry() { IsDirectory = false, SubFileName = entry.Key.Substring(entry.Key.LastIndexOf('/') + 1) });
            }





            WriteEntries(headerStream, newFileEntries, wad.DirectoryEntries);

            var NewWadFileStreamPairs = new List<StreamOffsetPair<System.IO.Stream>>()
            {
                new (headerStream, OffsetRange.FromStartAndLength(0, headerStream.Length))
            };

            WriteStreams(NewWadFileStreamPairs, headerStream.Length, newFileEntries);
            wad.Dispose();



            return new MultiStream(NewWadFileStreamPairs, logger);
        }
    }
}
