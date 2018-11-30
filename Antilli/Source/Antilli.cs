using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Input;
using System.Xml;

using Microsoft.Win32;

using FreeImageAPI;

using DSCript;
using DSCript.Models;
using DSCript.Spooling;

#if DEBUG
    using Logger = System.Diagnostics.Debug;
#else
    using Logger = System.Diagnostics.Trace;
#endif

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
        
        Resource        = (1 << 16),
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
    
    internal static class AT
    {
        public delegate void ObjectSelectedEventHandler(object selection, EventArgs e);

        public static CultureInfo CurrentCulture = new CultureInfo("en-US", false);

        public static void Log(string message)
        {
            Logger.WriteLine($"[ANTILLI] {message}");
        }

        public static void Log(string message, params object[] args)
        {
            Log(String.Format(message, args));
        }

        public static bool IsDevBuild
        {
            get
            {
            #if DEBUG
                return true;
            #else
                return false;
            #endif
            }
        }

        public struct StateData : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            
            public void NotifyChange(string property)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
            }

            private bool SetValue<T>(ref T backingField, T value, string propertyName)
            {
                if (object.Equals(backingField, value))
                    return false;

                backingField = value;
                NotifyChange(propertyName);
                return true;
            }
            
            int m_currentTab;
            
            public int CurrentTab
            {
                get { return m_currentTab; }
                set
                {
                    m_currentTab = value;
                    // TODO: Temporarily unload stuff?
                }
            }

            public bool IsModelViewerOpen
            {
                get { return CurrentTab == 0; }
            }

            public bool IsMaterialEditorOpen
            {
                get { return CurrentTab == 1; }
            }

            public bool IsTextureViewerOpen
            {
                get { return CurrentTab == 2; }
            }
            
            ModelFile m_modelFile;
            ModelPackage m_modelPackage;

            bool m_useGlobals;
            bool m_useBlendWeights;
            
            public ModelFile ModelFile
            {
                get { return m_modelFile; }
                set
                {
                    if (SetValue(ref m_modelFile, value, "CurrentModelFile"))
                    {
                        TextureCache.Flush();
                        NotifyChange("ModelPackages");
                    }
                }
            }
            
            public ModelPackage SelectedModelPackage
            {
                get { return m_modelPackage; }
                set
                {
                    m_modelPackage = value;

                    if (m_modelPackage != null)
                    {
                        // see if we need to load it
                        if (!m_modelPackage.HasModels)
                            m_modelPackage.GetInterface().Load();
                    }

                    NotifyChange("Materials");
                    NotifyChange("Textures");

                    if (CanUseGlobals)
                    {
                        NotifyChange("GlobalMaterials");
                        NotifyChange("GlobalTextures");
                    }

                    ModelPackageSelected?.Invoke(m_modelPackage, null);
                }
            }

            public List<ModelPackage> ModelPackages
            {
                get
                {
                    if (ModelFile != null)
                        return ModelFile.Models;

                    return null;
                }
            }
            
            public bool CanUseGlobals
            {
                get { return m_useGlobals; }
                set
                {
                    if (SetValue(ref m_useGlobals, value, "CanShowGlobals"))
                    {
                        NotifyChange("GlobalMaterials");
                        NotifyChange("GlobalTextures");
                        
                        NotifyChange("MatTexRowSpan");
                    }
                }
            }

            public bool CanUseBlendWeights
            {
                get { return m_useBlendWeights; }
                set { SetValue(ref m_useBlendWeights, value, "CanShowBlendWeights"); }
            }

            public Visibility CanShowGlobals
            {
                get { return CanUseGlobals ? Visibility.Visible : Visibility.Collapsed; }
            }

            public Visibility CanShowBlendWeights
            {
                get { return CanUseBlendWeights ? Visibility.Visible : Visibility.Collapsed; }
            }
            
            public EventHandler ModelPackageSelected;
            
            public EventHandler MaterialSelectQueried;
            public EventHandler TextureSelectQueried;

            public EventHandler FileModified;

            public void NotifyFileChange(object sender)
            {
                FileModified?.Invoke(sender, null);
            }
            
            public void QueryMaterialSelect(IMaterialData material)
            {
                MaterialSelectQueried?.Invoke(material, null);
            }

            public void QueryTextureSelect(ITextureData texture)
            {
                TextureSelectQueried?.Invoke(texture, null);
            }

            public bool UseDPLHacks { get; set; }
        }

        public static StateData CurrentState;

        public static string[] CommandLine { get; }
        
        static AT()
        {
            CommandLine = Environment.GetCommandLineArgs();
            CurrentState = new StateData();
        }
    }
    
    public static class FileManager
    {
        public static readonly GameFileFilter[] D3Filters = {
            new GameFileFilter("City|*.d3c",                GameFileFlags.Cities | GameFileFlags.Models | GameFileFlags.Textures),
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

        public static readonly GameFileFilter[] D4Filters = {
            new GameFileFilter("Animations|*.an4",          GameFileFlags.Animations),

            new GameFileFilter("City|*.d4c",                GameFileFlags.Cities | GameFileFlags.Models | GameFileFlags.Textures),
            new GameFileFilter("City litter|*.d4l",         GameFileFlags.Cities | GameFileFlags.Models | GameFileFlags.Textures),

            new GameFileFilter("Font|*.bnk",                GameFileFlags.Textures),

            new GameFileFilter("Minimap|*.map",             GameFileFlags.Models | GameFileFlags.Textures),
            new GameFileFilter("Overlay|*.gfx",             GameFileFlags.Textures),

            new GameFileFilter("Menu data|*.mec",           GameFileFlags.Textures),

            new GameFileFilter("Particle effects|*.pmu;*.ppx",
                                                            GameFileFlags.Models | GameFileFlags.Textures),

            new GameFileFilter("Sky|*.pkg",                 GameFileFlags.Models | GameFileFlags.Textures),

            new GameFileFilter("Spoolable Resource|*.sp",   GameFileFlags.Resource),
        };

        public static readonly GameFileFilter[] AllFilters = new GameFileFilter[] {
            new GameFileFilter(GameFileFilter.Combine("Driv3r", D3Filters)),
            new GameFileFilter(GameFileFilter.Combine("Driver: Parallel Lines", D4Filters)),
        };

        private static OpenFileDialog m_openDialog = null;

        public static OpenFileDialog OpenDialog
        {
            get
            {
                if (m_openDialog == null)
                {
                    var filter = String.Join("|", AllFilters.Select((f) => f.ToString()));

                    // TODO: support more games!
                    var gameDir = Driv3r.RootDirectory;
                    
                    m_openDialog = new OpenFileDialog() {
                        CheckFileExists = true,
                        CheckPathExists = true,
                        Filter = filter,
                        InitialDirectory = gameDir,
                        ValidateNames = true,
                    };
                }

                return m_openDialog;
            }
        }
        
        public static GameFileFilter FindFilter(string extension, GameFileFilter[] filters)
        {
            foreach (var filter in filters)
            {
                if (filter.HasExtension(extension))
                    return filter;
            }

            return GameFileFilter.GenericFilter;
        }
        
        public static GameFileFilter FindFilter(string extension, GameType gameType, GameFileFlags searchFlags = GameFileFlags.None)
        {
            var filter = GameFileFilter.GenericFilter;

            switch (gameType)
            {
            case GameType.Driv3r:
                filter = FindFilter(extension, D3Filters);
                break;
            case GameType.DriverPL:
                filter = FindFilter(extension, D4Filters);
                break;
            }

            if (searchFlags != GameFileFlags.None)
            {
                if ((filter.Flags & searchFlags) == GameFileFlags.None)
                    return GameFileFilter.GenericFilter;
            }

            return filter;
        }

        public static void WriteFile(string filename, byte[] buffer)
        {
            var dir = Path.GetDirectoryName(filename);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllBytes(filename, buffer);
        }
    }

    public static class Utils
    {
        public static bool TryParseNumber(string value, out int result)
        {
            result = -1;

            if (value == null)
                return false;
            
            if (value.Length > 2)
            {
                if (value.StartsWith("0x"))
                    return int.TryParse(value.Substring(2), NumberStyles.HexNumber, AT.CurrentCulture, out result);
            }

            return int.TryParse(value, out result);
        }
    }
}
