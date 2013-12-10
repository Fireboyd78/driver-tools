using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using HelixToolkit.Wpf;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for ModelViewer.xaml
    /// </summary>
    public partial class ModelViewer : UserControl
    {
        bool debug              = false;
        bool isWalkAround       = false;

        bool headlight          = false;
        bool infiniteSpin       = false;

        Point3D dCamPos         = new Point3D(-3, 6, 1);
        Vector3D dLookdir       = new Vector3D(3, -6, -1);

        //Vector3D dLightDir      = new Vector3D(-1, 2, 6);
        //Color dLightColor       = Color.FromRgb(230, 230, 230);

        Vector3D zUp           = new Vector3D(0, 0, 1);
        Vector3D zDown         = new Vector3D(0, 0, -1);

        static Thread debugInfoThread;
        volatile bool restartThread = false;

        public void SetDebugInfo(string str, params object[] args)
        {
            SetDebugInfo(String.Format(str, args));
        }

        public void SetDebugInfo(string str)
        {
            VP3D.DebugInfo = str;

            if (debugInfoThread != null && debugInfoThread.IsAlive)
                restartThread = true;
            else
            {
                debugInfoThread = new Thread(new ThreadStart(DelayResetDebugInfo)) { IsBackground = true };
                debugInfoThread.Start();
            }
        }

        public bool DebugMode
        {
            get { return debug; }
            set
            {
                //VP3D.ShowFieldOfView        = value;
                VP3D.ShowFrameRate          = value;
                VP3D.ShowCameraInfo         = value;
                VP3D.ShowCameraTarget       = value;
                VP3D.ShowTriangleCountInfo  = value;

                debug = value;

                //VP3D.ViewCubeVerticalPosition = (debug) ? VerticalAlignment.Center : VerticalAlignment.Top;

                SetDebugInfo("Debug mode {0}.", (debug) ? "enabled" : "disabled");
            }
        }

        public bool Headlight
        {
            get { return headlight; }
            set
            {
                headlight = value;
                VP3D.IsHeadLightEnabled = value;

                SetDebugInfo("Headlight {0}.", (headlight) ? "enabled" : "disabled");
            }
        }

        public bool InfiniteSpin
        {
            get { return infiniteSpin; }
            set
            {
                if (!isWalkAround)
                {
                    infiniteSpin = value;

                    VP3D.InfiniteSpin = value;

                    if (infiniteSpin)
                        VP3D.CameraController.StartSpin(new Vector(120.0, 0.0), new Point(0, 0), new Point3D(0, 0, 0));

                    VP3D.PanGesture = new MouseGesture((infiniteSpin) ? MouseAction.None : MouseAction.LeftClick);
                    VP3D.RotateGesture = new MouseGesture((infiniteSpin) ? MouseAction.None : MouseAction.RightClick);

                    SetDebugInfo("Infinite spin {0}.", (infiniteSpin) ? "enabled" : "disabled");
                }
                else
                {
                    SetDebugInfo("Infinite spin cannot be enabled in walkaround mode.");
                }
            }
        }

        public bool WalkaroundMode
        {
            get { return isWalkAround; }
            set
            {
                isWalkAround = value;

                VP3D.CameraMode                         = (!isWalkAround) ? CameraMode.Inspect : CameraMode.WalkAround;
                //VP3D.CameraRotationMode                 = (!isWalkAround) ? CameraRotationMode.Trackball : CameraRotationMode.Turntable;
                //VP3D.CameraController.ModelUpDirection  = (!isWalkAround) ? zDown : zUp;
                //VP3D.Camera.UpDirection                 = (!isWalkAround) ? zDown : zUp;

                InfiniteSpin = false;

                if (!isWalkAround)
                {
                    ViewCam.Position = dCamPos;
                    ViewCam.LookDirection = dLookdir;
                }

                SetDebugInfo("Walkaround mode {0}.", (isWalkAround) ? "enabled" : "disabled");
            }
        }

        public void ResetToDefaults(bool silent = false)
        {
            ViewCam.Position            = dCamPos;
            ViewCam.LookDirection       = dLookdir;
            ViewCam.UpDirection         = zUp;
            ViewCam.NearPlaneDistance   = Settings.Configuration.GetSetting<double>("NearDistance", 0);
            ViewCam.FarPlaneDistance    = Settings.Configuration.GetSetting<double>("FarDistance", 300000);

            ViewCam.FieldOfView         = Settings.Configuration.GetSetting<double>("DefaultFOV", 45);

            VP3D.PanGesture             = new MouseGesture(MouseAction.LeftClick);
            VP3D.ChangeLookAtGesture    = new MouseGesture(MouseAction.None);

            //MainLight.Direction         = dLightDir;
            //MainLight.Color             = dLightColor;

            Headlight                   = false;

            if (!silent)
                SetDebugInfo("Reset everything to default.");
        }

        delegate void ResetDebugCallback();

        void ResetDebug()
        {   
            VP3D.DebugInfo = String.Empty;
        }

        void DelayResetDebugInfo()
        {
            int timeout = Settings.Configuration.GetSetting<int>("DebugInfoTimeout", 2500);

            for (int i = 0; i < timeout; i++)
            {
                Thread.Sleep(1);

                if (restartThread)
                {
                    i = 0;
                    restartThread = false;
                }
            }

            VP3D.Dispatcher.Invoke(new ResetDebugCallback(ResetDebug));
        }

        public void SetMaterialOpacity(Material material, double opacity)
        {
            if (material is DiffuseMaterial)
            {
                ((DiffuseMaterial)material).Brush.Opacity = opacity;
            }
            else if (material is EmissiveMaterial)
            {
                ((EmissiveMaterial)material).Brush.Opacity = opacity;
            }
            else if (material is MaterialGroup)
            {
                foreach (Material mat in ((MaterialGroup)material).Children)
                    SetMaterialOpacity(mat, opacity);
            }
        }

        public void SetModelOpacity(Model3D model, double opacity)
        {
            if (model is Model3DGroup)
            {
                foreach (GeometryModel3D geom in ((Model3DGroup)model).Children)
                {
                    SetMaterialOpacity(geom.Material, opacity);
                    SetMaterialOpacity(geom.BackMaterial, opacity);
                }
            }
            else if (model is GeometryModel3D)
            {
                GeometryModel3D geom = (GeometryModel3D)model;

                SetMaterialOpacity(geom.Material, opacity);
                SetMaterialOpacity(geom.BackMaterial, opacity);
            }
        }

        public void GhostNonSelectedModels()
        {
            if (ModelList.SelectedIndex != -1)
            {
                Model3D model3D = Models.Content;
                Model3D selectedModel = ((ModelGroupListItem)ModelList.SelectedItem).Content;

                double ghostOpacity = Settings.Configuration.GetSetting<double>("GhostOpacity", 0.15);

                if (model3D is Model3DGroup)
                {
                    Model3DGroup models = (Model3DGroup)Models.Content;

                    foreach (Model3D model in models.Children)
                    {
                        if (model != selectedModel)
                            SetModelOpacity(model, ghostOpacity);
                        else
                            SetModelOpacity(model, 1.0);
                    }
                }
                else
                {
                    if (model3D != selectedModel)
                        SetModelOpacity(model3D, ghostOpacity);
                    else
                        SetModelOpacity(model3D, 1.0);
                }
            }
        }

        public void UnghostModels()
        {
            if (ModelList.SelectedIndex != -1)
            {
                Model3D model3D = Models.Content;

                if (model3D is Model3DGroup)
                {
                    Model3DGroup models = (Model3DGroup)Models.Content;

                    foreach (Model3D model in models.Children)
                        SetModelOpacity(model, 1.0);
                }
                else
                {
                    SetModelOpacity(model3D, 1.0);
                }
            }
        }

        public bool IsMouseOverTarget(Visual target, Point point)
        {
            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            return bounds.Contains(point);
        }

        public Model3D CurrentModel
        {
            get { return Models.Content; }
        }

        public void SetModel(Model3D model, bool enableDamage = false)
        {
            if (ModelList.Items.Count > 0)
                ModelList.Items.Clear();

            Models.Content = model;

            int idx = 0;

            if (model is Model3DGroup)
            {
                foreach (Model3D m3d in ((Model3DGroup)model).Children)
                    ModelList.Items.Add(new ModelGroupListItem(++idx, m3d));
            }
            else
            {
                ModelList.Items.Add(new ModelGroupListItem(++idx, model));
            }

            ShowDamage.Visibility = (enableDamage) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;

            if (!enableDamage)
                VP3D.Camera.LookAt(new Point3D(0, 0, CurrentModel.Bounds.SizeZ / 2), 0);
        }

        public ModelViewer()
        {
            InitializeComponent();

            ResetToDefaults(true);

            Loaded += (o, e) => {
                InfiniteSpin = Settings.Configuration.GetSetting<bool>("InfiniteSpin", true);
            };

            ModelList.GotFocus += (o, e) => GhostNonSelectedModels();
            ModelList.SelectionChanged += (o, e) => GhostNonSelectedModels();

            ModelList.MouseLeftButtonDown += (o, e) => {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    bool focused = false;

                    for (int i = 0; i < ModelList.Items.Count; i++)
                    {
                        ListBoxItem lbi = (ListBoxItem)ModelList.ItemContainerGenerator.ContainerFromIndex(i);

                        if (lbi == null)
                            continue;

                        if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                        {
                            focused = true;
                            break;
                        }
                    }

                    if (!focused)
                    {
                        UnghostModels();
                        ModelList.SelectedIndex = -1;
                    }
                }
            };

            ModelList.MouseRightButtonDown += (o, e) => {
                UnghostModels();
                ModelList.SelectedIndex = -1;
            };

            DeselectModel.Click += (o, e) => {
                UnghostModels();
                ModelList.SelectedIndex = -1;
            };

            KeyDown += (o, e) => {
                switch (e.Key)
                {
                case Key.G:
                    DebugMode = (DebugMode) ? false : true;
                    break;
                case Key.R:
                    ResetToDefaults();
                    break;
                case Key.P:
                    WalkaroundMode = (WalkaroundMode) ? false : true;
                    VP3D.CameraInertiaFactor = (WalkaroundMode) ? 0.75 : 0.93;
                    break;
                case Key.H:
                    Headlight = (Headlight) ? false : true;
                    break;
                case Key.I:
                    InfiniteSpin = (InfiniteSpin) ? false : true;
                    break;
                }
            };
        }
    }
}
