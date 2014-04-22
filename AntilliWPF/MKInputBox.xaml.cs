using System;
using System.Collections.Generic;
using System.Linq;
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
        public string InputValue
        {
            get { return ValueText.Text; }
        }

        public MKInputBox(string title, string prompt)
        {
            InitializeComponent();

            Title = title;
            PromptText.Content = prompt;

            Loaded += (o, e) => {
                ValueText.Text = "";
                ValueText.Focus();
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
