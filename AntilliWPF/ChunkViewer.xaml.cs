using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using DSCript.Spooling;

using FreeImageAPI;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for ChunkTest.xaml
    /// </summary>
    public partial class ChunkViewer : ObservableWindow
    {
        private bool inTextureMode;

        private BitmapSource _currentImage;
        private Spooler _currentSpooler;
        private Spooler _spoolerClipboard;
        
        protected string Filename { get; set; }
        
        protected bool IsDirty { get; set; }

        protected Spooler SpoolerClipboard
        {
            get { return _spoolerClipboard; }
            set
            {
                // dispose of orphan spoolers on the clipboard (if any)
                if (_spoolerClipboard != null && _spoolerClipboard.Parent == null)
                    _spoolerClipboard.Dispose();

                _spoolerClipboard = value;
            }
        }

        protected FileChunker ChunkFile { get; set; }

        public string WindowTitle
        {
            get
            {
                if (ChunkFile != null && !String.IsNullOrEmpty(Filename))
                {
                    var val = Filename;

                    if (IsDirty)
                        val += "*";

                    return String.Format("Chunk Viewer - {0}", val);
                }
                else
                {
                    return "Chunk Viewer";
                }
            }
        }
        
        protected SpoolablePackage LoadedChunk
        {
            get { return (ChunkFile != null) ? ChunkFile.Content : null; }
        }

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
            get { return _currentSpooler; }
            set
            {
                if (SetValue(ref _currentSpooler, value, "CurrentSpooler"))
                {
                    OnPropertyChanged("SpoolerInfo");
                }
            }
        }

        public BitmapSource CurrentImage
        {
            get { return _currentImage; }
            set { SetValue(ref _currentImage, value, "CurrentImage"); }
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

                sb.AppendColumn("Type",      col, true);

                if (magic > 255)
                    sb.AppendLine((ChunkType)magic);
                else
                    sb.AppendLine("Vehicle Container (ID:{0})", magic);

                sb.AppendColumn("Offset",    col, true).AppendLine("0x{0:X}", CurrentSpooler.BaseOffset);
                sb.AppendColumn("Reserved",  col, true).AppendLine(CurrentSpooler.Reserved);
                sb.AppendColumn("StrLen",    col, true).AppendLine(CurrentSpooler.StrLen);
                sb.AppendColumn("Alignment", col, true).AppendLine(1 << (int)CurrentSpooler.Alignment);
                sb.AppendColumn("Size",      col, true).AppendLine("0x{0:X}", CurrentSpooler.Size);

                if (CurrentSpooler.Magic == (int)ChunkType.BuildInfo)
                {
                    sb.AppendLine();

                    var buffer = ((SpoolableBuffer)CurrentSpooler).GetBuffer();
                    var encoding = (buffer.Length > 1 && buffer[1] == 0) ? Encoding.Unicode : Encoding.UTF8;

                    sb.AppendLine("Build Info:\r\n");
                    sb.Append(encoding.GetString(buffer));
                }

                return sb.ToString();
            }
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

                    if (ChunkFile != null)
                    {
                        DSC.Log("Collecting garbage...");
                        ChunkFile.Dispose();
                    }

                    ChunkFile = new DSCript.Spooling.FileChunker();

                    // capture events
                    ChunkFile.SpoolerLoaded += (o, e) => {
                        switch ((ChunkType)o.Magic)
                        {
                        case ChunkType.SpooledVehicleChunk:
                            {
                                if (o.Reserved != 0)
                                    break;

                                var vo3d = ((SpoolablePackage)o).Children[0] as SpoolableBuffer;
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

                    ChunkFile.Load(Filename);
                    OnPropertyChanged("Spoolers");

                    IsDirty = false;
                    OnPropertyChanged("WindowTitle");

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
            else
            {
                Filename = String.Empty;
            }
        }

        private bool CloseChunkFile()
        {
            var close = true;

            if (ChunkFile != null)
            {
                if (IsDirty)
                {
                    var result = MessageBox.Show("You have unsaved changes - do you wish to continue?", "Chunk Viewer", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    close = (result == MessageBoxResult.Yes);
                }

                if (close)
                {
                    ChunkFile.Dispose();
                    ChunkFile = null;
                }
            }

            OnPropertyChanged("WindowTitle");

            return close;
        }

        public void UpdateSpoolers()
        {
            OnPropertyChanged("Spoolers");
            IsDirty = true;

            OnPropertyChanged("WindowTitle");
        }

        private void LoadDDS()
        {
            var fibitmap = BitmapHelper.GetFIBITMAP(((SpoolableBuffer)CurrentSpooler).GetBuffer());
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
                var spooler = (SpoolableBuffer)CurrentSpooler;

                spooler.SetBuffer(File.ReadAllBytes(openFile.FileName));
                
                DSC.Log("Replaced buffer.");

                UpdateSpoolers();
            }
        }

        private void RemoveSpooler(object sender, RoutedEventArgs e)
        {
            if (CurrentSpooler.Parent == null && LoadedChunk.Children.Count == 1)
                MessageBox.Show("You cannot remove the root node!");
            else
            {
                // magic *snort snort*
                CurrentSpooler.Dispose();
                UpdateSpoolers();
            }
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
                UpdateSpoolers();
            }
        }

        public void SaveChunkFile()
        {
            if (IsDirty)
            {
                var bak = Filename + ".bak";
                var stopwatch = new Stopwatch();

                // make our backup
                File.Copy(Filename, bak, true);

                Mouse.OverrideCursor = Cursors.Wait;

                DSC.Log("Saving...");

                stopwatch.Start();
                ChunkFile.Save();
                stopwatch.Stop();

                IsDirty = false;
                OnPropertyChanged("WindowTitle");

                Mouse.OverrideCursor = null;
                DSC.Log("Successfully saved file in {0}ms.", stopwatch.ElapsedMilliseconds);
            }
            else
            {
                MessageBox.Show("No changes have been made!", "Chunk Viewer", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void ExportChunkFile()
        {
            var stopwatch = new Stopwatch();
            var filename = Path.Combine(Settings.Configuration.GetDirectory("Export"), Path.GetFileName(Filename));

            Mouse.OverrideCursor = Cursors.Wait;
            
            DSC.Log("Exporting...");
            
            stopwatch.Start();
            ChunkFile.Save(filename, false);
            stopwatch.Stop();

            IsDirty = false;
            OnPropertyChanged("WindowTitle");

            Mouse.OverrideCursor = null;
            DSC.Log("Exported file in {0}ms.", stopwatch.ElapsedMilliseconds);

            var msg = String.Format("Successfully exported to '{0}'!", filename);

            MessageBox.Show(msg, "Chunk Viewer", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportSpooler(object sender, RoutedEventArgs e)
        {
            bool chunk = (CurrentSpooler is SpoolablePackage);

            var saveDlg = new SaveFileDialog() {
                AddExtension = true,
                DefaultExt =  "." + ((chunk) ? "chunk" : Encoding.UTF8.GetString(BitConverter.GetBytes(CurrentSpooler.Magic)).ToLower()),
                InitialDirectory = Settings.Configuration.GetDirectory("Export"),
                Title = "Please enter a filename",
                ValidateNames = true,
                OverwritePrompt = true,
            };

            saveDlg.FileName = "export";

            if (saveDlg.ShowDialog() ?? false)
            {
                var stopwatch = new Stopwatch();

                DSC.Log("Exporting '{0}'...", saveDlg.FileName);
                Mouse.OverrideCursor = Cursors.Wait;

                stopwatch.Start();

                if (chunk)
                {
                    FileChunker.WriteChunk(saveDlg.FileName, (SpoolablePackage)CurrentSpooler);
                }
                else
                {
                    using (var fs = File.Create(saveDlg.FileName))
                    {
                        fs.SetLength(CurrentSpooler.Size);
                        fs.Write(((SpoolableBuffer)CurrentSpooler).GetBuffer());
                    }
                }

                stopwatch.Stop();

                IsDirty = false;
                OnPropertyChanged("WindowTitle");

                Mouse.OverrideCursor = null;
                DSC.Log("Successfully exported file in {0}ms!", stopwatch.ElapsedMilliseconds);

                var msg = String.Format("Successfully exported to '{0}'!", saveDlg.FileName);

                MessageBox.Show(msg, "Chunk Viewer", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SpoolerSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
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

        private void CutSpooler(object sender, RoutedEventArgs e)
        {
            if (CurrentSpooler != null)
            {
                var parent = CurrentSpooler.Parent;

                SpoolerClipboard = CurrentSpooler;

                parent.Children.Remove(CurrentSpooler);

                if (SpoolerClipboard != null && SpoolerClipboard.Parent == null)
                {
                    UpdateSpoolers();
                    DSC.Log("Clipboard set.");
                }
            }
        }

        private void PasteSpooler(object sender, RoutedEventArgs e)
        {
            if (CurrentSpooler != null && SpoolerClipboard != null)
            {
                if (CurrentSpooler is SpoolablePackage)
                {
                    ((SpoolablePackage)CurrentSpooler).Children.Add(SpoolerClipboard);
                }
                else
                {
                    var parent = CurrentSpooler.Parent;
                    var index = parent.Children.IndexOf(CurrentSpooler) + 1;

                    if (index < parent.Children.Count)
                    {
                        parent.Children.Insert(index, SpoolerClipboard);
                    }
                    else
                    {
                        parent.Children.Add(SpoolerClipboard);
                    }
                }

                UpdateSpoolers();
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

        protected override void OnClosing(CancelEventArgs e)
        {
            if (CloseChunkFile())
                base.OnClosing(e);
            else
            {
                // user canceled
                e.Cancel = true;
                base.OnClosing(e);
            }
        }

        public ChunkViewer()
        {
            InitializeComponent();

            fileOpen.Click += (o, e) => {
                OpenChunkFile();
            };

            fileSave.Click += (o, e) => {
                SaveChunkFile();
            };

            fileClose.Click += (o, e) => {
                if (CloseChunkFile())
                {
                    OnPropertyChanged("Spoolers");
                    IsDirty = false;
                }
            };

            fileExit.Click += (o, e) => {
                Close();
            };

            toolsExport.Click += (o, e) => {
                if (LoadedChunk != null)
                    ExportChunkFile();
            };

            KeyDown += (o, e) => {
                switch (e.Key)
                {
                case Key.T:
                    {
                        inTextureMode = !inTextureMode;
                        OnPropertyChanged("Spoolers");
                    } break;
                }
            };
        }

        
    }
}
