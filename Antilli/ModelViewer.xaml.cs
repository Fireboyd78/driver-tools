using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

        int FOVDefault          = 45;
        int FOVAlt              = 70;

        Point3D dCamPos         = new Point3D(-4, 7, 2);
        Vector3D dLookdir       = new Vector3D(4, -7, -2);

        Vector3D dLightDir      = new Vector3D(1, -1, 6);
        Color dLightColor       = Color.FromRgb(150, 150, 150);

        double dNearPDist       = 0.125;
        double dFarPDist        = 100000;

        Vector3D zUp           = new Vector3D(0, 0, 1);
        Vector3D zDown         = new Vector3D(0, 0, -1);

        public bool IsFOVDefault
        {
            get { return (ViewCam.FieldOfView == FOVDefault) ? true : false; }
        }

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
                VP3D.ShowFieldOfView        = value;
                VP3D.ShowFrameRate          = value;
                VP3D.ShowCameraInfo         = value;
                VP3D.ShowCameraTarget       = value;
                VP3D.ShowTriangleCountInfo  = value;

                debug = value;

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

        public void ResetToDefaults(bool silent)
        {
            ViewCam.Position            = dCamPos;
            ViewCam.LookDirection       = dLookdir;
            ViewCam.UpDirection         = zUp;
            ViewCam.NearPlaneDistance   = dNearPDist;
            ViewCam.FarPlaneDistance    = dFarPDist;

            MainLight.Direction         = dLightDir;
            MainLight.Color             = dLightColor;

            InfiniteSpin                = false;
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
            for (int i = 0; i < 2500; i++)
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

        public ModelViewer()
        {
            InitializeComponent();

            ResetToDefaults(true);

            KeyDown += (o, e) => {
                switch (e.Key)
                {
                case Key.G:
                    DebugMode = (DebugMode) ? false : true;
                    break;
                case Key.R:
                    ViewCam.FieldOfView = (IsFOVDefault) ? FOVAlt : FOVDefault;
                    SetDebugInfo("FOV changed to {0}.", ViewCam.FieldOfView);
                    break;
                case Key.T:
                    ResetToDefaults(false);
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
