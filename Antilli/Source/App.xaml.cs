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
        void InstallExceptionHandler()
        {
            AppDomain.CurrentDomain.UnhandledException += (o, ex) => {
                var exception = ex.ExceptionObject as Exception;
                var sb = new StringBuilder();

                sb.AppendLine($"A fatal error has occurred! The program will now close.");
                sb.AppendLine();

                // this is literally useless
                if (exception is TargetInvocationException)
                    exception = exception.InnerException;

                var stk = new StackTrace(exception, true);
                var trace = stk.ToString();

                sb.AppendLine($"{exception.Message}");
                sb.AppendLine();

                // should we dump out the memory cache info?
                if ((MemoryCache.Hits | MemoryCache.Misses) != 0)
                {
                    MemoryCache.Dump(sb, true);
                    sb.AppendLine();
                }

                sb.AppendLine($"===== Stack trace =====");
                sb.Append(trace);
                sb.AppendLine($"=======================");

                if (MessageBox.Show(sb.ToString(), "Antilli - ERROR!", MessageBoxButton.OK, MessageBoxImage.Error) == MessageBoxResult.OK)
                    Environment.Exit(1);
            };
        }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = AT.CurrentCulture;
            Thread.CurrentThread.CurrentUICulture = AT.CurrentCulture;

            if (!Debugger.IsAttached)
                InstallExceptionHandler();

            MainWindow = new MainWindow(e.Args);
            MainWindow.Show();

            base.OnStartup(e);
        }
    }
}
