using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
                    return m_bitmap.GetBitmapSource((BitmapSourceLoadFlags)m_imageLoadFlags);

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
            
            var piType = new PropertyItem("Type", delegate (string type) {
                MaterialType mtlType = MaterialType.Group;

                if (Enum.TryParse(type, out mtlType)
                    && (material.Type != mtlType))
                {
                    material.Type = mtlType;
                    AT.CurrentState.NotifyFileChange(material);

                    return true;
                }

                return false;
            }, material.Type.ToString());

            var piSpeed = new PropertyItem("Speed", delegate (string speed) {
                float value = 0.0f;

                if (float.TryParse(speed, out value)
                    && (material.AnimationSpeed != value))
                {
                    material.AnimationSpeed = value;
                    AT.CurrentState.NotifyFileChange(material);

                    return true;
                }

                return false;
            }, $"{material.AnimationSpeed:F2}");

            propPanelItems.Children.Clear();

            piType.AddToPanel(propPanelItems, Enum.GetNames(typeof(MaterialType)));
            piSpeed.AddToPanel(propPanelItems);

            m_contentInfo = String.Empty;

            OnPropertyChanged("CurrentImage");
            OnPropertyChanged("ContentInfo");
        }
        
        public void SetSubstance(ISubstanceData substance)
        {
            m_bitmap = null;
            
            var piBin = new PropertyItem("Bin", delegate (string bin) {
                RenderBinType renderBin = RenderBinType.ReflectedSky;

                if (Enum.TryParse(bin, out renderBin)
                    && (substance.Bin != renderBin))
                {
                    substance.Bin = renderBin;
                    AT.CurrentState.NotifyFileChange(substance);

                    return true;
                }

                return false;
            }, substance.Bin.ToString());

            var piFlags = new PropertyItem("Flags", delegate (string flags) {
                int value = 0;

                if (Utils.TryParseNumber(flags, out value)
                    && (substance.Flags != value))
                {
                    substance.Flags = value;
                    AT.CurrentState.NotifyFileChange(substance);

                    return true;
                }

                return false;
            }, $"0x{substance.Flags:X2}");

            propPanelItems.Children.Clear();

            piBin.AddToPanel(propPanelItems, Enum.GetNames(typeof(RenderBinType)));
            piFlags.AddToPanel(propPanelItems);

            var sb = new StringBuilder();
            var col = 12;
            
            sb.AppendLine("== Substance Information ==");
            
            int[] regs = {
                (substance.Mode & 0xFF),
                (substance.Mode >> 8),
                (substance.Type & 0xFF),
            };

            var slotFlags = (substance.Type >> 8);
            
            sb.AppendColumn("Registers", col, true).AppendLine($"{regs[0]} {regs[1]} {regs[2]}");
            sb.AppendColumn("SlotFlags", col, true).AppendLine($"0x{slotFlags:X}");

            if (substance is ISubstanceDataPC)
            {
                var substance_pc = (substance as ISubstanceDataPC);

                // flags?
                var eFlags = substance_pc.ExtraFlags;

                if (eFlags != 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("==== Extra Flags ====");

                    for (int i = 0; i < 8; i++)
                    {
                        int flg = (1 << i);

                        if (((int)eFlags & flg) != 0)
                        {
                            var flgStr = Enum.GetName(typeof(SubstanceExtraFlags), flg);

                            sb.AppendLine($"+{flgStr}");
                        }
                    }
                }

                sb.AppendLine();
                sb.AppendLine("==== Flags ====");

                sb.AppendColumn("Alpha", col, true).AppendLine(substance_pc.HasAlpha);
                sb.AppendColumn("Specular", col, true).AppendLine(substance_pc.IsSpecular);
                sb.AppendColumn("Emissive", col, true).AppendLine(substance_pc.IsEmissive);

                //--sb.AppendLine();
                //--sb.AppendLine("==== Debug Information ====");
                
                //--var resolved = substance_pc.GetResolvedData();
                //--
                //--var rst = (resolved >> 0) & 0xFF;
                //--var stage = (resolved >> 8) & 0xFFFF;
                //--var flags = (resolved >> 16) & 0xFFFF;
                //--
                //--sb.AppendColumn("Resolved", col, true).AppendLine("0x{0:X6} ; Resolved value by Driv3r", resolved);
                //--sb.AppendColumn(".rst", col, true).AppendLine("0x{0:X2}", rst);
                //--sb.AppendColumn(".stage", col, true).AppendLine("0x{0:X2}", stage);
                //--sb.AppendColumn(".flags", col, true).AppendLine("0x{0:X2}", flags);
                //--
                //--sb.AppendLine();
                //--sb.AppendColumn("FlagsTest", col, true).AppendLine("0x{0:X6} ; Flags from resolved data", substance_pc.GetCompiledFlags(resolved));    
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
            
            var piUID = new PropertyItem("UID", null, $"{tex.UID:X8}") { ReadOnly = true };
            var piHash = new PropertyItem("Hash", null, $"{tex.Handle:X8}") { ReadOnly = true };

            var piType = new PropertyItem("Type", delegate (string input) {
                int value = 0;

                if (Utils.TryParseNumber(input, out value)
                    && (texture.Type != value))
                {
                    texture.Type = value;
                    AT.CurrentState.NotifyFileChange(texture);

                    return true;
                }

                return false;
            }, $"{tex.Type}");

            var piFlags = new PropertyItem("Flags", delegate (string input) {
                int value = 0;

                if (Utils.TryParseNumber(input, out value)
                    && (texture.Flags != value))
                {
                    texture.Flags = value;
                    AT.CurrentState.NotifyFileChange(texture);

                    return true;
                }

                return false;
            }, $"{tex.Flags}");

            var piWidth = new PropertyItem("Width", null, $"{tex.Width}") { ReadOnly = true };
            var piHeight = new PropertyItem("Height", null, $"{tex.Height}") { ReadOnly = true };

            propPanelItems.Children.Clear();

            piUID.AddToPanel(propPanelItems);
            piHash.AddToPanel(propPanelItems);
            piType.AddToPanel(propPanelItems);
            piFlags.AddToPanel(propPanelItems);
            piWidth.AddToPanel(propPanelItems);
            piHeight.AddToPanel(propPanelItems);

            m_contentInfo = String.Empty;

            OnPropertyChanged("CurrentImage");
            OnPropertyChanged("ContentInfo");
        }
        
        public void Clear()
        {
            m_bitmap = null;
            m_contentInfo = "";

            OnPropertyChanged("CurrentImage");
            OnPropertyChanged("ContentInfo");

            propPanelItems.Children.Clear();
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
