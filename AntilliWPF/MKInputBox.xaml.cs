using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

using System.Windows.Forms;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for MKInputBox.xaml
    /// </summary>
    public partial class MKInputBox : ObservableWindow
    {
        private string _optionName;
        private bool _showCancelButton;
        private bool _showOptionCheckbox;

        public string OptionName
        {
            get { return _optionName; }
            set { SetValue(ref _optionName, value, "OptionName"); }
        }

        public string InputValue
        {
            get { return ValueText.Text; }
        }
        
        public bool IsOptionChecked
        {
            get { return chkOption.IsChecked ?? false; }
        }

        public bool ShowCancelButton
        {
            get { return _showCancelButton; }
            set { SetValue(ref _showCancelButton, value, "ShowCancelButton"); }
        }

        public bool ShowOptionCheckbox
        {
            get { return _showOptionCheckbox; }
            set { SetValue(ref _showOptionCheckbox, value, "ShowOptionCheckbox"); }
        }

        public MKInputBox(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();

            Title = title;
            PromptText.Content = prompt;

            Loaded += (o, e) => {
                ValueText.Text = defaultValue;
                ValueText.Focus();

                if (defaultValue.Length > 0)
                    ValueText.Select(0, defaultValue.Length);
            };

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
}
