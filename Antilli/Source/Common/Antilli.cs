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
        None = -1,

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
                    SetValue(ref m_currentTab, value, "CurrentTab");
                    // TODO: Temporarily unload stuff?
                }
            }

            public MainWindow ModelView { get; set; }

            public MaterialsView MaterialEditor { get; set; }
            public TexturesView TextureEditor { get; set; }

            public bool IsFileOpened
            {
                get { return ModelFile != null; }
            }

            bool m_isFileDirty;

            public bool IsFileDirty
            {
                get { return m_isFileDirty; }
                set
                {
                    if (SetValue(ref m_isFileDirty, value, "IsFileDirty"))
                    {
                        if (IsFileOpened)
                        {
                            var filename = ModelFile.FileName;

                            ModelView.SubTitle = (m_isFileDirty) ? $"{filename} *" : filename;
                        }

                        UpdateFileStatus();
                    }
                }
            }

            public bool AreChangesPending
            {
                get { return IsFileOpened && (IsFileDirty || ModelFile.AreChangesPending); }
            }

            public bool CanSaveFile
            {
                get { return IsFileOpened && AreChangesPending; }
            }

            public void UpdateFileStatus()
            {
                NotifyChange("IsFileOpened");
                NotifyChange("AreChangesPending");
                NotifyChange("CanSaveFile");
            }

            public void ResetEditors()
            {
                MaterialEditor.ResetView();
                TextureEditor.ResetView();
            }

            public void UpdateEditors()
            {
                MaterialEditor.UpdateView();
                TextureEditor.UpdateView();
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
            
            IModelFile m_modelFile;
            ModelPackage m_modelPackage;

            bool m_unregisterModelPackage;

            bool m_useGlobals;
            bool m_useBlendWeights;

            public AntilliClient BlenderClient { get; set; }
            
            public IModelFile ModelFile
            {
                get { return m_modelFile; }
                set
                {
                    if (SetValue(ref m_modelFile, value, "CurrentModelFile"))
                    {
                        TextureCache.Flush(true);
                        NotifyChange("ModelPackages");
                    }
                }
            }
            
            public ModelPackage SelectedModelPackage
            {
                get { return m_modelPackage; }
                set
                {
                    if (ReferenceEquals(m_modelPackage, value))
                        return;

                    if (m_modelPackage != null)
                    {
                        if (m_unregisterModelPackage)
                        {
                            PackageManager.UnRegister(m_modelPackage);
                            m_unregisterModelPackage = false;
                        }

                        if (!PackageManager.IsRegistered(m_modelPackage))
                        {
                            if (m_modelPackage.HasModels)
                                m_modelPackage.FreeModels();
                            if (m_modelPackage.HasMaterials)
                                m_modelPackage.FreeMaterials();
                        }
                    }

                    m_modelPackage = value;

                    if (m_modelPackage != null)
                    {
                        // see if we need to load it
                        if (!m_modelPackage.HasModels)
                        {
                            SpoolableResourceFactory.Load(m_modelPackage);
                        }
                        else
                        {
                            // reload the materials
                            if (!m_modelPackage.HasMaterials)
                            {
                                var level = ModelPackage.LoadLevel;
                                ModelPackage.LoadLevel = ModelPackageLoadLevel.Materials;

                                SpoolableResourceFactory.Load(m_modelPackage);

                                ModelPackage.LoadLevel = level;
                            }
                        }
                    }

                    if (PackageManager.IsRegisterable(m_modelPackage))
                    {
                        PackageManager.Register(m_modelPackage);
                        m_unregisterModelPackage = (m_modelPackage.UID != 0); // stupid hacks
                    }

                    NotifyChange("Materials");
                    NotifyChange("Textures");
                    
                    if (CanUseGlobals)
                    {
                        NotifyChange("GlobalMaterials");
                        NotifyChange("GlobalTextures");
                    }

                    // reset the editors
                    ResetEditors();

                    ModelPackageSelected?.Invoke(m_modelPackage, null);
                }
            }

            public List<ModelPackage> ModelPackages
            {
                get
                {
                    if (ModelFile != null)
                        return ModelFile.Packages;

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
                set { SetValue(ref m_useBlendWeights, value, "CanUseBlendWeights"); }
            }


            public EventHandler ModelPackageSelected;
            
            public EventHandler MaterialSelectQueried;
            public object MaterialSelectQueryResult;

            public EventHandler TextureSelectQueried;

            public EventHandler FileModified;

            public void NotifyFileChange(object sender)
            {
                FileModified?.Invoke(sender, null);
            }

            public void FreeModels()
            {
                if (ModelFile != null)
                {
                    SelectedModelPackage = null;
                    CanUseGlobals = false;
                    CanUseBlendWeights = false;

                    ModelFile.Dispose();
                    ModelFile = null;

                    TextureCache.Flush(true);
                    PackageManager.Clear();

                    ModelView.Viewer.ClearModels();

                    ResetEditors();
                }
            }
            
            public void QueryMaterialSelect(IMaterialData material)
            {
                MaterialSelectQueried?.Invoke(material, null);
            }

            public void QueryTextureSelect(ITextureData texture)
            {
                TextureSelectQueried?.Invoke(texture, null);
            }

            /*
                This will check the globals (if applicable),
                then the current model package for the material/texture
            */
            public bool OnQuerySelection<TQueryInput, TQueryObject>(Func<TQueryInput, TQueryObject, bool> fnQuery,
                TQueryInput queryGlobals, TQueryInput queryOther, object obj, int tabIdx = -1)
                where TQueryInput : class
                where TQueryObject : class
            {
                var queryObj = obj as TQueryObject;

                if (queryObj != null)
                {
                    if (tabIdx != -1)
                        CurrentTab = tabIdx;

                    return (CanUseGlobals && fnQuery(queryGlobals, queryObj))
                    || fnQuery(queryOther, queryObj);
                }

                // no material!
                return false;
            }

            public void HandleKeyDown(object sender, KeyEventArgs e)
            {
                switch (CurrentTab)
                {
                case 1:
                    MaterialEditor.HandleKeyDown(sender, e);
                    break;
                case 2:
                    TextureEditor.HandleKeyDown(sender, e);
                    break;
                }
            }

            public Window MainWindow { get; set; }

            private Dictionary<Type, Window> m_dialogs;

            public T CreateDialog<T>(bool modal, bool show = true)
                where T : Window, new()
            {
                if (m_dialogs == null)
                    m_dialogs = new Dictionary<Type, Window>();

                var wndType = typeof(T);

                var window = MainWindow;
                var dialogs = m_dialogs;

                T dialog = null;

                if (modal && dialogs.ContainsKey(wndType))
                {
                    dialog = (T)dialogs[wndType];

                    if (show)
                        dialog.Activate();
                }
                else
                {
                    dialog = new T()
                    {
                        Owner = MainWindow,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    };

                    if (modal)
                    {
                        dialog.Closed += (o, e) => {
                            dialogs.Remove(wndType);
                        };

                        dialogs.Add(wndType, dialog);
                    }

                    if (show)
                        dialog.Show();
                }

                dialog.Closed += (o, e) => {
                    // focus main window so everything doesn't disappear
                    window.Focus();
                };

                return dialog;
            }
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
        private static string m_rootDir = String.Empty;
        
        public static string RootDirectory
        {
            get { return m_rootDir; }
            set { m_rootDir = value; }
        }

        public static OpenFileDialog OpenDialog
        {
            get
            {
                if (m_openDialog == null)
                {
                    var filter = String.Join("|", AllFilters.Select((f) => f.ToString()));
                    
                    m_openDialog = new OpenFileDialog() {
                        CheckFileExists = true,
                        CheckPathExists = true,
                        Filter = filter,
                        InitialDirectory = RootDirectory,
                        ValidateNames = true,
                    };
                }

                return m_openDialog;
            }
        }

        public static string GetDirectory(GameType gameType, bool verify = false)
        {
            string[] names = {
                "Driv3r",
                "DriverPL",
                "DriverSF",
            };

            var name = names[(int)gameType];

            if (verify)
                DSC.VerifyGameDirectory(name, "Antilli");

            return DSC.Configuration.GetDirectory(name);
        }
        
        public static OpenFileDialog GetOpenDialog(GameType gameType)
        {
            var dlg = new OpenFileDialog() {
                CheckFileExists = true,
                CheckPathExists = true,

                InitialDirectory = Environment.CurrentDirectory,

                ValidateNames = true,
            };

            if (gameType != GameType.None)
            {
                var gameDir = GetDirectory(gameType);

                if (!String.IsNullOrEmpty(gameDir))
                    dlg.InitialDirectory = gameDir;

                switch (gameType)
                {
                case GameType.Driv3r:
                case GameType.DriverPL:
                    dlg.Filter = AllFilters[(int)gameType].ToString();
                    break;
                }
            }

            return dlg;
        }

        public static OpenFileDialog GetOpenDialog(GameType gameType, string extension)
        {
            var dlg = new OpenFileDialog()
            {
                CheckFileExists = true,
                CheckPathExists = true,

                InitialDirectory = Environment.CurrentDirectory,

                ValidateNames = true,
            };

            if (gameType != GameType.None)
            {
                var gameDir = GetDirectory(gameType);

                if (!String.IsNullOrEmpty(gameDir))
                    dlg.InitialDirectory = gameDir;

                dlg.Filter = FindFilter(extension, gameType).ToString();
            }

            return dlg;
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

        public static bool TryGetImageFormat(byte[] buffer, out string result)
        {
            var magic = BitConverter.ToInt32(buffer, 0);

            switch (magic)
            {
            // iffy at best
            case 0x20000:
            case 0xA0000:
                result = "tga";
                return true;
            case 0x364D42:
            case 0x384D42:
            case 0x10364D42:
            case 0x10384D42:
                result = "bmp";
                return true;
            case 0x20534444:
                result = "dds";
                return true;
            case 0x20AF30:
                result = "tpl";
                return true;
            }

            // unknown
            result = "";
            return false;
        }
    }
}
