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
    /// Interaction logic for MKChooserBox.xaml
    /// </summary>
    public partial class MKChooserBox : ObservableWindow
    {
        private string _optionName;
        private string _optionHover;
        private bool _showCancelButton;
        private bool _showOptionCheckbox;

        public string OptionName
        {
            get { return _optionName; }
            set { SetValue(ref _optionName, value, "OptionName"); }
        }

        public string OptionToolTip
        {
            get { return _optionHover; }
            set { SetValue(ref _optionHover, value, "OptionToolTip"); }
        }

        public string SelectedItem
        {
            get { return ItemsBox.SelectedItem as string; }
        }

        public int SelectedIndex
        {
            get { return ItemsBox.SelectedIndex; }
            set { ItemsBox.SelectedIndex = value; }
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

        public MKChooserBox(string title, string prompt, string[] items)
        {
            InitializeComponent();

            Title = title;
            PromptText.Content = prompt;

            Loaded += (o, e) => {
                foreach (var item in items)
                    ItemsBox.Items.Add(item);

                ItemsBox.SelectedIndex = 0;
                ItemsBox.Focus();
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
