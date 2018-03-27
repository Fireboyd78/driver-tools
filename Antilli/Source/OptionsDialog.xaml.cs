using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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

using DSCript;

using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using FormDialogResult = System.Windows.Forms.DialogResult;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for OptionsDialog.xaml
    /// </summary>
    public partial class OptionsDialog : ObservableWindow
    {
        protected Dictionary<string, string> m_gamePaths;
        protected FolderBrowserDialog m_folderBrowser;
        
        protected FolderBrowserDialog FolderBrowser
        {
            get
            {
                if (m_folderBrowser == null)
                    m_folderBrowser = new FolderBrowserDialog() {
                        Description = "Please select a directory:",
                        ShowNewFolderButton = false,
                    };

                return m_folderBrowser;
            }
        }

        protected void RegisterGameDirectoryOption(string gameName, TextBox textBox, Button button)
        {
            var dir = DSC.Configuration.GetDirectory(gameName);

            m_gamePaths.Add(gameName, dir);

            textBox.Text = dir;

            textBox.TextChanged += (o, e) => {
                m_gamePaths[gameName] = textBox.Text;
            };

            button.Click += (o, e) => {
                if (FolderBrowser.ShowDialog() == FormDialogResult.OK)
                    textBox.Text = FolderBrowser.SelectedPath;
            };
        }

        protected bool AreChangesPending
        {
            get
            {
                foreach (var game in m_gamePaths)
                {
                    var oldDir = DSC.Configuration.GetDirectory(game.Key);

                    // did user update path?
                    if (!String.Equals(game.Value, oldDir, StringComparison.InvariantCultureIgnoreCase))
                        return true;
                }

                return false;
            }
        }

        protected void ShowError(string message)
        {
            MessageBox.Show(this, message, "Antilli", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected bool AskUserPrompt(string message)
        {
            return MessageBox.Show(this, message, "Antilli", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }
        
        protected void SaveChanges()
        {
            foreach (var game in m_gamePaths)
            {
                var gameName = game.Key;
                var gameDir = game.Value;
                
                var oldDir = DSC.Configuration.GetDirectory(gameName);

                // user decided to keep previous directory?
                if (String.Equals(gameDir, oldDir, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                // user specified an invalid directory?
                if (!String.IsNullOrEmpty(gameDir) && !Directory.Exists(gameDir))
                {
                    ShowError($"The specified directory for '{gameName}' is invalid -- its changes will be ignored.");
                    continue;
                }

                // update the setting
                if (!DSC.Configuration.SetDirectory(gameName, gameDir))
                {
                    ShowError($"An unexpected error occurred trying to save the directory for '{gameName}'!"
                        + "\r\nThis could indicate failure to write to the registry, or your settings file may be corrupted.");
                    continue;
                }
            }
        }

        protected bool m_ignoreChanges = false;

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!m_ignoreChanges && AreChangesPending)
            {
                if (!AskUserPrompt("You still have unsaved changes. Are you sure?"))
                    e.Cancel = true;
            }
        }

        public OptionsDialog()
        {
            InitializeComponent();

            m_gamePaths = new Dictionary<string, string>();

            RegisterGameDirectoryOption("Driv3r", txtDirDriv3r, btnDirDriv3r);
            RegisterGameDirectoryOption("DriverPL", txtDirDriverPL, btnDirDriverPL);
            RegisterGameDirectoryOption("DriverSF", txtDirDriverSF, btnDirDriverSF);

            btnOk.Click += (o, e) => {
                SaveChanges();
                Close();
            };

            btnCancel.Click += (o, e) => {
                var canClose = true;

                if (AreChangesPending)
                    canClose = AskUserPrompt("All pending changes will be lost. Are you sure?");

                if (canClose)
                {
                    m_ignoreChanges = true;
                    Close();
                }
            };
        }
    }
}
