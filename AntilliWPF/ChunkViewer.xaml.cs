using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.Win32;

using DSCript;
using DSCript.Spoolers;
using DSCript.Spooling;
using DSC = DSCript.DSC;

using FreeImageAPI;

using Spooler = DSCript.Spooling.Spooler;
using SpoolableChunk = DSCript.Spooling.SpoolablePackage;
using SpoolableData = DSCript.Spooling.SpoolableBuffer;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for ChunkTest.xaml
    /// </summary>
    public partial class ChunkViewer : ObservableWindow
    {
        private string Filename { get; set; }

        private SpoolableChunk loadedChunk;
        private FileChunker _chunker;
        private bool inTextureMode;

        protected SpoolableChunk LoadedChunk
        {
            get { return loadedChunk; }
            set
            {
                if (loadedChunk != null)
                {
                    DSC.Log("Collecting garbage...");

                    loadedChunk.Dispose();

                    GC.Collect();
                }

                refreshSpoolers = true;

                loadedChunk = value;
            }
        }

        private Spooler currentSpooler;
        private BitmapSource currentImage;
        
        private bool refreshSpoolers = false;

        //-- Don't need these anymore
        //private IEnumerable<Spooler> allSpoolers;
        //private IEnumerable<Spooler> AllSpoolers
        //{
        //    get
        //    {
        //        if (allSpoolers == null || refreshSpoolers)
        //        {
        //            var stopwatch = new Stopwatch();
        //            stopwatch.Start();
        //
        //            allSpoolers = LoadedChunk.GetAllSpoolers();
        //
        //            stopwatch.Stop();
        //
        //            DSC.Log("Took {0}ms to flatten {1:N0} spoolers.", stopwatch.Elapsed.TotalMilliseconds, allSpoolers.Count());
        //        }
        //
        //        refreshSpoolers = false;
        //
        //        return allSpoolers;
        //    }
        //}
        //
        //private SpoolableChunk FindParent(Spooler child)
        //{
        //    SpoolableChunk parent = null;
        //
        //    var chunks = AllSpoolers.Where((s) => (s is SpoolableChunk && s != child));
        //
        //    if (chunks != null)
        //    {
        //        var timer = new Stopwatch();
        //        timer.Start();
        //
        //        foreach (SpoolableChunk chunk in chunks)
        //        {
        //            if (chunk.Spoolers.Contains(child))
        //            {
        //                parent = chunk;
        //                break;
        //            }
        //        }
        //
        //        timer.Stop();
        //
        //        DSC.Log("Searched through {0:N0} chunks in {1}ms.", chunks.Count(), timer.Elapsed.TotalMilliseconds);
        //    }
        //
        //    return parent;
        //}

        public IList<Spooler> Spoolers
        {
            get
            {
                if (LoadedChunk == null)
                    return null;

                //var dxtc = (int)ChunkType.ResourceTextureDDS;
                //var spoolers = (inTextureMode) ? AllSpoolers.Where((s) => s.Magic == dxtc) : LoadedChunk.Children;
                //
                //if (inTextureMode && spoolers.Count() == 0)
                //{
                //    MessageBox.Show("There are no textures available to load!", "Chunk Viewer", MessageBoxButton.OK, MessageBoxImage.Information);
                //
                //    inTextureMode = false;
                //
                //    spoolers = LoadedChunk.Spoolers;
                //}

                return new List<Spooler>(LoadedChunk.Children);
            }
        }

        public Spooler CurrentSpooler
        {
            get { return currentSpooler; }
            set
            {
                if (SetValue(ref currentSpooler, value, "CurrentSpooler"))
                {
                    OnPropertyChanged("SpoolerInfo");
                }
            }
        }

        public BitmapSource CurrentImage
        {
            get { return currentImage; }
            set { SetValue(ref currentImage, value, "CurrentImage"); }
        }

        public string SpoolerInfo
        {
            get
            {
                if (CurrentSpooler == null)
                    return "";

                var sb = new StringBuilder();
                var col = 10;
                var magic = CurrentSpooler.Magic;

                sb.AppendColumn("Alignment", col, true).AppendLine(CurrentSpooler.Alignment);
                sb.AppendColumn("Type",      col, true);

                if (magic > 255)
                    sb.AppendLine((ChunkType)magic);
                else
                    sb.AppendLine("Vehicle Container (ID:{0})", magic);

                sb.AppendColumn("Offset",    col, true).AppendLine("0x{0:X}", CurrentSpooler.BaseOffset);
                sb.AppendColumn("Reserved",  col, true).AppendLine(CurrentSpooler.Reserved);
                sb.AppendColumn("StrLen",    col, true).AppendLine(CurrentSpooler.StrLen);
                sb.AppendColumn("Flags",     col, true).AppendLine("0x{0:X2} / 0x{1:X2}", (CurrentSpooler.Flags & 0xFF), ((CurrentSpooler.Flags >> 8) & 0xFF));
                sb.AppendColumn("Size",      col, true).AppendLine("0x{0:X}", CurrentSpooler.Size);

                if (CurrentSpooler.Magic == (int)ChunkType.BuildInfo)
                {
                    sb.AppendLine();

                    var buffer = ((SpoolableData)CurrentSpooler).GetBuffer();
                    var encoding = (buffer.Length > 1 && buffer[1] == 0) ? Encoding.Unicode : Encoding.UTF8;

                    sb.AppendLine("Build Info:\r\n");
                    sb.Append(encoding.GetString(buffer));
                }

                return sb.ToString();
            }
        }

        private void SpoolerSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //var target = e.NewValue as Spooler;
            //CurrentSpooler = (target != null) ? AllSpoolers.First((s) => s == target) : null;

            CurrentSpooler = e.NewValue as Spooler;

            if (CurrentSpooler != null)
            {
                if (CurrentSpooler.Magic == (int)ChunkType.ResourceTextureDDS)
                {
                    LoadDDS();
                    return;
                }
            }
            
            if (CurrentImage != null)
                CurrentImage = null;
        }

        private void LoadDDS()
        {
            var fibitmap = BitmapHelper.GetFIBITMAP(((SpoolableData)CurrentSpooler).GetBuffer());
            var fibitmap24 = FreeImage.ConvertTo24Bits(fibitmap);

            var isNull = fibitmap.Unload();

            Debug.Assert(isNull, "MEMORY LEAK!!!", "The FIBITMAP was not released properly!");

            using (var bitmap = fibitmap24.ToBitmap(true))
            {
                CurrentImage = (bitmap != null) ? bitmap.ToBitmapSource() : null;
            }
        }

        private void ReplaceBuffer(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog() {
                CheckFileExists = true,
                CheckPathExists = true,
                ValidateNames = true
            };

            if (openFile.ShowDialog() ?? false)
            {
                var spooler = (SpoolableData)CurrentSpooler;

                spooler.SetBuffer(File.ReadAllBytes(openFile.FileName));
                
                DSC.Log("Replaced buffer.");

                OnPropertyChanged("Spoolers");
            }

            refreshSpoolers = true;
        }

        private void RemoveCurrentSpoolerFromParent(SpoolableChunk parent)
        {
            CurrentSpooler.Dispose();
            //parent.Spoolers.Remove(CurrentSpooler);

            OnPropertyChanged("Spoolers");
        }

        private void RemoveSpooler(object sender, RoutedEventArgs e)
        {
            if (CurrentSpooler.Parent == null && LoadedChunk.Children.Count == 1)
                MessageBox.Show("You cannot remove the root node!");
            else
            {
                // magic *snort snort*
                CurrentSpooler.Dispose();
                OnPropertyChanged("Spoolers");
            }

            //var spoolers = LoadedChunk.Spoolers;
            //
            //if (!spoolers.Contains(CurrentSpooler))
            //{
            //    var parent = FindParent(CurrentSpooler);
            //
            //    if (parent != null)
            //    {
            //        RemoveCurrentSpoolerFromParent(parent);
            //    }
            //    else
            //    {
            //        MessageBox.Show("FATAL ERROR: Parent is null!");
            //    }
            //}
            //else
            //{
            //    if (spoolers.Count >= 2)
            //    {
            //        RemoveCurrentSpoolerFromParent(LoadedChunk);
            //    }
            //    else
            //    {
            //        MessageBox.Show("You cannot remove the root node!");
            //    }
            //}
        }

        public void ExportChunkFile()
        {
            var stopwatch = new Stopwatch();
            Mouse.OverrideCursor = Cursors.Wait;
            
            DSC.Log("Exporting...");
            //Chunk.PaddingType = Chunk.BytePaddingType.PaddingType2;

            stopwatch.Start();
            _chunker.Save(Path.Combine(Settings.Configuration.GetDirectory("Export"), Path.GetFileName(Filename)));
            stopwatch.Stop();

            Mouse.OverrideCursor = null;
            DSC.Log("Exported file in {0}ms.", stopwatch.ElapsedMilliseconds); 
        }

        public void OpenChunkFile()
        {
            var openFile = new OpenFileDialog() {
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "All Files|*.*",
                InitialDirectory = Driv3r.RootDirectory,
                ValidateNames = true,
            };

            if (openFile.ShowDialog() ?? false)
            {
                Filename = openFile.FileName;

                try
                {
                    var stopwatch = new System.Diagnostics.Stopwatch();
                    
                    Mouse.OverrideCursor = Cursors.Wait;
 
                    //var file = new DSCript.Spoolers.SpoolableChunk(Filename);

                    
                    if (_chunker != null)
                    {
                        DSC.Log("Collecting garbage...");
                        _chunker.Dispose();
                    }

                    _chunker = new DSCript.Spooling.FileChunker();

                    // capture events
                    _chunker.SpoolerLoaded += (o, e) => {
                        switch ((ChunkType)o.Magic)
                        {
                        case ChunkType.SpooledVehicleChunk:
                            {
                                if (o.Reserved != 0)
                                    break;

                                var vo3d = ((SpoolableChunk)o).Children[0] as SpoolableBuffer;
                                var buffer = BitConverter.ToInt16(vo3d.GetBuffer(), 0);

                                var vehId = buffer & 0xFF;
                                var modLvl = (buffer & 0x7000) / 0x1000;

                                // VehicleName [optional:(Bodykit #N)]
                                o.Description = String.Format("{0}{1}", DriverPL.VehicleNames[vehId], (modLvl > 0) ? String.Format(" (Bodykit #{0})", modLvl) : "");
                            } break;
                        }
                    };

                    DSC.Log("Loading...");

                    stopwatch.Start();

                    _chunker.Load(Filename);

                    //LoadedChunk = file;
                    LoadedChunk = _chunker.Content;
                    OnPropertyChanged("Spoolers");

                    stopwatch.Stop();

                    Mouse.OverrideCursor = null;

                    DSC.Log("Loaded {0} chunks in {1}s.", LoadedChunk.Children.Count, stopwatch.Elapsed.TotalSeconds);
                    DSC.Log("Temp directory size: {0:N0} KB", DSC.GetTempDirectorySize() / 1024.0);
                }
                catch (FileFormatException e)
                {
                    Mouse.OverrideCursor = null;
                    MessageBox.Show(e.Message, "Chunk Viewer", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportSpooler(object sender, RoutedEventArgs e)
        {
#if OLDEXPORT
            var ext = (CurrentSpooler.Magic > 255)
                        ? Encoding.UTF8.GetString(BitConverter.GetBytes(CurrentSpooler.Magic))
                        : CurrentSpooler.Magic.ToString("X");

            // DSF: Change 'DXTC' magic to 'dds'
            if (ext == "DXTC")
                ext = "dds";

            var filename = String.Format("{0}_{1}_{2}.{3}",
                Path.GetFileName(Filename).Replace('.', '_'),
                CurrentSpooler.GetHashCode() - 1, // minus 'LoadedChunk' instance
                CurrentSpooler.Size,
                ext);

            filename = Path.Combine(Settings.Configuration.GetDirectory("Export"), filename);

            CurrentSpooler.Save(filename);

            MessageBox.Show(String.Format("Successfully exported '{0}'!", filename), "Chunk Viewer", MessageBoxButton.OK, MessageBoxImage.Information);
#else
            bool chunk = (CurrentSpooler is SpoolablePackage);

            var openFile = new SaveFileDialog() {
                AddExtension = true,
                DefaultExt =  "." + ((chunk) ? "chunk" : Encoding.UTF8.GetString(BitConverter.GetBytes(CurrentSpooler.Magic)).ToLower()),
                InitialDirectory = Settings.Configuration.GetDirectory("Export"),
                Title = "Please enter a filename",
                ValidateNames = true,
                OverwritePrompt = true,
            };

            openFile.FileName = "export";

            if (openFile.ShowDialog() ?? false)
            {
                var stopwatch = new Stopwatch();

                DSC.Log("Exporting '{0}'...", openFile.FileName);
                Mouse.OverrideCursor = Cursors.Wait;

                stopwatch.Start();

                //if (chunk)
                //{
                //    FileChunker.WriteChunk(openFile.FileName, (SpoolablePackage)CurrentSpooler);
                //}
                //else
                //{
                //    using (var fs = File.Create(openFile.FileName))
                //    {
                //        fs.SetLength(CurrentSpooler.Size);
                //        fs.Write(((SpoolableBuffer)CurrentSpooler).GetBuffer());
                //    }
                //}

                UnifiedPackage.Create(openFile.FileName, CurrentSpooler);

                stopwatch.Stop();

                Mouse.OverrideCursor = null;
                DSC.Log("Successfully exported file in {0}ms!", stopwatch.ElapsedMilliseconds);
            }
#endif
        }

        private void RenameSpooler(object sender, RoutedEventArgs e)
        {
            var inputBox = new MKInputBox("Edit Description", "Please enter a new description:", CurrentSpooler.Description) {
                Owner = this,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
            };

            if (inputBox.ShowDialog() ?? false)
            {
                CurrentSpooler.Description = inputBox.InputValue;
                OnPropertyChanged("Spoolers");
            }
        }

        private void TreeViewItem_MouseRightButtonDown(object sender, MouseEventArgs e)
        {
            var item = sender as TreeViewItem;

            if (item != null)
            {
                item.IsSelected = true;
                item.Focus();

                e.Handled = true;
            }
        }

        public ChunkViewer()
        {
            InitializeComponent();

            KeyDown += (o, e) => {
                switch (e.Key)
                {
                case Key.E:
                    if (LoadedChunk != null)
                        ExportChunkFile();
                    break;
                case Key.O: OpenChunkFile();
                    break;
                case Key.T:
                    {
                        inTextureMode = !inTextureMode;
                        OnPropertyChanged("Spoolers");
                    } break;
                default: break;
                }
            };

            Closing += (o, e) => {
                if (LoadedChunk != null)
                    LoadedChunk.Dispose();
            };
        }
    }
}
