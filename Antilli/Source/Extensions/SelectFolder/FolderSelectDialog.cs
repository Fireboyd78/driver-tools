using System;
using System.Reflection;
using System.Windows.Forms;

namespace Antilli
{
    /// <summary>
	/// Creates IWin32Window around an IntPtr
	/// </summary>
	public class WindowWrapper : IWin32Window
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="handle">Handle to wrap</param>
        public WindowWrapper(IntPtr handle)
        {
            _hwnd = handle;
        }

        /// <summary>
        /// Original ptr
        /// </summary>
        public IntPtr Handle
        {
            get { return _hwnd; }
        }

        private IntPtr _hwnd;
    }

    /// <summary>
    /// Wraps System.Windows.Forms.OpenFileDialog to make it present
    /// a vista-style dialog.
    /// </summary>
    public class FolderSelectDialog
	{
		// Wrapped dialog
		OpenFileDialog _this = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public FolderSelectDialog()
        {
            _this = new OpenFileDialog() {
                Filter = "Folders|\n",
                AddExtension = false,
                CheckFileExists = false,
                DereferenceLinks = true,
                Multiselect = false,
                ValidateNames = false,
            };
        }

		#region Properties

		/// <summary>
		/// Gets/Sets the initial folder to be selected. A null value selects the current directory.
		/// </summary>
		public string InitialDirectory
		{
			get { return _this.InitialDirectory; }
			set { _this.InitialDirectory = String.IsNullOrEmpty(value) ? Environment.CurrentDirectory : value; }
		}

		/// <summary>
		/// Gets/Sets the title to show in the dialog
		/// </summary>
		public string Title
		{
			get { return _this.Title; }
			set { _this.Title = value ?? "Select a folder"; }
		}

		/// <summary>
		/// Gets the selected folder
		/// </summary>
		public string SelectedPath
		{
			get { return _this.FileName; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Shows the dialog
		/// </summary>
		/// <returns>True if the user presses OK else false</returns>
		public bool ShowDialog()
		{
			return ShowDialog(IntPtr.Zero);
		}

		/// <summary>
		/// Shows the dialog
		/// </summary>
		/// <param name="hWndOwner">Handle of the control to be parent</param>
		/// <returns>True if the user presses OK else false</returns>
		public bool ShowDialog(IntPtr hWndOwner)
		{
			bool result = false;

			if (Environment.OSVersion.Version.Major >= 6)
			{
				var r = new Reflector("System.Windows.Forms");

				var IFileDialog_T = r.GetType("FileDialogNative.IFileDialog");
				var fileDialog = r.Call(_this, "CreateVistaDialog");

				r.Call(_this, "OnBeforeVistaDialog", fileDialog);

				var options = (uint)r.CallAs(typeof(FileDialog), _this, "GetOptions");
				options |= (uint)r.GetEnum("FileDialogNative.FOS", "FOS_PICKFOLDERS");

				r.CallAs(IFileDialog_T, fileDialog, "SetOptions", options);

				var vistaDialogEvents = r.New("FileDialog.VistaDialogEvents", _this);

				uint dwCookie = 0;
				var parameters = new object[] { vistaDialogEvents, /* out */ dwCookie };

				r.CallAs2(IFileDialog_T, fileDialog, "Advise", parameters);
				dwCookie = (uint)parameters[1];

				try
				{
					if ((int)r.CallAs(IFileDialog_T, fileDialog, "Show", hWndOwner) == 0)
						result = true;
				}
				finally
				{
					r.CallAs(IFileDialog_T, fileDialog, "Unadvise", dwCookie);
					GC.KeepAlive(vistaDialogEvents);
				}
			}
			else
			{
                var fbd = new FolderBrowserDialog() {
                    Description = Title,
                    SelectedPath = InitialDirectory,
                    ShowNewFolderButton = false,
                };

                if (fbd.ShowDialog(new WindowWrapper(hWndOwner)) == DialogResult.OK)
                {
                    _this.FileName = fbd.SelectedPath;
                    result = true;
                }
			}

			return result;
		}

		#endregion
	}
}
