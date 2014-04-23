using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using DSC = DSCript.DSC;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for ChunkTest.xaml
    /// </summary>
    public partial class ChunkViewer : ObservableWindow
    {
        private string Filename { get; set; }

        private SpoolableChunk loadedChunk;

        protected SpoolableChunk LoadedChunk
        {
            get { return loadedChunk; }
            set
            {
                if (loadedChunk != null)
                    loadedChunk.Dispose();

                refreshSpoolers = true;

                loadedChunk = value;
            }
        }

        private Spooler currentSpooler;

        private bool refreshSpoolers = false;

        private IEnumerable<Spooler> allSpoolers;
        private IEnumerable<Spooler> AllSpoolers
        {
            get
            {
                if (allSpoolers == null || refreshSpoolers)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    allSpoolers = LoadedChunk.GetAllSpoolers();

                    stopwatch.Stop();

                    DSC.Log("Took {0}ms to flatten {1:N0} spoolers.", stopwatch.Elapsed.TotalMilliseconds, allSpoolers.Count());
                }

                refreshSpoolers = false;

                return allSpoolers;
            }
        }

        private SpoolableChunk FindParent(Spooler child)
        {
            SpoolableChunk parent = null;

            var chunks = AllSpoolers.Where((s) => (s is SpoolableChunk && s != child));

            if (chunks != null)
            {
                var timer = new Stopwatch();
                timer.Start();

                foreach (SpoolableChunk chunk in chunks)
                {
                    if (chunk.Spoolers.Contains(child))
                    {
                        parent = chunk;
                        break;
                    }
                }

                timer.Stop();

                DSC.Log("Searched through {0:N0} chunks in {1}ms.", chunks.Count(), timer.Elapsed.TotalMilliseconds);
            }

            return parent;
        }

        public List<Spooler> Spoolers
        {
            get { return (LoadedChunk != null) ? new List<Spooler>(LoadedChunk.Spoolers) : null; }
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

        public string SpoolerInfo
        {
            get
            {
                if (CurrentSpooler == null)
                    return "";

                var str = String.Format("Alignment: 0x{0:X} ({0})\r\nReserved: {1}\r\nSize: 0x{2:X}\r\n\r\n",
                    CurrentSpooler.Alignment, CurrentSpooler.Reserved, CurrentSpooler.Size);

                if (CurrentSpooler.Magic == (int)ChunkType.BuildInfo)
                {
                    var buffer = ((SpoolableData)CurrentSpooler).Buffer;
                    var encoding = (buffer.Length > 1 && buffer[1] == 0) ? Encoding.Unicode : Encoding.UTF8;

                    str += String.Format("Build Info:\r\n\r\n{0}", encoding.GetString(buffer));
                }

                return str;
            }
        }

        private void SpoolerSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var target = e.NewValue as Spooler;
            CurrentSpooler = AllSpoolers.First((s) => s == target);
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

                spooler.Buffer = File.ReadAllBytes(openFile.FileName);
                
                DSC.Log("Replaced buffer.");

                OnPropertyChanged("Spoolers");
            }

            refreshSpoolers = true;
        }

        private void RemoveCurrentSpoolerFromParent(SpoolableChunk parent)
        {
            CurrentSpooler.Dispose();
            parent.Spoolers.Remove(CurrentSpooler);

            OnPropertyChanged("Spoolers");
        }

        private void RemoveSpooler(object sender, RoutedEventArgs e)
        {
            var spoolers = LoadedChunk.Spoolers;

            if (!spoolers.Contains(CurrentSpooler))
            {
                var parent = FindParent(CurrentSpooler);

                if (parent != null)
                {
                    RemoveCurrentSpoolerFromParent(parent);
                }
                else
                {
                    MessageBox.Show("FATAL ERROR: Parent is null!");
                }
            }
            else
            {
                if (spoolers.Count >= 2)
                {
                    RemoveCurrentSpoolerFromParent(LoadedChunk);
                }
                else
                {
                    MessageBox.Show("You cannot remove the root node!");
                }
            }
        }

        public void ExportFile()
        {
            var stopwatch = new Stopwatch();

            DSC.Log("Exporting...");
            Chunk.PaddingType = Chunk.BytePaddingType.PaddingType2;

            stopwatch.Start();
            LoadedChunk.Save(Path.Combine(Settings.Configuration.GetDirectory("Export"), Path.GetFileName(Filename)));
            stopwatch.Stop();

            DSC.Log("Exported file in {0}s.", stopwatch.Elapsed.TotalSeconds);
        }

        public void OpenChunkFile()
        {
            var openFile = new OpenFileDialog() {
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = Driv3r.FileFilter,
                InitialDirectory = Driv3r.RootDirectory,
                ValidateNames = true,
            };

            openFile.AddFilter("All Files|*.*");

            if (openFile.ShowDialog() ?? false)
            {
                Filename = openFile.FileName;

                var stopwatch = new System.Diagnostics.Stopwatch();

                if (LoadedChunk != null)
                {
                    DSC.Log("Collecting garbage...");

                    LoadedChunk = null;
                    GC.Collect();
                }

                DSC.Log("Loading...");

                stopwatch.Start();
                var file = new DSCript.Spoolers.SpoolableChunk(Filename);
                stopwatch.Stop();

                DSC.Log("Loaded {0} chunks in {1}s.", file.Spoolers.Count, stopwatch.Elapsed.TotalSeconds);
                DSC.Log("Temp directory size: {0:F4}kb", DSC.GetTempDirectorySize() / 1024.0);

                LoadedChunk = file;
                OnPropertyChanged("Spoolers");

                //if (file.Spoolers.Count > 0)
                //    ExportFile();

                GC.Collect();
            }
        }

        public ChunkViewer(SpoolableChunk chunk) : this()
        {
            LoadedChunk = chunk;
            OnPropertyChanged("Spoolers");
        }

        public ChunkViewer()
        {
            InitializeComponent();

            KeyDown += (o, e) => {
                switch (e.Key)
                {
                case Key.E:
                    if (LoadedChunk != null)
                        ExportFile();
                    break;
                case Key.O: OpenChunkFile();
                    break;
                default: break;
                }
            };
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
    }
}
