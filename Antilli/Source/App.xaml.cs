using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Navigation;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            var sb = new StringBuilder();

            sb.AppendLine($"A fatal error has occurred!");
            sb.AppendLine();

            // this is literally useless
            if (exception is TargetInvocationException)
                exception = exception.InnerException;
            
            sb.AppendLine($"{exception.Message}");
            sb.AppendLine();
            
            // should we dump out the memory cache info?
            if ((MemoryCache.Hits | MemoryCache.Misses) != 0)
            {
                MemoryCache.Dump(sb, true);
                sb.AppendLine();
            }

            sb.AppendLine($"===== Stack trace =====");

            var stk = new StackTrace(exception, true);
            
            int frames = 0;

            for (int i = 0; i < stk.FrameCount; i++)
            {
                var frame = stk.GetFrame(i);

                if ((frame.GetFileLineNumber() | frame.GetFileColumnNumber()) != 0)
                    sb.AppendLine($"(*) {frame.ToString()}");

                frames++;
            }

            if (frames < stk.FrameCount)
                sb.AppendLine($" (+{(stk.FrameCount - frames)} more frames)");

            sb.AppendLine($"=======================");
            sb.AppendLine();

            sb.AppendLine("Do you wish to ignore this and continue anyways?");
            sb.AppendLine();

            sb.AppendLine("** WARNING: This may result in unexpected behavior! **");

            var result = MessageBox.Show(sb.ToString(), "Antilli - ERROR!", MessageBoxButton.YesNo, MessageBoxImage.Error, MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
            {
                if (!e.IsTerminating)
                    Environment.Exit(1);
            }
        }

        MainWindow CreateWindow(StartupEventArgs ev)
        {
            var window = new MainWindow(ev.Args);

            if (!Debugger.IsAttached)
            {
                MainWindow.Dispatcher.UnhandledException += (o, e) => {
                    UnhandledExceptionHandler(o, new UnhandledExceptionEventArgs(e.Exception, false));
                    e.Handled = true;
                };
            }

            window.Initialize();

            return window;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = AT.CurrentCulture;
            Thread.CurrentThread.CurrentUICulture = AT.CurrentCulture;
            
            MainWindow = CreateWindow(e);
            MainWindow.Show();

            base.OnStartup(e);
        }
    }
}
