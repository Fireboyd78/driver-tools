﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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
        private bool _inTextureMode;
        private bool _isDirty;

        private FileChunker _chunkFile;
        private BitmapSource _currentImage;
        private Spooler _spoolerClipboard;

        private SpoolerListItem _currentSpoolerItem;
        
        protected string FileName
        {
            get
            {
                return (IsChunkFileOpen) ? ChunkFile.FileName : String.Empty;
            }
        }

        protected bool IsDirty
        {
            get { return _isDirty; }
            set
            {
                _isDirty = value;

                OnPropertyChanged("CanSaveChunkFile");
                OnPropertyChanged("WindowTitle");
            }
        }

        protected Spooler SpoolerClipboard
        {
            get { return _spoolerClipboard; }
            set
            {
                // dispose of orphan spoolers on the clipboard (if any)
                if (_spoolerClipboard != null && _spoolerClipboard.Parent == null)
                    _spoolerClipboard.Dispose();

                _spoolerClipboard = value;

                OnPropertyChanged("CanPasteSpooler");
            }
        }

        protected FileChunker ChunkFile
        {
            get { return _chunkFile; }
            set
            {
                // NOTE: This will NOT take care of unsaved changes!
                if (_chunkFile != null)
                {
                    _chunkFile.Dispose();
                    _chunkFile = null;
                }

                _chunkFile = value;

                OnPropertyChanged("IsChunkFileOpen");
            }
        }

        public string AppTitle
        {
            get { return "Chunk Editor"; }
        }

        public string WindowTitle
        {
            get
            {
                if (IsChunkFileOpen)
                {
                    var file = ChunkFile.FileName;

                    if (IsDirty)
                        file += "*";

                    return $"{AppTitle} - {file}";
                }

                return $"{AppTitle}";
            }
        }
        
        protected SpoolablePackage LoadedChunk
        {
            get { return (ChunkFile != null) ? ChunkFile.Content : null; }
        }

        private Thread m_filterThread = null;
        private volatile string m_filterString = "";
        private volatile int m_filterTimeout = 750;
        
        private bool IsFilterApproved(Spooler spooler)
        {
            return String.IsNullOrEmpty(SearchFilter) || (spooler.Context.CompareTo(SearchFilter) != -1);
        }

        private IEnumerable<Spooler> EnumerateFilteredSpoolers(SpoolablePackage root)
        {
            foreach (var c in root.Children)
            {
                if (c is SpoolablePackage)
                {
                    var pkg = (SpoolablePackage)c;

                    if (IsFilterApproved(pkg))
                    {
                        yield return pkg;
                    }
                    else
                    {
                        foreach (var cc in EnumerateFilteredSpoolers(pkg))
                            yield return cc;
                    }
                }
                else
                {
                    if (IsFilterApproved(c))
                        yield return c;
                }
            }

            yield break;
        }

        private void SpoolerItem_Expanded(object sender, RoutedEventArgs e)
        {
            var targetNode = e.OriginalSource as SpoolerListItem;

            if (targetNode != null && !targetNode.FullyLoaded)
            {
                // delay expansion so we don't run out of memory ;)
                foreach (var item in targetNode.Items.OfType<SpoolerListItem>())
                {
                    // skip ones that don't need expansion
                    if (item.FullyLoaded)
                        continue;

                    var spoo = item.Spooler as SpoolablePackage;

                    // we only care for spooled packages
                    if (spoo == null)
                        continue;

                    // are all of the children present?
                    if (item.Items.Count != spoo.Children.Count)
                    {
                        // add the delayed child nodes
                        foreach (var s in spoo.Children)
                            AddSpoolerToNode(s, item);
                    }
                }

                // delayed expansion complete! :)
                targetNode.Expanded -= SpoolerItem_Expanded;
                targetNode.FullyLoaded = true;
            }
        }

        private void AddSpoolerToNode(Spooler spooler, ItemsControl node)
        {
            var childNode = new SpoolerListItem(spooler);

            if (spooler is SpoolablePackage)
            {
                var package = (SpoolablePackage)spooler;

                // populate the "fake" child nodes (only expand them as needed)
                foreach (var child in package.Children)
                {
                    var fakeNode = new SpoolerListItem(child);

                    if (child is SpoolablePackage)
                    {
                        var c = (SpoolablePackage)child;

                        // unless it's an empty chunk, it will require delayed expansion
                        fakeNode.FullyLoaded = (c.Children.Count == 0);
                    }
                    else
                    {
                        fakeNode.FullyLoaded = true;
                    }

                    childNode.Items.Add(fakeNode);
                }

                // delay recursion until user opens node for the first time
                childNode.Expanded += SpoolerItem_Expanded;
            }
            else
            {
                // no further action needed
                childNode.FullyLoaded = true;
            }

            node.Items.Add(childNode);
        }

        private Stack<SpoolablePackage> ParentQueue = null;

        private void ProcessFilterQueue(Stack<SpoolablePackage> stack)
        {
            Debug.WriteLine($"Processing filter queue...");

            var child = stack.Pop();
            var items = ChunkList.Items;

            while (items != null)
            {
                SpoolerListItem itemNode = null;

                foreach (var item in items.OfType<SpoolerListItem>())
                {
                    if (ReferenceEquals(item.Spooler, child))
                    {
                        itemNode = item;
                        items = itemNode.Items;
                        break;
                    }
                }

                if (itemNode == null)
                    break;

                itemNode.IsExpanded = true;
                itemNode.IsSelected = true;

                child = (stack.Count > 0) ? stack.Pop() : null;

                if (child == null)
                    break;
            }
        }

        private void UpdateSpoolers()
        {
            Dispatcher.VerifyAccess();

            Debug.WriteLine($"Updating spoolers using search filter '{m_filterString}'...");

            // remove any previous items
            ChunkList.Items.Clear();
            ChunkList.UpdateLayout();

            if (LoadedChunk != null)
            {
                // populate our new items
                foreach (var c in EnumerateFilteredSpoolers(LoadedChunk))
                    AddSpoolerToNode(c, ChunkList);

                if (ParentQueue != null)
                {
                    ProcessFilterQueue(ParentQueue);
                    ParentQueue = null;
                }
            }
        }

        private void DelayUpdateSpoolers()
        {
            while (m_filterTimeout-- > 0)
                Thread.Sleep(1);

            Dispatcher.Invoke((Action)UpdateSpoolers);
        }
        
        private void QueueFilterUpdate(string filter)
        {
            m_filterString = filter;
            m_filterTimeout = 750;

            if ((m_filterThread == null) || !m_filterThread.IsAlive)
            {
                m_filterThread = new Thread(DelayUpdateSpoolers) { IsBackground = true };
                m_filterThread.Start();
            }   
        }

        public string SearchFilter
        {
            get { return m_filterString; }
            set
            {
                m_filterString = value;
                UpdateSpoolers();
            }
        }

        public Visibility CanFindSpoolerParent
        {
            get
            {
                if (!String.IsNullOrEmpty(SearchFilter))
                {
                    if (CurrentSpooler != null && CurrentSpooler.Parent != null)
                        return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
        }

        public Visibility CanReplaceSpooler
        {
            get
            {
                return (CanModifySpooler && CurrentSpooler is SpoolableBuffer) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public bool CanModifySpooler
        {
            get { return (CurrentSpooler != null) && !ChunkFile.IsCompressed; }
        }

        public bool CanPasteSpooler
        {
            get { return (CurrentSpooler != null && SpoolerClipboard != null); }
        }

        public bool CanSaveChunkFile
        {
            get { return IsDirty; }
        }

        public bool IsChunkFileOpen
        {
            get { return (ChunkFile != null); }
        }

        public SpoolerListItem CurrentSpoolerItem
        {
            get { return _currentSpoolerItem; }
            set
            {
                if (SetValue(ref _currentSpoolerItem, value, "CurrentSpoolerItem"))
                {
                    OnPropertyChanged("CurrentSpooler");

                    OnPropertyChanged("SpoolerInfo");
                    OnPropertyChanged("CanModifySpooler");
                    OnPropertyChanged("CanReplaceSpooler");
                    OnPropertyChanged("CanFindSpoolerParent");
                }
            }
        }

        public Spooler CurrentSpooler
        {
            get
            {
                return (CurrentSpoolerItem != null) ? CurrentSpoolerItem.Spooler : null;
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
                var magic = CurrentSpooler.Context;

                sb.AppendColumn("Type",      col, true);

                if (magic == 0 || magic > 255)
                    sb.AppendLine((ChunkType)magic);
                else
                    sb.AppendLine("Vehicle Container (ID:{0})", magic);

                sb.AppendColumn("Offset",       col, true).AppendLine("0x{0:X}", CurrentSpooler.BaseOffset);
                sb.AppendColumn("Version",      col, true).AppendLine(CurrentSpooler.Version);
                sb.AppendColumn("Alignment",    col, true).AppendLine(1 << (int)CurrentSpooler.Alignment);
                sb.AppendColumn("Size",         col, true).AppendLine("0x{0:X}", CurrentSpooler.Size);

                if (CurrentSpooler.Context == ChunkType.BuildInfo)
                {
                    sb.AppendLine();

                    var buffer = ((SpoolableBuffer)CurrentSpooler).GetBuffer();
                    var encoding = (buffer.Length > 1 && buffer[1] == 0) ? Encoding.Unicode : Encoding.UTF8;

                    sb.AppendLine("Build Info:\r\n");
                    sb.Append(encoding.GetString(buffer));
                }
                else if (CurrentSpooler.Context == 0x444D4244) // DBMD
                {
                    sb.AppendLine("\r\n"); // 2 newlines

                    var buffer = ((SpoolableBuffer)CurrentSpooler).GetBuffer();
                    var encoding = (buffer.Length > 1 && buffer[1] == 0) ? Encoding.Unicode : Encoding.UTF8;

                    // dump debug matxml container
                    sb.AppendLine(encoding.GetString(buffer));
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
                try
                {
                    // ask user if we can open the file, losing any changes made
                    if (!CloseChunkFile())
                        return;

                    Mouse.OverrideCursor = Cursors.Wait;

                    var stopwatch = new Stopwatch();
                    
                    ChunkFile = new FileChunker();

                    // capture events
                    ChunkFile.SpoolerLoaded += (o, e) => {
                        switch ((ChunkType)o.Context)
                        {
                        case ChunkType.SpooledVehicleChunk:
                            {
                                if (o.Version != 0)
                                    break;

                                var vo3d = ((SpoolablePackage)o).Children[0] as SpoolableBuffer;
                                var buffer = BitConverter.ToInt16(vo3d.GetBuffer(), 0);

                                var vehId = buffer & 0xFF;
                                var modLvl = (buffer & 0x7000) / 0x1000;

                                // VehicleName [optional:(Bodykit #N)]
                                o.Description = String.Format("{0}{1}", DriverPL.VehicleNames[vehId], (modLvl > 0) ? String.Format(" (Bodykit #{0})", modLvl) : "");
                            }
                            break;
                        }
                    };

                    AT.Log("Loading...");

                    stopwatch.Start();

                    ChunkFile.Load(openFile.FileName);
                    
                    UpdateSpoolers();

                    IsDirty = false;
                    OnPropertyChanged("WindowTitle");

                    stopwatch.Stop();
                    
                    AT.Log("Loaded {0} chunks in {1}s.", LoadedChunk.Children.Count, stopwatch.Elapsed.TotalSeconds);
                    AT.Log("Temp directory size: {0:N0} KB", DSCTempFileManager.GetTempDirectorySize() / 1024.0);
                }
                catch (FileFormatException e)
                {
                    MessageBox.Show(e.Message, AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }
        }
        
        private bool CanCloseChunkFile
        {
            get
            {
                if (IsChunkFileOpen)
                {
                    if (IsDirty)
                    {
                        var result = MessageBox.Show("You have unsaved changes - do you wish to continue?", AppTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning);

                        // user decides if we can close it
                        return (result == MessageBoxResult.Yes);
                    }

                    // chunk file can be closed
                    return true;
                }

                // nothing to close!
                return false;
            }
        }

        private bool CloseChunkFile()
        {
            if (IsChunkFileOpen)
            {
                if (CanCloseChunkFile)
                {
                    ChunkFile.Dispose();
                    ChunkFile = null;

                    ChunkList.Items.Clear();
                    ChunkList.UpdateLayout();

                    IsDirty = false;
                    OnPropertyChanged("Spoolers");

                    // closed successfully
                    return true;
                }

                // nothing was closed
                return false;
            }
            else
            {
                // nothing was open in the first place
                return true;
            }
        }
        
        private void LoadDDS()
        {
            if (CurrentImage != null)
            {
                CurrentImage = null;

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            var fibitmap = BitmapHelper.GetFIBITMAP(((SpoolableBuffer)CurrentSpooler).GetBuffer());
            var fibitmap24 = FreeImage.ConvertTo24Bits(fibitmap);

            try
            {
                using (var bitmap = FreeImage.GetBitmap(fibitmap24))
                {
                    CurrentImage = (bitmap != null) ? bitmap.ToBitmapSource() : null;
                }
            }
            finally
            {
                FreeImage.UnloadEx(ref fibitmap);
                FreeImage.UnloadEx(ref fibitmap24);
            }
        }

        private void CutSpooler(object sender, RoutedEventArgs e)
        {
            if (CanModifySpooler)
            {
                var parentItem  = CurrentSpoolerItem.Parent as SpoolerListItem;
                var parent      = CurrentSpooler.Parent;
                var items       = (parentItem != null) ? parentItem.Items : ChunkList.Items;

                if (items != null)
                {
                    SpoolerClipboard = CurrentSpooler;

                    var index = items.IndexOf(CurrentSpoolerItem);

                    parent.Children.Remove(CurrentSpooler);
                    items.RemoveAt(index);

                    if (SpoolerClipboard != null && SpoolerClipboard.Parent == null)
                    {
                        AT.Log("Clipboard set.");
                        IsDirty = true;
                    }
                }
            }
        }

        private void PasteSpooler(object sender, RoutedEventArgs e)
        {
            if (CanPasteSpooler)
            {
                if (CurrentSpooler is SpoolablePackage)
                {
                    ((SpoolablePackage)CurrentSpooler).Children.Add(SpoolerClipboard);
                    CurrentSpoolerItem.Items.Add(new SpoolerListItem(SpoolerClipboard));
                }
                else
                {
                    var parentItem = CurrentSpoolerItem.Parent as SpoolerListItem;

                    var parent = CurrentSpooler.Parent;
                    var index = parent.Children.IndexOf(CurrentSpooler) + 1;

                    var item = new SpoolerListItem(SpoolerClipboard);

                    if (index < parent.Children.Count)
                    {
                        parent.Children.Insert(index, SpoolerClipboard);

                        if (parentItem != null)
                            parentItem.Items.Insert(index, item);
                        else
                            ChunkList.Items.Insert(index, item);
                    }
                    else
                    {
                        parent.Children.Add(SpoolerClipboard);

                        if (parentItem != null)
                            parentItem.Items.Add(item);
                        else
                            ChunkList.Items.Add(item);
                    }
                }

                IsDirty = true;
            }
        }

        private void RemoveSpooler(object sender, RoutedEventArgs e)
        {
            if (CanModifySpooler)
            {
                if (CurrentSpooler.Parent == null && LoadedChunk.Children.Count == 1)
                    MessageBox.Show("You cannot remove the root node!");
                else
                {
                    var parentItem = CurrentSpoolerItem.Parent as SpoolerListItem;
                    var items = (parentItem != null) ? parentItem.Items : ChunkList.Items;

                    if (items != null)
                    {
                        var index = items.IndexOf(CurrentSpoolerItem);

                        // magic *snort snort*
                        CurrentSpooler.Dispose();

                        items.RemoveAt(index);

                        if (items.Count > 0)
                            ((SpoolerListItem)items[(index < items.Count) ? index : --index]).IsSelected = true;
                        
                        IsDirty = true;
                    }
                }
            }
        }

        private void RenameSpooler(object sender, RoutedEventArgs e)
        {
            if (CanModifySpooler)
            {
                var inputBox = new MKInputBox("Edit Description", "Please enter a new description:", CurrentSpooler.Description) {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (inputBox.ShowDialog() ?? false)
                {
                    CurrentSpooler.Description = inputBox.InputValue;

                    // this is very hackish, but it works!
                    CurrentSpoolerItem.Header = null;
                    CurrentSpoolerItem.Header = CurrentSpooler;

                    ChunkList.Items.Refresh();
                    ChunkList.UpdateLayout();

                    IsDirty = true;
                }
            }
        }

        private void ReplaceBuffer(object sender, RoutedEventArgs e)
        {
            if (ChunkFile.IsCompressed)
            {
                MessageBox.Show("Sorry, compressed chunk files cannot be modified.", AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var openFile = new OpenFileDialog() {
                CheckFileExists = true,
                CheckPathExists = true,
                ValidateNames = true
            };

            if (openFile.ShowDialog() ?? false)
            {
                var spooler = (SpoolableBuffer)CurrentSpooler;

                spooler.SetBuffer(File.ReadAllBytes(openFile.FileName));

                AT.Log("Replaced buffer.");
                IsDirty = true;
            }
        }

        public void SaveChunkFile()
        {
            if (ChunkFile.IsCompressed)
            {
                MessageBox.Show("Sorry, compressed chunk files cannot be modified.", AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (IsDirty)
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var bak = FileName + ".bak";
                var stopwatch = new Stopwatch();

                AT.Log("Saving...");
                stopwatch.Start();

                // make our backup
                File.Copy(FileName, bak, true);

                ChunkFile.Save();
                stopwatch.Stop();

                IsDirty = false;
                Mouse.OverrideCursor = null;

                DSC.Log("Successfully saved file in {0}ms.", stopwatch.ElapsedMilliseconds);
            }
            else
            {
                MessageBox.Show("No changes have been made!", AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void ExportChunkFile()
        {
            if (ChunkFile.IsCompressed)
            {
                MessageBox.Show("Sorry, compressed chunk files cannot be exported.", AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;

            var stopwatch = new Stopwatch();
            var filename = Path.Combine(Settings.ExportDirectory, Path.GetFileName(FileName));

            AT.Log("Exporting...");

            stopwatch.Start();
            ChunkFile.Save(filename, false);
            stopwatch.Stop();
            
            IsDirty = false;
            Mouse.OverrideCursor = null;

            DSC.Log("Exported file in {0}ms.", stopwatch.ElapsedMilliseconds);

            MessageBox.Show($"Successfully exported to '{filename}'!", AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportSpooler(object sender, RoutedEventArgs e)
        {
            bool chunk = (CurrentSpooler is SpoolablePackage);

            var saveDlg = new SaveFileDialog() {
                AddExtension = true,
                DefaultExt =  "." + ((chunk) ? "chunk" : Encoding.UTF8.GetString(BitConverter.GetBytes(CurrentSpooler.Context)).ToLower()),
                FileName = "export",
                InitialDirectory = Settings.ExportDirectory,
                Title = "Please enter a filename",
                ValidateNames = true,
                OverwritePrompt = true,
            };

            if (CurrentSpooler.Context == 0x444D4244) // DBMD
                saveDlg.DefaultExt = ".debugmat.xml";

            if (saveDlg.ShowDialog() ?? false)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var stopwatch = new Stopwatch();

                DSC.Log("Exporting '{0}'...", saveDlg.FileName);
                stopwatch.Start();
                
                if (chunk)
                {
                    FileChunker.WriteChunk(saveDlg.FileName, (SpoolablePackage)CurrentSpooler);
                }
                else
                {
                    // var unifiedPackage = ChunkTemplates.UnifiedPackage;
                    // 
                    // // copy the buffer
                    // var spoolerCopy = new SpoolableBuffer() {
                    //     Alignment = CurrentSpooler.Alignment,
                    //     Magic = CurrentSpooler.Magic,
                    //     Description = CurrentSpooler.Description,
                    //     Reserved = CurrentSpooler.Reserved
                    // };
                    // 
                    // spoolerCopy.SetBuffer(((SpoolableBuffer)CurrentSpooler).GetBuffer());
                    // 
                    // unifiedPackage.Children.Add(spoolerCopy);
                    // 
                    // FileChunker.WriteChunk(saveDlg.FileName, unifiedPackage);
                    // 
                    // // release resources
                    // spoolerCopy.Dispose();
                    // unifiedPackage.Dispose();

                    using (var fs = File.Create(saveDlg.FileName))
                    {
                        fs.SetLength(CurrentSpooler.Size);
                        fs.Write(((SpoolableBuffer)CurrentSpooler).GetBuffer());
                    }
                }

                stopwatch.Stop();
                
                IsDirty = false;
                Mouse.OverrideCursor = null;

                DSC.Log("Successfully exported file in {0}ms!", stopwatch.ElapsedMilliseconds);

                MessageBox.Show($"Successfully exported to '{saveDlg.FileName}'!", AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SpoolerSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = e.NewValue as SpoolerListItem;

            CurrentSpoolerItem = item;

            if (CurrentSpooler != null)
            {
                if (CurrentSpooler.Context == ChunkType.ResourceTextureDDS)
                {
                    LoadDDS();
                    return;
                }
            }

            if (CurrentImage != null)
                CurrentImage = null;
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

        private void FindSpoolerParent(object sender, RoutedEventArgs e)
        {
            var spooler = CurrentSpoolerItem.Spooler;
            var parent = spooler.Parent;

            // nothing to do
            if (parent == null)
                return;

            var stack = new Stack<SpoolablePackage>();

            stack.Push(parent);

            while (parent.Parent != null)
            {
                parent = parent.Parent;

                stack.Push(parent);
            }

            var root = stack.Pop();

            if (stack.Count > 0 && ReferenceEquals(root, LoadedChunk))
            {
                tbSearchFilter.Text = "";
                ParentQueue = stack;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!CloseChunkFile())
            {
                // user canceled
                e.Cancel = true;
            }

            base.OnClosing(e);
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
                CloseChunkFile();
            };

            fileExit.Click += (o, e) => {
                Close();
            };

            toolsExport.Click += (o, e) => {
                if (LoadedChunk != null)
                    ExportChunkFile();
            };

            tbSearchFilter.TextChanged += (o, e) => QueueFilterUpdate(tbSearchFilter.Text);
        }
    }

    public class SpoolerListItem : TreeViewItem
    {
        public Spooler Spooler { get; set; }

        public bool FullyLoaded { get; set; }

        public SpoolerListItem(Spooler spooler)
        {
            Spooler = spooler;
            Header = Spooler;
        }
    }
}
