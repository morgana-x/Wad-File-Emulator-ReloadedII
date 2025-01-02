﻿using FileEmulationFramework.Lib.IO;
using FileEmulationFramework.Lib;
using System;
using FileEmulationFramework.Lib.Utilities;

namespace Wad.Stream.Emulator.Wad
{
    internal class WadBuilderFactory
    {
        private List<RouteGroupTuple> _routeGroupTuples = new();

        /// <summary>
        /// Adds all available routes from folders.
        /// </summary>
        /// <param name="redirectorFolder">Folder containing the redirector's files.</param>
        public void AddFromFolders(string redirectorFolder, Logger log = null)
        {
            redirectorFolder = redirectorFolder.Replace("/", "\\");
            // Get contents.
            WindowsDirectorySearcher.GetDirectoryContentsRecursiveGrouped(redirectorFolder, out var groups);

            // Find matching folders.
            foreach (var group in groups)
            {
                if (group.Files.Length <= 0)
                    continue;
                log?.Debug("Adding " + "\n  " + redirectorFolder + "\n  " +  group.Directory.FullPath + "\n     " + group.Files[0]);
                var route = Route.GetRoute(redirectorFolder, group.Directory.FullPath);
                int index = route.IndexOf("\\");
                if (index != -1) route = route.Substring(0, index); // Avoid redirector's file in the root of wad file, which will cause the route to have no "\\"
                log?.Debug("Route: " + route);
                _routeGroupTuples.Add(new RouteGroupTuple()
                {
                    Route = new Route(route),
                    Files = group
                });
            }
        }

        /// <summary>
        /// Tries to create an WAD from a given route.
        /// </summary>
        /// <param name="path">The file path/route to create WAD Builder for.</param>
        /// <param name="builder">The created builder.</param>
        /// <returns>True if a builder could be made, else false (if there are no files to modify this WAD).</returns>
        public bool TryCreateFromPath(string path, Logger log, out WadBuilder? builder)
        {
            builder = default;
            var route = new Route(path);
            foreach (var group in _routeGroupTuples)
            {
                //log?.Debug("[   " + route.FullPath);
                //log?.Debug("[   " + group.Route.FullPath);
                if (!route.Matches(group.Route.FullPath))
                    continue;

                // Make builder if not made.
                builder ??= new WadBuilder();

                // Add files to builder.
                var dir = group.Files.Directory.FullPath;
                foreach (var file in group.Files.Files)
                    builder.AddOrReplaceFile(Path.Combine(dir, file));
            }

            return builder != null;
        }
    }

    internal struct RouteGroupTuple
    {
        /// <summary>
        /// Route associated with this tuple.
        /// </summary>
        public Route Route;

        /// <summary>
        /// Files bound by this route.
        /// </summary>
        public DirectoryFilesGroup Files;
    }
}
