using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Windows;
using System.Xml;

using Microsoft.Win32;

using FreeImageAPI;

using DSCript;
using DSCript.Models;
using DSCript.Spooling;

namespace Antilli
{
    public enum GameType : int
    {
        None = 0,

        Driv3r,
        DriverPL,
        DriverSF,
    }

    [Flags]
    public enum GameFileFlags : int
    {
        None = 0,

        Animations      = (1 << 4),
        Characters      = (1 << 5),
        Cities          = (1 << 6),
        Missions        = (1 << 7),
        Models          = (1 << 8),
        Litter          = (1 << 9),
        Overlays        = (1 << 10),
        Shaders         = (1 << 11),
        Skies           = (1 << 12),
        Sounds          = (1 << 13),
        Textures        = (1 << 14),
        Vehicles        = (1 << 15),
    }

    public struct GameFileFilter
    {
        public static readonly GameFileFilter GenericFilter = new GameFileFilter("Any file|*.*");

        public readonly string Description;
        public readonly string[] Extensions;

        public readonly GameFileFlags Flags;

        public static implicit operator GameFileFilter(string value)
        {
            return new GameFileFilter(value);
        }

        public bool HasExtension(string extension)
        {
            if (String.IsNullOrEmpty(extension))
                return false;
            
            var subStr = (extension[0] == '.') ? 1 : 0;

            foreach (var ext in Extensions)
            {
                // HACK: assumes '*.' is prefixed in the extensions list!!!
                if (String.Equals(extension.Substring(subStr), ext.Substring(subStr + 1), StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }

        public static string Combine(GameFileFilter[] filters)
        {
            return Combine("All supported files", filters);
        }

        public static string Combine(string name, GameFileFilter[] filters)
        {
            // TODO: check for dupes?
            var exts = "";

            foreach (var filter in filters)
                exts += $"{String.Join(";", filter.Extensions)};";

            return $"{name}|{exts}";
        }

        public override string ToString()
        {
            return $"{Description}|{String.Join(";", Extensions)}";
        }

        public GameFileFilter(string filter)
        {
            var splitIdx = filter.IndexOf('|');

            Description = filter.Substring(0, splitIdx);
            Extensions = filter.Substring(splitIdx + 1).Split(';');

            Flags = GameFileFlags.None;
        }

        public GameFileFilter(string filter, GameFileFlags flags)
            : this(filter)
        {
            Flags = flags;
        }
    }
    
    public static class FileManager
    {
        public static readonly GameFileFilter[] D3Filters = new GameFileFilter[] {
            new GameFileFilter("City|*.d3c",                GameFileFlags.Cities),
            new GameFileFilter("City models|*.pcs",         GameFileFlags.Cities | GameFileFlags.Models | GameFileFlags.Textures),
                                                            
            new GameFileFilter("Font|*.bnk",                GameFileFlags.Textures),
                                                            
            new GameFileFilter("Minimap|*.map",             GameFileFlags.Models | GameFileFlags.Textures),
            new GameFileFilter("Overlay|*.gfx",             GameFileFlags.Textures),
                                                            
            new GameFileFilter("Menu data|*.mec",           GameFileFlags.Textures),
            new GameFileFilter("Mission models|*.dam",      GameFileFlags.Characters | GameFileFlags.Models | GameFileFlags.Textures),
                                                            
            new GameFileFilter("Particle effects|*.pmu",    GameFileFlags.Models | GameFileFlags.Textures),
                                                            
            new GameFileFilter("Sky|*.d3s",                 GameFileFlags.Models | GameFileFlags.Textures),
                                                            
            new GameFileFilter("Weapons|*.cpr",             GameFileFlags.Models | GameFileFlags.Textures),
                                                            
            new GameFileFilter("Vehicles|*.vvs;*.vvv",      GameFileFlags.Vehicles | GameFileFlags.Models | GameFileFlags.Textures),
            new GameFileFilter("Vehicle globals|*.vgt",     GameFileFlags.Vehicles | GameFileFlags.Textures),
        };

        public static readonly GameFileFilter[] AllFilters = new GameFileFilter[] {
            new GameFileFilter(GameFileFilter.Combine("DRIV3R", D3Filters)),
            //new GameFileFilter("Driver: Parallel Lines|*.sp;*.an4;*.d4c;*.d4l;*.chunk;*.mec;*.gfx;*.map;*.pmu;*.ppx;*.pkg*.bnk;", GameFileFlags.DriverPL),
        };

        public static readonly OpenFileDialog Driv3rOpenDialog = CreateOpenDialog(GameType.Driv3r);

        public static OpenFileDialog CreateOpenDialog(GameType gameType)
        {
            var filter = "All files|*.*";
            var gameDir = "";

            switch (gameType)
            {
            case GameType.Driv3r:
                filter = GameFileFilter.Combine(D3Filters);
                gameDir = Driv3r.RootDirectory;
                break;
            }

            return new OpenFileDialog() {
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = filter,
                InitialDirectory = gameDir,
                ValidateNames = true,
            };
        }
        
        public static GameFileFilter FindFilter(string extension, GameFileFilter[] filters, GameFileFlags searchFlags = GameFileFlags.None)
        {
            var checkFlags = (searchFlags != GameFileFlags.None);

            foreach (var filter in filters)
            {
                if (checkFlags && !filter.Flags.HasFlag(searchFlags))
                    continue;

                if (filter.HasExtension(extension))
                    return filter;
            }

            return GameFileFilter.GenericFilter;
        }
        
        public static GameFileFilter FindFilter(string extension, GameType gameType, GameFileFlags searchFlags = GameFileFlags.None)
        {
            switch (gameType)
            {
            case GameType.Driv3r:
                return FindFilter(extension, D3Filters, searchFlags);
            }

            return GameFileFilter.GenericFilter;
        }
    }
}
