using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using DSCript;
using DSCript.Models;

namespace Antilli
{
    /// <summary>
    /// Interaction logic for ImageWidget.xaml
    /// </summary>
    public partial class ImageWidget : UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string property)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

        protected bool SetValue<T>(ref T backingField, T value, string propertyName)
        {
            if (object.Equals(backingField, value))
            {
                return false;
            }

            backingField = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

        delegate void PropertyUpdateCallback(string updatedValue);

        class PropertyItem
        {
            private string m_value;

            public string Name { get; set; }

            public string Value
            {
                get { return m_value; }
            }

            PropertyUpdateCallback Callback { get; set; }

            public void AddToPanel(StackPanel panel)
            {
                var label = new Label() {
                    Content = $"{Name}:"
                };
                
                var txtBox = new TextBox() {
                    Text = Value
                };

                txtBox.KeyDown += (o, e) => {
                    if (e.Key == Key.Enter)
                    {
                        if (!String.Equals(m_value, txtBox.Text))
                        {
                            Callback(txtBox.Text);
                            m_value = txtBox.Text;
                        }
                    }
                };

                var grid = new Grid() {
                    Margin = new Thickness(4)
                };

                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

                Grid.SetColumn(txtBox, 1);

                panel.Children.Add(grid);
            }

            public PropertyItem(string name, PropertyUpdateCallback callback, string initialValue = "")
            {
                Name = name;
                Callback = callback;

                m_value = initialValue;
            }
        }

        string m_contentInfo;
        BitmapReference m_bitmap;
        int m_imageLoadFlags;
        
        public string ContentInfo
        {
            get { return m_contentInfo; }
        }

        public BitmapSource CurrentImage
        {
            get
            {
                if (m_bitmap != null)
                    return m_bitmap.ToBitmapSource((BitmapSourceLoadFlags)m_imageLoadFlags);

                return null;
            }
        }

        public int ImageLoadFlags
        {
            get { return m_imageLoadFlags; }
        }

        public void SetMaterial(IMaterialData material)
        {
            m_bitmap = null;

            var sb = new StringBuilder();
            var col = 12;

            sb.AppendLine("== Material Information ==");

            sb.AppendColumn("Type", col, true).AppendLine("{0}", material.Type);
            sb.AppendColumn("AnimSpeed", col, true).AppendLine("{0}", material.AnimationSpeed);

            m_contentInfo = sb.ToString();

            OnPropertyChanged("CurrentImage");
            OnPropertyChanged("ContentInfo");
        }

        public void SetSubstance(ISubstanceData substance)
        {
            m_bitmap = null;

            var sb = new StringBuilder();
            var col = 12;

            sb.AppendLine("== Substance Information ==");

            sb.AppendColumn("Bin", col, true).AppendLine($"{substance.Bin} ({substance.RenderBin})");
            sb.AppendColumn("Flags", col, true).AppendFormat("0x{0:X}", substance.Flags);

            if (substance.Flags != 0)
            {
                sb.Append(" (");

                for (int i = 0, ii = 0; i < 24; i++)
                {
                    var nFlg = (substance.Flags & (1 << i));

                    if (nFlg == 0)
                        continue;

                    var sFlg = $"FLAG_{nFlg}";

                    if (nFlg == 4)
                        sFlg = "Alpha";

                    if (ii != 0)
                        sb.Append(" | ");

                    sb.Append(sFlg);
                    ii++;
                }

                sb.Append(")");
            }

            sb.AppendLine();

            int[] regs = {
                (substance.Mode & 0xFF),
                (substance.Mode >> 8),
                (substance.Type & 0xFF),
            };

            var slotFlags = (substance.Type >> 8);

            //sb.AppendColumn("K1", col, true).AppendLine("{0} {1}", (substance.Mode & 0xFF), (substance.Mode >> 8));
            //sb.AppendColumn("K2", col, true).AppendLine("{0}", (substance.Type & 0xFF));
            //sb.AppendColumn("K3", col, true).AppendLine("0x{0:X}", (substance.Type >> 8));

            sb.AppendColumn("Registers", col, true).AppendLine($"{regs[0]} {regs[1]} {regs[2]}");
            sb.AppendColumn("SlotFlags", col, true).AppendLine($"0x{slotFlags:X}");

            if (substance is ISubstanceDataPC)
            {
                var substance_pc = (substance as ISubstanceDataPC);

                // flags?
                var k3 = (substance.Type >> 8);

                if (k3 != 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("==== Extra Flags ====");

                    if ((k3 & 0x1) != 0)
                        sb.AppendLine("+FLAG_1");
                    if ((k3 & 0x2) != 0)
                        sb.AppendLine("+FLAG_2");
                    if ((k3 & 0x4) != 0)
                        sb.AppendLine("+ColorMask");
                    if ((k3 & 0x8) != 0)
                        sb.AppendLine("+Damage");
                    if ((k3 & 0x10) != 0)
                        sb.AppendLine("+DamageWithColorMask");
                    if ((k3 & 0x20) != 0)
                        sb.AppendLine("+FLAG_32");
                    if ((k3 & 0x40) != 0)
                        sb.AppendLine("+FLAG_64");
                    if ((k3 & 0x80) != 0)
                        sb.AppendLine("+FLAG_128");
                }

                sb.AppendLine();
                sb.AppendLine("==== Flags ====");

                sb.AppendColumn("Alpha", col, true).AppendLine(substance_pc.HasAlpha);
                sb.AppendColumn("Specular", col, true).AppendLine(substance_pc.IsSpecular);
                sb.AppendColumn("Emissive", col, true).AppendLine(substance_pc.IsEmissive);

                sb.AppendLine();
                sb.AppendLine("==== Debug Information ====");
                
                var resolved = substance_pc.GetResolvedData();

                var rst = (resolved >> 0) & 0xFF;
                var stage = (resolved >> 8) & 0xFFFF;
                var flags = (resolved >> 16) & 0xFFFF;

                sb.AppendColumn("Resolved", col, true).AppendLine("0x{0:X6} ; Resolved value by Driv3r", resolved);
                sb.AppendColumn(".rst", col, true).AppendLine("0x{0:X2}", rst);
                sb.AppendColumn(".stage", col, true).AppendLine("0x{0:X2}", stage);
                sb.AppendColumn(".flags", col, true).AppendLine("0x{0:X2}", flags);

                sb.AppendLine();
                sb.AppendColumn("FlagsTest", col, true).AppendLine("0x{0:X6} ; Flags from resolved data", substance_pc.GetCompiledFlags(resolved));

                
            }

            m_contentInfo = sb.ToString();

            OnPropertyChanged("CurrentImage");
            OnPropertyChanged("ContentInfo");
        }

        public void SetTexture(ITextureData texture)
        {
            if (m_bitmap != null)
                m_bitmap = null;

            var textureRef = TextureCache.GetTexture(texture);

            m_bitmap = textureRef.Bitmap;

            var tex = textureRef.Data;

            var sb = new StringBuilder();
            var col = 12;

            sb.AppendLine("== Texture Information ==");

            sb.AppendColumn("UID", col, true).AppendLine($"{tex.UID:X8}");
            sb.AppendColumn("Type", col, true).AppendLine($"{tex.Type}");
            sb.AppendColumn("Flags", col, true).AppendLine($"0x{tex.Flags:X8}");

            sb.AppendColumn("Width", col, true).AppendLine($"{tex.Width}");
            sb.AppendColumn("Height", col, true).AppendLine($"{tex.Height}");
            
            m_contentInfo = sb.ToString();

            OnPropertyChanged("CurrentImage");
            OnPropertyChanged("ContentInfo");
        }

        private void ClearProperties()
        {
            propPanel.Children.Clear();
        }

        public void Clear()
        {
            m_bitmap = null;
            m_contentInfo = "";

            OnPropertyChanged("CurrentImage");
            OnPropertyChanged("ContentInfo");
        }

        public void Update()
        {
            OnPropertyChanged("CurrentImage");
        }

        public void OnKeyPressed(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
            case Key.OemPeriod:
                if (++m_imageLoadFlags > 2)
                    m_imageLoadFlags = 0;
                OnPropertyChanged("CurrentImage");
                break;
            case Key.OemComma:
                if (--m_imageLoadFlags < 0)
                    m_imageLoadFlags = 2;
                OnPropertyChanged("CurrentImage");
                break;
            }
        }
        
        public ImageWidget()
        {
            InitializeComponent();
        }
    }
}
