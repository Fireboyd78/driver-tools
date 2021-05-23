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
        
        Texture m_texture;
        int m_imageLoadFlags;
        string m_contentInfo;

        public Texture CurrentTexture
        {
            get { return m_texture; }
        }

        public BitmapSource CurrentImage
        {
            get
            {
                if (m_texture != null)
                    return m_texture.Bitmap.GetBitmapSource((BitmapSourceLoadFlags)m_imageLoadFlags);

                return null;
            }
        }

        public int ImageLoadFlags
        {
            get { return m_imageLoadFlags; }
        }

        public string ContentInfo
        {
            get { return m_contentInfo; }
        }

        public Texture LoadTexture(ITextureData texture)
        {
            if (m_texture != null)
                TextureCache.Release(m_texture);

            m_texture = TextureCache.GetTexture(texture);

            return m_texture;
        }

        public void FreeTexture()
        {
            if (m_texture != null)
            {
                TextureCache.Release(m_texture);
                m_texture = null;
            }
        }

        public void SetMaterial(IMaterialData material)
        {
            FreeTexture();

            // display the first texture from the first substance of this material
            var substance1 = material.Substances.FirstOrDefault();

            if (substance1 != null)
            {
                var texture1 = substance1.Textures.FirstOrDefault();

                if (texture1 != null)
                    LoadTexture(texture1);
            }
            
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

            var sb = new StringBuilder();
            var col = 12;

            //
            // quick and dirty debug information
            //
            var idx = 0;

            foreach (var substance in material.Substances)
            {
                sb.AppendLine($"== Substance {++idx} ==");

                int[] regs = {
                    (substance.Mode & 0xFF),
                    (substance.Mode >> 8),
                    (substance.Type & 0xFF),
                };

                var slotFlags = (substance.Type >> 8);

                sb.AppendColumn("Registers", col, true).AppendLine($"{regs[0]} {regs[1]} {regs[2]}");
                sb.AppendColumn("SlotFlags", col, true).AppendLine($"0x{slotFlags:X}");
            }
            
            m_contentInfo = sb.ToString();

            OnPropertyChanged("CurrentImage");
            OnPropertyChanged("ContentInfo");
        }
        
        public void SetSubstance(ISubstanceData substance)
        {
            FreeTexture();

            // display the first texture from this substance
            var texture1 = substance.Textures.FirstOrDefault();

            if (texture1 != null)
                LoadTexture(texture1);
            
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

#if DEBUG
                sb.AppendLine();
                sb.AppendLine("==== Debug Information ====");
                
                var data = substance_pc.GetData(false);

                // resolve it over and over again
                for (int i = 0; i < 4; i++)
                {
                    SubstanceInfo.Resolve(ref data);

                    sb.AppendColumn($"Resolved ({i + 1})", col, true).AppendLine();
                    sb.AppendColumn(".bin", col, true).AppendLine("{0}", data.Bin);
                    sb.AppendColumn(".flg", col, true).AppendLine("0x{0:X2}", data.Flags);
                    sb.AppendColumn(".ts1", col, true).AppendLine("{0}", data.TS1);
                    sb.AppendColumn(".ts2", col, true).AppendLine("{0}", data.TS2);
                    sb.AppendColumn(".ts3", col, true).AppendLine("{0}", data.TS3);
                    sb.AppendColumn(".tsf", col, true).AppendLine("0x{0:X2}", data.TextureFlags);

                    SubstanceInfo.Compile(ref data);

                    sb.AppendColumn($"Compiled ({i + 1})", col, true).AppendLine();
                    sb.AppendColumn(".bin", col, true).AppendLine("{0}", data.Bin);
                    sb.AppendColumn(".flg", col, true).AppendLine("0x{0:X2}", data.Flags);
                    sb.AppendColumn(".ts1", col, true).AppendLine("{0}", data.TS1);
                    sb.AppendColumn(".ts2", col, true).AppendLine("{0}", data.TS2);
                    sb.AppendColumn(".ts3", col, true).AppendLine("{0}", data.TS3);
                    sb.AppendColumn(".tsf", col, true).AppendLine("0x{0:X2}", data.TextureFlags);
                }
#endif
            }

            m_contentInfo = sb.ToString();

            OnPropertyChanged("CurrentImage");
            OnPropertyChanged("ContentInfo");
        }

        public void SetTexture(ITextureData texture)
        {
            var textureRef = LoadTexture(texture);

            var tex = textureRef.Data;
            
            var piUID = new PropertyItem("UID", null, $"{tex.UID:X8}") { ReadOnly = true };
            var piHash = new PropertyItem("Handle", null, $"{tex.Handle:X8}") { ReadOnly = true };

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
            FreeTexture();

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
