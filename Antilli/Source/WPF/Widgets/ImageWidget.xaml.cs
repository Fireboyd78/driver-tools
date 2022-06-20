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

            var piType = new PropertyItem("Type", PropertyItem.EnumParser<MaterialType>(
                (type) => material.Type != type,
                (type) =>
                {
                    material.Type = type;
                    AT.CurrentState.NotifyFileChange(material);
                }), material.Type);

            var piSpeed = new PropertyItem("Speed", PropertyItem.TryParser<float>(float.TryParse,
                (speed) => material.AnimationSpeed != speed,
                (speed) =>
                {
                    material.AnimationSpeed = speed;
                    AT.CurrentState.NotifyFileChange(material);
                }), $"{material.AnimationSpeed:F2}");

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
                    substance.TS1,
                    substance.TS2,
                    substance.TS3,
                };

                var slotFlags = substance.TextureFlags;

                sb.AppendColumn("TexSlots", col, true).AppendLine($"{regs[0]} {regs[1]} {regs[2]}");
                sb.AppendColumn("TexFlags", col, true).AppendLine($"0x{slotFlags:X}");
            }
            
            m_contentInfo = sb.ToString();

            OnPropertyChanged("CurrentImage");
            OnPropertyChanged("ContentInfo");
        }

        // substances have a lot of control over how stuff is rendered,
        // so making changes to one should update ALL the things!
        private void NotifySubstanceModified(ISubstanceData substance)
        {
            AT.CurrentState.NotifyFileChange(substance);

            // this can have various effects on things, so refresh all views
            AT.CurrentState.ModelView.Viewer.UpdateActiveModel();
            AT.CurrentState.UpdateEditors();
        }
        
        public void SetSubstance(ISubstanceData substance)
        {
            FreeTexture();

            // display the first texture from this substance
            var texture1 = substance.Textures.FirstOrDefault();

            if (texture1 != null)
                LoadTexture(texture1);

            var piBin = new PropertyItem("Bin", PropertyItem.EnumParser<RenderBinType>(
                (renderBin) => substance.Bin != renderBin,
                (renderBin) =>
                {
                    substance.Bin = renderBin;
                    NotifySubstanceModified(substance);
                }), substance.Bin);

            var piFlags = new PropertyItem("Flags", PropertyItem.TryParser<int>(Utils.TryParseNumber,
                (flags) => substance.Flags != flags,
                (flags) =>
                {
                    substance.Flags = flags;
                    NotifySubstanceModified(substance);
                }), $"0x{substance.Flags:X2}");

            var piTS1 = new PropertyItem("TS1", PropertyItem.TryParser<int>(Utils.TryParseNumber,
                (value) => substance.TS1 != value,
                (value) =>
                {
                    substance.TS1 = value;
                    NotifySubstanceModified(substance);
                }), substance.TS1);

            var piTS2 = new PropertyItem("TS2", PropertyItem.TryParser<int>(Utils.TryParseNumber,
                (value) => substance.TS2 != value,
                (value) =>
                {
                    substance.TS2 = value;
                    NotifySubstanceModified(substance);
                }), substance.TS2);

            var piTS3 = new PropertyItem("TS3", PropertyItem.TryParser<int>(Utils.TryParseNumber,
                (value) => substance.TS3 != value,
                (value) =>
                {
                    substance.TS3 = value;
                    NotifySubstanceModified(substance);
                }), substance.TS3);

            propPanelItems.Children.Clear();

            piBin.AddToPanel(propPanelItems, Enum.GetNames(typeof(RenderBinType)));
            piFlags.AddToPanel(propPanelItems);
            piTS1.AddToPanel(propPanelItems);
            piTS2.AddToPanel(propPanelItems);
            piTS3.AddToPanel(propPanelItems);

#if tex_flags_dropdown
            //
            // this would be nice if a multi-select combobox existed..
            // or maybe add a bunch of checkboxes and shit? lol
            //
            var piTextureFlags = new PropertyItem("TexFlags", PropertyItem.EnumParser<SubstanceExtraFlags>(
                (value) => substance.TextureFlags != (int)value,
                (value) =>
                {
                    substance.TextureFlags = (int)value;
                    NotifySubstanceModified(substance);
                }), (SubstanceExtraFlags)substance.TextureFlags);

            piTextureFlags.AddToPanel(propPanelItems, Enum.GetNames(typeof(SubstanceExtraFlags)));
#else
            var piTextureFlags = new PropertyItem("TextureFlags", PropertyItem.TryParser<int>(Utils.TryParseNumber,
                (value) => substance.TextureFlags != value,
                (value) =>
                {
                    substance.TextureFlags = value;
                    NotifySubstanceModified(substance);
                }), $"0x{substance.TextureFlags:X2}");

            piTextureFlags.AddToPanel(propPanelItems);
#endif

            var sb = new StringBuilder();
            var col = 12;
            
            if (substance is ISubstanceDataPC)
            {
                var substance_pc = (substance as ISubstanceDataPC);

                // flags?
                var eFlags = (SubstanceExtraFlags)substance.TextureFlags;

                if (eFlags != 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("==== Texture Flags ====");

                    for (int i = 0; i < 16; i++)
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

            var piType = new PropertyItem("Type", PropertyItem.TryParser<int>(Utils.TryParseNumber,
                (type) => tex.Type != type,
                (type) =>
                {
                    tex.Type = type;
                    AT.CurrentState.NotifyFileChange(tex);
                }), tex.Type);

            var piFlags = new PropertyItem("Flags", PropertyItem.TryParser<int>(Utils.TryParseNumber,
                (flags) => tex.Flags != flags,
                (flags) =>
                {
                    tex.Flags = flags;

                    AT.CurrentState.NotifyFileChange(tex);
                }), tex.Flags);

            var piWidth = new PropertyItem("Width", null, $"{tex.Width}") { ReadOnly = true };
            var piHeight = new PropertyItem("Height", null, $"{tex.Height}") { ReadOnly = true };

            propPanelItems.Children.Clear();

            piUID.AddToPanel(propPanelItems);
            piHash.AddToPanel(propPanelItems);
            piType.AddToPanel(propPanelItems);

            // don't add this for Driv3r PC!
            if (tex.Flags != TextureInfo.FLAGS_DRIV3R_PC)
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
