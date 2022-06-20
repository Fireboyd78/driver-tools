using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for Exporter.xaml
    /// </summary>
    public partial class ExportModelDialog : ObservableWindow
    {
        private string _defaultName = "Enter a name...";

        private bool _nameChanged = false;
        private bool _nameEmpty = false;

        private void OnTextBoxInFocus(object sender, RoutedEventArgs e)
        {
            if (!_nameChanged)
            {
                var text = tbFolderName.Text;
                if (text.Length > 0)
                    tbFolderName.Select(0, text.Length);
            }
        }

        private void OnTextBoxOuttaFocus(object sender, RoutedEventArgs e)
        {
            if (_nameEmpty)
            {
                tbFolderName.Text = _defaultName;
                _nameEmpty = false;
                _nameChanged = false;
                
                OnPropertyChanged("IsValid");
            }
        }

        private void InitFancyTextBox()
        {
            tbFolderName.GotKeyboardFocus += OnTextBoxInFocus;
            tbFolderName.GotMouseCapture += OnTextBoxInFocus;

            tbFolderName.LostFocus += OnTextBoxOuttaFocus;

            tbFolderName.Text = _defaultName;

            tbFolderName.TextChanged += (o, e) => {
                if (_nameChanged)
                {
                    _nameEmpty = (tbFolderName.Text.Length == 0);
                }
                else
                {
                    _nameChanged = true;
                }

                OnPropertyChanged("IsValid");
            };
        }

        private ExportModelFlags _flags = ExportModelFlags.None;

        private void AddOption(CheckBox control, ExportModelFlags flags, Action changedCallback = null)
        {
            control.Checked += (o, e) => {
                _flags |= flags;
                changedCallback?.Invoke();
            };

            control.Unchecked += (o, e) => {
                _flags &= ~flags;
                changedCallback?.Invoke();
            };
        }
        
        public ExportModelFormat Format
        {
            get
            {
                switch (cmbGame.SelectedIndex)
                {
                case 0: return ExportModelFormat.Driv3r;
                case 1: return ExportModelFormat.DriverParallelLines;
                }

                return ExportModelFormat.Invalid;
            }
            set
            {
                switch (value)
                {
                case ExportModelFormat.Driv3r:
                    cmbGame.SelectedIndex = 0;
                    break;
                case ExportModelFormat.DriverParallelLines:
                    cmbGame.SelectedIndex = 1;
                    break;
                default:
                    cmbGame.SelectedIndex = -1;
                    break;
                }
            }
        }

        public ExportModelFlags Flags
        {
            get { return _flags; }
        }

        private bool _showFormatSelector = false;

        public bool ShowFormatSelector
        {
            get { return _showFormatSelector; }
            set { SetValue(ref _showFormatSelector, value, "ShowFormatSelector"); }
        }

        public bool IsValid
        {
            get
            {
                return (_nameChanged && !_nameEmpty);
            }
        }

        public string FolderName
        {
            get { return tbFolderName.Text; }
            set
            {
                if (!_nameChanged)
                    tbFolderName.Text = value;
            }
        }
        
        public bool BakeTransforms
        {
            get { return Flags.HasFlag(ExportModelFlags.BakeTransforms); }
        }

        public bool SplitByMaterial
        {
            get { return Flags.HasFlag(ExportModelFlags.SplitByMaterial); }
        }

        public bool ExportAll
        {
            get { return Flags.HasFlag(ExportModelFlags.ExportAll); }
        }
        
        private void UpdatePrompt()
        {
            OnPropertyChanged("UseModelName");
        }
        
        public ExportModelDialog()
        {
            InitializeComponent();
            InitFancyTextBox();

            AddOption(chkBakeTransforms, ExportModelFlags.BakeTransforms);
            AddOption(chkSplitMeshes, ExportModelFlags.SplitByMaterial);
            AddOption(chkExportAll, ExportModelFlags.ExportAll, UpdatePrompt);
            
            btnOk.Click += (o, e) => {
                DialogResult = true;
                Close();
            };

            btnCancel.Click += (o, e) => {
                DialogResult = false;
                Close();
            };
        }
    }

    [Flags]
    public enum ExportModelFlags
    {
        None,

        BakeTransforms = (1 << 0),
        SplitByMaterial = (1 << 1),
        ExportAll = (1 << 2),
    }

    public enum ExportModelFormat
    {
        Invalid = -1,

        Driv3r,
        DriverParallelLines,
    }
}
