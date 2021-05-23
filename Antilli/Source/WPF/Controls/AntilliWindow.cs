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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;

namespace Antilli
{
    public class AntilliWindow : ObservableWindow
    {
        private const string TitleFormat = "{0} - {1}";

        public static new readonly DependencyProperty TitleProperty;
        public static readonly DependencyProperty SubTitleProperty;

        static AntilliWindow()
        {
            Type handle = typeof(AntilliWindow);

            SubTitleProperty =
                DependencyProperty.Register("SubTitle", typeof(string), handle,
                new FrameworkPropertyMetadata(String.Empty, TitleChanged));

            TitleProperty =
                DependencyProperty.Register("Title", typeof(string), handle,
                new FrameworkPropertyMetadata(String.Empty, TitleChanged));
        }

        protected static void TitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AntilliWindow)d).OnTitleChanged();
        }

        protected void OnTitleChanged()
        {
            base.Title = (!String.IsNullOrEmpty(SubTitle)) ? String.Format(TitleFormat, Title, SubTitle) : Title;
        }

        public new string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public virtual string SubTitle
        {
            get { return (string)GetValue(SubTitleProperty); }
            set { SetValue(SubTitleProperty, value); }
        }

        public AntilliWindow()
            : base()
        {

        }
    }
}
