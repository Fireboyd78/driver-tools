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

            sb.AppendColumn("Animated", col, true).AppendLine("{0}", material.IsAnimated);
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

            sb.AppendColumn("Flags", col, true).AppendLine("0x{0:X8}", substance.Flags);

            sb.AppendColumn("Mode", col, true).AppendLine("0x{0:X4}", substance.Mode);
            sb.AppendColumn("Type", col, true).AppendLine("0x{0:X4}", substance.Type);

            if (substance is ISubstanceDataPC)
            {
                var substance_pc = (substance as ISubstanceDataPC);

                sb.AppendColumn("Transparent", col, true).AppendLine(substance_pc.Transparency);
                sb.AppendColumn("Damage", col, true).AppendLine(substance_pc.Damage);
                sb.AppendColumn("Mask", col, true).AppendLine(substance_pc.AlphaMask);
                sb.AppendColumn("Specular", col, true).AppendLine(substance_pc.Specular);
                sb.AppendColumn("Emissive", col, true).AppendLine(substance_pc.Emissive);
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
            m_contentInfo = "";

            OnPropertyChanged("CurrentImage");
            OnPropertyChanged("ContentInfo");
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
