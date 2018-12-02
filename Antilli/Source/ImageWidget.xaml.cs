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

            var lookup = new Dictionary<int, string>() {
                    { 0, "ReflectedSky" },                              //  0
                    { 1, "Portal" },                                    //  1
                    
                    { 4, "Building" },                                  // -1
                    { 5, "Clutter" },                                   // -1

                    { 3, "Road" },                                      //  6
                    { 35, "PostRoad" },                                 //  7
                    
                    { 39, "PreWater" },                                 //  9
                    { 21, "FarWater" },                                 // 10
                    { 23, "CarInterior" },                              // 11
                    { 6, "Car" },                                       // 12
                    
                    { 26, "FarWater_2" },                               // 14
                    { 27, "FarWater_3" },                               // 15
                    { 20, "NearWater" },                                // 16
                    
                    { 25, "CarOverlay" },                               // 17
                    { 22, "GrimeOverlay" },                             // 19

                    { 2, "Sky" },                                       // 21
                    { 24, "LowPoly" },                                  // 22

                    { 34, "GlowingLight" },                             // 25
                    
                    { 37, "DrawAlphaLast" },                            // 28
                    { 30, "FullBrightOverlay" },                        // 29
                    { 7, "Particle" },                                  // 30

                    { 33, "ShadowedParticle" },                         // -1
                    
                    { 8, "MissionIcon" },                               // 32

                    { 12, "OverheadMap_1" },                            // 33
                    { 13, "OverheadMap_2" },                            // 34
                    { 14, "OverheadMap_3" },                            // 35
                    { 15, "OverheadMap_4" },                            // 36
                    { 16, "OverheadMap_5" },                            // 37
                    { 17, "OverheadMap_6" },                            // 38
                    { 18, "OverheadMap_7" },                            // 39
                    { 19, "OverheadMap_8" },                            // 40
                    { 28, "OverheadMap_9" },                            // 41
                    { 29, "OverheadMap_10" },                           // 42
                    { 31, "OverheadMap_11" },                           // 43
                    { 32, "OverheadMap_12" },                           // 44
                    
                    { 40, "Overlay_0_25" },                             // 46
                    { 10, "Overlay_0_5" },                              // 47
                    { 9, "Overlay_1_0" },                               // 48
                    { 11, "Overlay_1_5" },                              // 49
                    { 36, "Overlay_2_0" },                              // -1

                    { 38, "UntexturedSemiTransparent" },                // 51

                    { 48, "Trees" },

                    { 49, "Menus_0_25" },
                    { 50, "Menus_0_5" },
                    { 51, "Menus_0_75" },
                    { 52, "Menus_1_0" },
                    { 53, "Menus_1_25" },
                    { 54, "Menus_1_5" },
                    { 55, "Menus_1_75" },
                    { 56, "Menus_2_0" },

                    { 57, "Clouds" },
                    { 58, "Hyperlow" },
                    { 59, "LightFlare" },

                    { 60, "OverlayMask_1" },
                    { 61, "OverlayMask_2" },

                    { 62, "TreeWall" },
                    { 63, "BridgeWires" },
                };

            var piBin = new PropertyItem("Bin", delegate (string bin) {
                foreach (var kv in lookup)
                {
                    if (String.Equals(kv.Value, bin))
                    {
                        substance.Bin = kv.Key;
                        AT.CurrentState.NotifyFileChange(substance);

                        return true;
                    }
                }

                return false;
            }, lookup[substance.Bin]);

            var piFlags = new PropertyItem("Flags", delegate (string flags) {
                int value = 0;

                if (Utils.TryParseNumber(flags, out value))
                {
                    substance.Flags = value;
                    AT.CurrentState.NotifyFileChange(substance);

                    return true;
                }

                return false;
            }, $"0x{substance.Flags:X2}");

            propPanelItems.Children.Clear();

            piBin.AddToPanel(propPanelItems, lookup.Select((kv) => kv.Value).ToArray());
            piFlags.AddToPanel(propPanelItems);

            var sb = new StringBuilder();
            var col = 12;
            
            sb.AppendLine("== Substance Information ==");

            //--sb.AppendColumn("Bin", col, true).AppendLine($"{substance.Bin} ({substance.RenderBin})");
            //--sb.AppendColumn("Flags", col, true).AppendFormat("0x{0:X}", substance.Flags);
            
            //--if (substance.Flags != 0)
            //--{
            //--    sb.Append(" (");
            //--
            //--    for (int i = 0, ii = 0; i < 24; i++)
            //--    {
            //--        var nFlg = (substance.Flags & (1 << i));
            //--
            //--        if (nFlg == 0)
            //--            continue;
            //--
            //--        var sFlg = $"FLAG_{nFlg}";
            //--
            //--        if (nFlg == 4)
            //--            sFlg = "Alpha";
            //--
            //--        if (ii != 0)
            //--            sb.Append(" | ");
            //--
            //--        sb.Append(sFlg);
            //--        ii++;
            //--    }
            //--
            //--    sb.Append(")");
            //--}

            //--sb.AppendLine();

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
