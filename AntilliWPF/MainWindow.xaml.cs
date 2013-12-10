using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
//using System.Windows.Shapes;

using HelixToolkit.Wpf;

using Microsoft.Win32;

using DSCript;
using DSCript.Models;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool debugMode = false;

        static OpenFileDialog openFile = new OpenFileDialog() {
            CheckFileExists = true,
            CheckPathExists = true,
            Filter = "All supported formats|*.vvs;*.vvv;*.vgt;*.d3c;*.pcs;*.cpr;*.dam;*.map;*.gfx;*.pmu;*.d3s;*.mec;*.bnk|Any file|*.*",
            ValidateNames = true,
        };

        public void OpenFile()
        {
            if (openFile.ShowDialog() ?? false)
            {
                IModelFile modelFile = null;

                ChunkFile chunkFile = new ChunkFile(openFile.FileName);

                if (chunkFile.Load() != -1)
                    DSC.Log("Successfully loaded the file!");

                switch (Path.GetExtension(openFile.FileName))
                {
                case ".vvs":
                    modelFile = new VVSFile(openFile.FileName);
                    break;
                default:
                    modelFile = new ModelFile(openFile.FileName);
                    break;
                }
            }
        }

        public bool DebugMode
        {
            get { return debugMode; }
            set
            {
                Viewer.ShowFieldOfView = value;
                Viewer.ShowFrameRate = value;
                Viewer.ShowCameraInfo = value;
                Viewer.ShowCameraTarget = value;
                Viewer.ShowTriangleCountInfo = value;

                debugMode = value;

                SetDebugInfo("Debug mode {0}.", (debugMode) ? "enabled" : "disabled");
            }
        }

        delegate void ResetDebugCallback();

        static Thread debugInfoThread;
        volatile bool restartThread = false;

        public void SetDebugInfo(string str, params object[] args)
        {
            SetDebugInfo(String.Format(str, args));
        }

        public void SetDebugInfo(string str)
        {
            Viewer.DebugInfo = str;

            if (debugInfoThread != null && debugInfoThread.IsAlive)
                restartThread = true;
            else
            {
                debugInfoThread = new Thread(new ThreadStart(DelayResetDebugInfo)) { IsBackground = true };
                debugInfoThread.Start();
            }
        }

        void ResetDebugInfo()
        {
            Viewer.DebugInfo = String.Empty;
        }

        void DelayResetDebugInfo()
        {
            int timeout = Settings.Configuration.GetSetting<int>("DebugInfoTimeout", 1500);

            for (int i = 0; i < timeout; i++)
            {
                Thread.Sleep(1);

                if (restartThread)
                {
                    i = 0;
                    restartThread = false;
                }
            }

            Viewer.Dispatcher.Invoke(new ResetDebugCallback(ResetDebugInfo));
        }

        public bool InfiniteSpin
        {
            get { return Viewer.InfiniteSpin; }
            set
            {
                if (Viewer.CameraMode != HelixToolkit.Wpf.CameraMode.WalkAround)
                {
                    Viewer.InfiniteSpin = value;

                    if (InfiniteSpin)
                        Viewer.CameraController.StartSpin(new Vector(120.0, 0.0), new Point(0, 0), new Point3D(0, 0, 0));

                    SetDebugInfo("Infinite spin {0}.", (InfiniteSpin) ? "enabled" : "disabled");
                }
                else
                {
                    SetDebugInfo("Infinite spin cannot be enabled in walkaround mode.");
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            fileOpen.Click += (o, e) => {
                OpenFile();
            };

            Viewer.Loaded += (o, e) => {
                InfiniteSpin = Settings.Configuration.GetSetting<bool>("InfiniteSpin", true);
            };

            // Set up FOV and Near/Far distance
            VCam.FieldOfView = Settings.Configuration.GetSetting<int>("DefaultFOV", 45);
            VCam.NearPlaneDistance = Settings.Configuration.GetSetting<double>("NearDistance", 0.125);
            VCam.FarPlaneDistance = Settings.Configuration.GetSetting<double>("FarDistance", 150000);

            MeshBuilder box = new MeshBuilder();

            GridLinesVisual3D gridLines = new GridLinesVisual3D();

            box.AddBox(new Point3D(0, 0, 0), 1.5, 1.5, 1.5);

            Model3DGroup group = new Model3DGroup();

            gridLines = new GridLinesVisual3D() {
                Width = 20.0,
                Length = 20.0,
                Thickness = 0.015,
                Material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 80, 80, 80)))
            };

            group.Children.Add(gridLines.Model);

            group.Children.Add(new GeometryModel3D() {
                Geometry = box.ToMesh(),
                Material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 180, 180, 180)))
            });

            Viewer.Children.Add(new SortingVisual3D() {
                Content = group,
                IsSorting = true,
                SortingFrequency = 0.05,
                Method = SortingMethod.BoundingBoxCenter
            });

            KeyDown += (o, e) => {
                switch (e.Key)
                {
                case Key.I:
                    InfiniteSpin = !InfiniteSpin;
                    break;
                case Key.G:
                    DebugMode = !debugMode;
                    break;
                default:
                    break;
                }
            };
        }
    }
}
