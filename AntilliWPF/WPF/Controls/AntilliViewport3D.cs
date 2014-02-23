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
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;

using System.Xml;

using HelixToolkit.Wpf;

using DSCript;
using DSCript.Models;

namespace Antilli
{
    public class AntilliViewport3D : HelixViewport3D
    {
        public static readonly DependencyProperty CurrentVisualProperty;

        public static readonly DependencyProperty DebugModeProperty;
        public static readonly DependencyProperty DebugInfoTimeoutProperty;

        static AntilliViewport3D()
        {
            Type handle = typeof(AntilliViewport3D);

            CameraModeProperty.OverrideMetadata(handle, new UIPropertyMetadata(CameraModeChanged));
            InfiniteSpinProperty.OverrideMetadata(handle, new UIPropertyMetadata(InfiniteSpinChanged));

            DebugModeProperty =
                DependencyProperty.Register("DebugMode", typeof(bool), handle,
                new UIPropertyMetadata(false, DebugModeChanged));

            DebugInfoTimeoutProperty =
                DependencyProperty.Register("DebugInfoTimeout", typeof(int), handle,
                new PropertyMetadata(1500));
        }

        protected static void CameraModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewport = d as AntilliViewport3D;

            var oldMode = (CameraMode)e.OldValue;
            var newMode = (CameraMode)e.NewValue;

            bool isWalkAround = (newMode == CameraMode.WalkAround);
            bool isFixed = (newMode == CameraMode.FixedPosition);

            if (isWalkAround || isFixed)
            {
                //VP3D.CameraRotationMode                 = (!isWalkAround) ? CameraRotationMode.Trackball : CameraRotationMode.Turntable;
                //VP3D.CameraController.ModelUpDirection  = (!isWalkAround) ? zDown : zUp;
                //VP3D.Camera.UpDirection                 = (!isWalkAround) ? zDown : zUp;

                //if (!isWalkAround)
                //{
                //    VCam.Position = dCamPos;
                //    VCam.LookDirection = dLookdir;
                //}

                viewport.SilenceNextDebugInfo();
                viewport.InfiniteSpin = false;
            }
            else if (oldMode == CameraMode.WalkAround || oldMode == CameraMode.FixedPosition && viewport.IsSpinningPaused)
            {
                viewport.SilenceNextDebugInfo();
                viewport.InfiniteSpin = true;
            }

            viewport.UnmuteNextDebugInfo();
            viewport.SetDebugInfo("{0} mode enabled.", newMode);
        }

        protected static void DebugModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewport = d as AntilliViewport3D;
            var value = (bool)e.NewValue;

            viewport.ShowFieldOfView = value;
            viewport.ShowFrameRate = value;
            viewport.ShowCameraInfo = value;
            viewport.ShowCameraTarget = value;
            viewport.ShowTriangleCountInfo = value;

            viewport.SetDebugInfoState("Debug mode", value);
        }

        protected static void InfiniteSpinChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewport = d as AntilliViewport3D;

            var oldVal = (bool)e.OldValue;
            var value = (bool)e.NewValue;

            if (viewport.CameraMode != CameraMode.Inspect)
            {
                if (!viewport.IsSpinningPaused)
                {
                    viewport.CameraController.StopSpin();
                    viewport.IsSpinningPaused = true;
                }

                viewport.DebugInfo = "Infinite spin can only be used in 'Inspect' mode.";
                viewport.InfiniteSpin = false;
            }
            else
            {
                viewport.InfiniteSpin = value;

                if (value)
                {
                    viewport.CameraController.StartSpin(new Vector(120.0, 0.0), new Point(0, 0), new Point3D(0, 0, 0));
                    viewport.IsSpinningPaused = false;
                }

                viewport.SetDebugInfoState("Infinite spin", value);
            }
        }

        public bool IsSpinningPaused { get; protected set; }

        public Visual3D CurrentVisual
        {
            get { return (Visual3D)GetValue(CurrentVisualProperty); }
            set { SetValue(CurrentVisualProperty, value); }
        }

        public bool DebugMode
        {
            get { return (bool)GetValue(DebugModeProperty); }
            set { SetValue(DebugModeProperty, value); }
        }

        public new string DebugInfo
        {
            get { return base.DebugInfo; }
            set
            {
                if ((!ShouldDebugInfoShutup) == (ShouldDebugInfoShutup = false))
                    return;

                base.DebugInfo = value;

                if (DebugInfoTimeout > 0)
                    DelayDebugInfo();
            }
        }

        public int DebugInfoTimeout
        {
            get { return (int)GetValue(DebugInfoTimeoutProperty); }
            set { SetValue(DebugInfoTimeoutProperty, value); }
        }

        #region SetDebugInfo method/properties
        protected Thread DebugInfoThread;
        protected volatile bool ShouldDebugThreadRestart;
        protected volatile bool ShouldDebugInfoShutup;
        protected volatile int DebugTimeout;

        public void SilenceNextDebugInfo()
        {
            ShouldDebugInfoShutup = true;
        }

        public void UnmuteNextDebugInfo()
        {
            ShouldDebugInfoShutup = false;
        }

        public void SetDebugInfo(string str)
        {
            DebugInfo = str;   
        }

        public void SetDebugInfo(string str, params object[] args)
        {
            SetDebugInfo(String.Format(str, args));
        }

        public void SetDebugInfoState(string name, bool stateCondition)
        {
            SetDebugInfo(String.Format("{0} {1}.", name, (stateCondition) ? "enabled" : "disabled"));
        }

        protected void DelayDebugInfo()
        {
            DebugTimeout = DebugInfoTimeout;

            ShouldDebugThreadRestart = (DebugInfoThread != null && DebugInfoThread.IsAlive);

            if (!ShouldDebugThreadRestart)
            {
                (DebugInfoThread = new Thread(new ThreadStart(() => {
                    int i = 0;

                    while (((i = (ShouldDebugThreadRestart) ? 0 : ++i) <= DebugTimeout)
                        && (!(ShouldDebugThreadRestart = false)) && !ShouldDebugInfoShutup)
                    {
                        Thread.Sleep(1);
                    }

                    Dispatcher.Invoke((Action)(() => base.DebugInfo = String.Empty));
                })) { IsBackground = true }).Start();
            }
        }
        #endregion

        public AntilliViewport3D()
            : base()
        {

        }
    }
}
