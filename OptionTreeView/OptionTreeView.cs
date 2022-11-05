using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace OptionTreeView
{
    [Docking(DockingBehavior.Ask)]
    public partial class OptionTreeView: UserControl
    {
        #region Fields
        bool Changed;
        int VisibleIndex;
        string program;
        readonly ToolTip ToolTip1;
        readonly List<Panel> Panels;
        readonly object[] rgbs = new object[] { //RGB color codes chart
            "330000", "660000", "990000", "CC0000", "FF0000", "FF3333", "FF6666", "FF9999", "FFCCCC",
            "331900", "663300", "994C00", "CC6600", "FF8000", "FF9933", "FFB266", "FFCC99", "FFE5CC",
            "333300", "666600", "999900", "CCCC00", "FFFF00", "FFFF33", "FFFF66", "FFFF99", "FFFFCC",
            "193300", "336600", "4C9900", "66CC00", "80FF00", "99FF33", "B2FF66", "CCFF99", "E5FFCC",
            "003300", "006600", "009900", "00CC00", "00FF00", "33FF33", "66FF66", "99FF99", "CCFFCC",
            "003319", "006633", "00994C", "00CC66", "00FF80", "33FF99", "66FFB2", "99FFCC", "CCFFE5",
            "003333", "006666", "009999", "00CCCC", "00FFFF", "33FFFF", "66FFFF", "99FFFF", "CCFFFF",
            "001933", "003366", "004C99", "0066CC", "0080FF", "3399FF", "66B2FF", "99CCFF", "CCE5FF",
            "000033", "000066", "000099", "0000CC", "0000FF", "3333FF", "6666FF", "9999FF", "CCCCFF",
            "190033", "330066", "4C0099", "6600CC", "7F00FF", "9933FF", "B266FF", "CC99FF", "E5CCFF",
            "330033", "660066", "990099", "CC00CC", "FF00FF", "FF33FF", "FF66FF", "FF99FF", "FFCCFF",
            "330019", "660033", "99004C", "CC0066", "FF007F", "FF3399", "FF66B2", "FF99CC", "FFCCE5",
            "000000", "202020", "404040", "606060", "808080", "A0A0A0", "C0C0C0", "E0E0E0", "FFFFFF"};

        OpenFileDialog OpenCheatDialog = null;
        SaveFileDialog SaveCheatDialog = null;
        DataContractJsonSerializer JsonSerializer = null;
        enum Manage
        {
            Export,
            Import,
            Restore,
        }
        #endregion

        #region Constructors
        public OptionTreeView()
        {
            InitializeComponent();

            Changed = false;
            VisibleIndex = -1;
            ToolTip1 = new ToolTip();
            Panels = new List<Panel>();
        }
        #endregion

        #region Control Event
        private void OptionLeftView_AfterSelect(object sender, TreeViewEventArgs e) => DisplayPanel(e.Node.Index);

        private void OptionTreeView_Load(object sender, EventArgs e)
        {
            ParentForm.FormClosing += ParentForm_FormClosing;
            ParentForm.FormClosed += ParentForm_FormClosed;
        }

        private void ParentForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SelectNextControl((Control)sender, true, true, true, true); //Move the focus before closing to confirm whether there is an option in editing
            if (!Changed) return;

            if (MessageBox.Show("Do you want to save settings before leaving??", "OptionFormClosing", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            Default.Save();
            MessageBox.Show("Save", "OptionTreeView", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ParentForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            ToolTip1.RemoveAll();
            ToolTip1.Dispose();
            for (int idx = 0; idx < Panels.Count; idx++)
            {
                var panel = Panels[idx];
                if (panel.Controls.Count > 0) RecursiveDispose(panel.Controls[0]);
                panel.Dispose();
            }
            if (Controls.Count > 0) RecursiveDispose(Controls[0]);
            Panels.Clear();
            Controls.Clear();
            TreeGroupOptions.Clear();
            Dispose(true);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// https://stackoverflow.com/q/2047012
        /// </summary>
        private bool RecursiveDispose(Control ctrl)
        {
            try
            {
                bool isBreak = true;
                while (isBreak && ctrl.Controls.Count > 0) isBreak = RecursiveDispose(ctrl.Controls[0]);

                ctrl.MouseHover -= Control_MouseHover;
                if (ctrl is ComboBox comboBox)
                {
                    comboBox.DrawItem -= ColorBox_DrawItem;
                    comboBox.DrawItem -= FontBox_DrawItem;
                    comboBox.DrawItem -= new DrawItemEventHandler(ColorBox_DrawItem);
                    comboBox.DrawItem -= new DrawItemEventHandler(FontBox_DrawItem);
                    comboBox.SelectedIndexChanged -= ColorBox_SelectedIndexChanged;
                    comboBox.SelectedIndexChanged -= ColorNum_ValueChanged;
                    comboBox.SelectedIndexChanged -= Control_Changed;
                }
                else if (ctrl is CheckBox checkBox) checkBox.CheckedChanged -= Control_Changed;
                else if (ctrl is TextBox textBox) textBox.Leave -= Control_Changed;
                else if (ctrl is NumericUpDown upDown)
                {
                    upDown.ValueChanged -= Control_Changed;
                    upDown.ValueChanged -= ColorNum_ValueChanged;
                }

                ctrl.Dispose();
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Color Picker Combo Box
        /// https://stackoverflow.com/a/25616698
        /// </summary>
        private void ColorBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (!(sender is ComboBox comboBox) || e.Index < 0) return;

            Rectangle rect = e.Bounds; //Rectangle of item
            string itemName = comboBox.Items[e.Index].ToString(); //Get item color name
            bool isARGB = uint.TryParse(itemName, NumberStyles.HexNumber, null, out uint argb);
            if (argb < 0x01000000) argb += 0xFF000000;
            Color itemColor = isARGB ? Color.FromArgb((int)argb) : Color.FromName(itemName); //Get instance color from item name

            using (Graphics g = e.Graphics)
            using (Brush brush = new SolidBrush(itemColor)) //Get instance brush with Solid style to draw background
            using (Font itemFont = new Font(e.Font.FontFamily, e.Font.Size, FontStyle.Bold)) //Get instance a font to draw item name with this style
            { //Draw the background with my brush style and rectangle of item
                g.FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
                //Draw the item name
                g.DrawString(itemName, itemFont, itemColor.GetBrightness() >= 0.4 ? Brushes.Black : Brushes.White, rect.X, rect.Top);
            }
        }

        private void ColorBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(sender is ComboBox comboBox) || !(comboBox.SelectedItem is KnownColor knownColor)) return;

            Color itemColor = Color.FromKnownColor(knownColor);
            comboBox.ForeColor = itemColor.GetBrightness() >= 0.4 ? Color.Black : Color.White;
            comboBox.BackColor = itemColor;
        }

        /// <summary>
        /// Changing the format of a ComboBox item
        /// https://stackoverflow.com/a/10528683
        /// </summary>
        private void FontBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (!(sender is ComboBox comboBox) || e.Index < 0) return;

            using (Graphics g = e.Graphics)
            using (SolidBrush brush = new SolidBrush(e.ForeColor))
            using (Font font = new Font(comboBox.Items[e.Index].ToString(), e.Font.Size))
            {
                e.DrawBackground(); //Draw the background of the ComboBox for each item.
                g.DrawString(comboBox.Items[e.Index].ToString(), font, brush, e.Bounds);
                e.DrawFocusRectangle(); //If the ComboBox has focus, draw a focus rectangle around the selected item.
            }
        }

        private void Control_MouseHover(object sender, EventArgs e)
        {
            Control control = sender as Control;
            var option = ((object Value, string TreeName, string GroupName, string Name, string Description, uint Seq))control.Tag;
            ToolTip1.Show(option.Description, control, ShowToolTipDuration); //https://stackoverflow.com/a/8225836
        }

        private void Control_Changed(object sender, EventArgs e)
        {
            object newVal = null;
            Control control = sender as Control;
            var option = ((object Value, string TreeName, string GroupName, string Name, string Description, uint Seq))control.Tag;
            if (control is ComboBox comboBox) newVal = comboBox.SelectedItem;
            else if (control is CheckBox checkBox) newVal = checkBox.Checked;
            else if (control is NumericUpDown numericUpDown) newVal = numericUpDown.Value;
            else if (control is TextBox textBox) newVal = textBox.Text;

            if (option.Name.Length > 0 && newVal != null) UpdateSettings(option.Name, newVal);
        }

        private void ExoprtImport_Click(object sender, EventArgs e)
        {
            Button control = sender as Button;
            var option = ((object Value, string TreeName, string GroupName, string Name, string Description, uint Seq))control.Tag;
            Manage manage = (Manage)option.Value;
            try
            {
                if (manage == Manage.Export)
                {
                    SaveCheatDialog.Filter = "Settings Json (*.json)|*.json";
                    SaveCheatDialog.FileName = program + "Settings";
                    SaveCheatDialog.AddExtension = true;
                    SaveCheatDialog.RestoreDirectory = true;

                    if (SaveCheatDialog.ShowDialog() != DialogResult.OK) return;

                    var propertyInfos = Default.GetType().GetProperties();
                    List<OptionJson> optionJsons = new List<OptionJson>();
                    foreach (var propInfo in propertyInfos)
                    {
                        if (!propInfo.CanWrite) continue;

                        SettingsProperty property = Default.Properties[propInfo.Name];

                        if (property == null) continue;

                        string name = property.Name;
                        object value = Default[name];
                        if (value != null) value = value.ToString();
                        optionJsons.Add(new OptionJson(name, value));
                    }

                    using (var msObj = new MemoryStream())
                    {
                        JsonSerializer.WriteObject(msObj, optionJsons);
                        var bytes = msObj.ToArray();
                        string json = Encoding.UTF8.GetString(bytes, 0, bytes.Length).Replace("},{", "},\n{");
                        using (var myStream = new StreamWriter(SaveCheatDialog.FileName)) myStream.Write(json);
                    }
                }
                else if (manage == Manage.Import)
                {
                    OpenCheatDialog.Filter = "Settings Json (*.json)|*.json";
                    OpenCheatDialog.FileName = program + "Settings";
                    OpenCheatDialog.AddExtension = true;
                    OpenCheatDialog.RestoreDirectory = true;

                    if (OpenCheatDialog.ShowDialog() != DialogResult.OK) return;

                    string settingsText = File.ReadAllText(OpenCheatDialog.FileName);
                    using (var msObj = new MemoryStream(Encoding.UTF8.GetBytes(settingsText)))
                    {
                        List<OptionJson> optionJsons = (List<OptionJson>)JsonSerializer.ReadObject(msObj);
                        if (optionJsons == null || optionJsons.Count == 0) throw new Exception("optionJson is invalid\n" + settingsText);

                        foreach (var optionJson in optionJsons)
                        {
                            if (optionJson.Name == null || optionJson.Name == "") throw new Exception(String.Format("optionJson is invalid\nName:{0}, Value:{1}", optionJson.Name, optionJson.Value));
                            SettingsProperty property = Default.Properties[optionJson.Name];

                            Type optionInstanceType = property.PropertyType;
                            var converter = TypeDescriptor.GetConverter(optionInstanceType);
                            var x = converter.ConvertFrom(optionJson.Value);
                            UpdateSettings(optionJson.Name, x);
                        }
                        Default.Save();
                        Changed = false;
                    }
                    ToolTip1.RemoveAll();
                    for (int idx = 0; idx < Panels.Count; idx++)
                    {
                        var panel = Panels[idx];
                        if (panel.Controls.Count > 0) RecursiveDispose(panel.Controls[0]);
                        panel.Dispose();
                    }
                    Panels.Clear();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    InitSettings(Default);
                }
                else if (manage == Manage.Restore)
                {
                    if (MessageBox.Show("Are you sure you want to reset the settings to default?", "Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

                    Default.Reset();

                    ToolTip1.RemoveAll();
                    for (int idx = 0; idx < Panels.Count; idx++)
                    {
                        var panel = Panels[idx];
                        if (panel.Controls.Count > 0) RecursiveDispose(panel.Controls[0]);
                        panel.Dispose();
                    }
                    Panels.Clear();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    InitSettings(Default);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(String.Format("{0}\nManage:{1}\n{2}", exception.Message, manage, exception.StackTrace), exception.Source, MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        private void ColorNum_ValueChanged(object sender, EventArgs e)
        {
            Control control = sender as Control;
            bool isComboBox = control is ComboBox;
            var ColorR = control.Parent.Controls.Find("ColorR", false)[0] as NumericUpDown;
            var ColorG = control.Parent.Controls.Find("ColorG", false)[0] as NumericUpDown;
            var ColorB = control.Parent.Controls.Find("ColorB", false)[0] as NumericUpDown;
            var ColorBox = control.Parent.Controls.Find("ColorBox", false)[0] as ComboBox;

            if (isComboBox)
            {
                uint argb = uint.Parse(ColorBox.SelectedItem.ToString(), NumberStyles.HexNumber);
                if (argb < 0x01000000) argb += 0xFF000000;

                Color color = Color.FromArgb((int)argb);
                ColorR.Value = color.R;
                ColorG.Value = color.G;
                ColorB.Value = color.B;
            }
            ColorBox.BackColor = Color.FromArgb((int)ColorR.Value, (int)ColorG.Value, (int)ColorB.Value);
            if (!isComboBox) ColorBox.Text = ColorBox.BackColor.ToArgb().ToString("X");

            var option = ((object Value, string TreeName, string GroupName, string Name, string Description, uint Seq))control.Parent.Tag;
            if (option.Name.Length > 0) UpdateSettings(option.Name, ColorBox.BackColor);
        }
        #endregion

        #region properties Appearance
        /// <summary>
        /// Gets or sets the foreground color of OptionLeftView.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the foreground color of OptionLeftView.")]
        public Color ForeColorLeftView
        {
            get => OptionLeftView.ForeColor;
            set => OptionLeftView.ForeColor = value;
        }

        /// <summary>
        /// Gets or sets the background color of OptionLeftView.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the background color of OptionLeftView.")]
        public Color BackColorLeftView
        {
            get => OptionLeftView.BackColor;
            set => OptionLeftView.BackColor = value;
        }

        /// <summary>
        /// Gets or sets the border style of OptionLeftView.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the border style of OptionLeftView.")]
        public BorderStyle BorderStyleLeftView
        {
            get => OptionLeftView.BorderStyle;
            set => OptionLeftView.BorderStyle = value;
        }

        /// <summary>
        /// Gets or sets the font of OptionLeftView.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the font of OptionLeftView.")]
        public Font FontLeftView
        {
            get => OptionLeftView.Font;
            set => OptionLeftView.Font = value;
        }

        /// <summary>
        /// Gets or sets the height of each tree node in the OptionLeftView control.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the height of each tree node in the OptionLeftView.")]
        public int ItemHeightLeftView
        {
            get => OptionLeftView.ItemHeight;
            set => OptionLeftView.ItemHeight = value;
        }
        #endregion
        #region properties Behavior
        /// <summary>
        /// Gets or sets the ContextMenuStrip associated with this OptionLeftView.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets the ContextMenuStrip associated with this OptionLeftView.")]
        public ContextMenuStrip ContextMenuStripLeftView
        {
            get => OptionLeftView.ContextMenuStrip;
            set => OptionLeftView.ContextMenuStrip = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the selection highlight spans the width of OptionLeftView.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets a value indicating whether the selection highlight spans the width of OptionLeftView.")]
        public bool FullRowSelectLeftView
        {
            get => OptionLeftView.FullRowSelect;
            set => OptionLeftView.FullRowSelect = value;
        }

        /// <summary>
        /// Gets or sets whether to show the left tree view.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets whether to show the left tree view."), DefaultValue(true)]
        public bool ShowLeftView
        {
            get => showLeftView;
            set
            {
                showLeftView = value;
                SplitContainer1.Panel1Collapsed = !showLeftView;
            }
        }
        bool showLeftView = true;

        /// <summary>
        /// Gets or sets whether to display default names of unconfigured groups.
        /// Group default with 'Default' name when the group name is not specified in Settings.settings.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets whether to display default names of unconfigured groups.\n" +
            "Group default with 'Default' name when the group name is not specified in Settings.settings."), DefaultValue(true)]
        public bool ShowDefaultGroupName { get; set; } = true;

        /// <summary>
        /// Determine whether to sort by the values before the underline of tree name. Default is false.
        /// </summary>
        [Category("Behavior"), Description("Determine whether to sort by the values before the underline of tree name. Default is false."), DefaultValue(false)]
        public bool SortTreeBeforeUnderline { get; set; } = false;

        /// <summary>
        /// Determine whether to sort by the values before the underline of group name. Default is false.
        /// </summary>
        [Category("Behavior"), Description("Determine whether to sort by the values before the underline of group name. Default is false."), DefaultValue(false)]
        public bool SortGroupBeforeUnderline { get; set; } = false;

        /// <summary>
        /// Gets or sets the number of decimal places for floating-point numbers.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets the number of decimal places for floating-point numbers.")]
        public int FloatingPointDecimalPlaces { get; set; } = 2;

        /// <summary>
        /// Gets or sets the number of decimal places for floating-point numbers.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets the number containing the duration, in milliseconds, to display the ToolTip.")]
        public int ShowToolTipDuration { get; set; } = 15000;

        /// <summary>
        /// Get or set whether to automatically add spaces between CamelCases of GroupName. Default is true.
        /// </summary>
        [Category("Behavior"), Description("Get or set whether to automatically add spaces between CamelCases of GroupName. Default is true."), DefaultValue(true)]
        public bool InsertSpaceOnCamelCaseGroupName { get; set; } = true;

        /// <summary>
        /// Get or set whether to automatically add spaces between CamelCases of LabelName. Default is true.
        /// </summary>
        [Category("Behavior"), Description("Get or set whether to automatically add spaces between CamelCases of LabelName. Default is true."), DefaultValue(true)]
        public bool InsertSpaceOnCamelCaseLabelName { get; set; } = true;

        /// <summary>
        /// Get or set whether to automatically add spaces between CamelCases of LabelName. Default is true.
        /// </summary>
        [Category("Behavior"), Description("Get or set whether to automatically add spaces between CamelCases and number. Default is false. Example: Abc123DefGhi => Abc 123 Def Ghi"), DefaultValue(true)]
        public bool InsertSpaceOnCamelCaseNumber { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to display the export-import tree page. Default is true.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets whether to display the export-import tree page. Default is true."), DefaultValue(true)]
        public bool ShowExportImportTreePage { get; set; } = true;
        #endregion
        #region properties Layout
        /// <summary>
        /// Gets or sets a value determining whether OptionLeft is collapsed or expanded.
        /// </summary>
        [Category("Layout"), Description("Gets or sets a value determining whether OptionLeft is collapsed or expanded.")]
        public bool OptionLeftCollapsed
        {
            get => SplitContainer1.Panel1Collapsed;
            set => SplitContainer1.Panel1Collapsed = value;
        }

        /// <summary>
        /// Gets or sets the minimum distance in pixels of the OptionLeft.
        /// </summary>
        [Category("Layout"), Description("Gets or sets the minimum distance in pixels of the OptionLeft.")]
        public int OptionLeftMinSize
        {
            get => SplitContainer1.Panel1MinSize;
            set => SplitContainer1.Panel1MinSize = value;
        }

        /// <summary>
        /// Gets or sets a value determining whether OptionRight is collapsed or expanded.
        /// </summary>
        [Category("Layout"), Description("Gets or sets a value determining whether OptionRight is collapsed or expanded.")]
        public bool OptionRightCollapsed
        {
            get => SplitContainer1.Panel2Collapsed;
            set => SplitContainer1.Panel2Collapsed = value;
        }

        /// <summary>
        /// Gets or sets the minimum distance in pixels of the OptionRight.
        /// </summary>
        [Category("Layout"), Description("Gets or sets the minimum distance in pixels of the OptionRight.")]
        public int OptionRightMinSize
        {
            get => SplitContainer1.Panel2MinSize;
            set => SplitContainer1.Panel2MinSize = value;
        }

        /// <summary>
        /// Get or set the Label size in pixels of the OptionRight. Default 150.
        /// </summary>
        [Category("Layout"), Description("Get or set the Label size in pixels of the OptionRight. Default 150")]
        public float OptionRightLabelSize { get; set; } = 150;

        /// <summary>
        /// Gets or sets the location of the splitter, in pixels, from the left or top edge of the SplitContainer.
        /// </summary>
        [Category("Layout"), Description("Gets or sets the location of the splitter, in pixels, from the left or top edge of the SplitContainer.")]
        public int SplitterDistance
        {
            get => SplitContainer1.SplitterDistance;
            set => SplitContainer1.SplitterDistance = value;
        }

        /// <summary>
        /// Gets or sets a value representing the increment of splitter movement in pixels.
        /// </summary>
        [Category("Layout"), Description("Gets or sets a value representing the increment of splitter movement in pixels.")]
        public int SplitterIncrement
        {
            get => SplitContainer1.SplitterIncrement;
            set => SplitContainer1.SplitterIncrement = value;
        }

        /// <summary>
        /// Gets or sets the width of the splitter in pixels.
        /// </summary>
        [Category("Layout"), Description("Gets or sets the width of the splitter in pixels.")]
        public int SplitterWidth
        {
            get => SplitContainer1.SplitterWidth;
            set => SplitContainer1.SplitterWidth = value;
        }
        #endregion
        #region properties Hidden
        /// <summary>
        /// Application settings defaultInstance
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ApplicationSettingsBase Default { get; private set; }

        /// <summary>
        /// Remember the result of parsing the Settings.settings of properties
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<(dynamic Value, string TreeName, string GroupName, string Name, string Description, uint Seq)> TreeGroupOptions { get; private set; }
        #endregion

        #region Init TreeGroupOptions
        /// <summary>
        /// Parse Settings to TreeGroupOptions list
        /// </summary>
        /// <param name="default_">Properties.Settings.Default</param>
        /// <param name="OptionTypeSeparator">Gets or sets the Separator for Option<T>. Default is '|'</param>
        public void InitSettings(ApplicationSettingsBase default_, char OptionTypeSeparator = '|')
        {
            if (default_ == null || default_.Properties.Count == 0) throw new ArgumentException("InitSettings failed: Settings is null", "default_");

            uint seq = 0;
            Default = default_;
            program = Path.GetFileNameWithoutExtension(Default.GetType().Module.Name);

            OptionTypeConverter.Separator = OptionTypeSeparator;
            if (TreeGroupOptions != null) TreeGroupOptions.Clear();
            TreeGroupOptions = new List<(dynamic Value, string TreeName, string GroupName, string Name, string Description, uint Seq)>();

            var propertyInfos = Default.GetType().GetProperties();

            foreach (var propInfo in propertyInfos)
            {
                if (!propInfo.CanWrite) continue;

                SettingsProperty property = Default.Properties[propInfo.Name];

                if (property == null) continue;

                string name = property.Name;
                object value = Default[name];
                Type optionInstanceType = property.PropertyType; //value.GetType();
                object defaultValue = property.DefaultValue; //if (defaultValue == null && optionInstanceType.IsValueType) defaultValue = Activator.CreateInstance(optionInstanceType);

                object oValue = null;
                string oTreeName = null;
                string oGroupName = null;
                string oDescription = null;

                if (!optionInstanceType.IsGenericType) oValue = value;
                else if (optionInstanceType.GetGenericTypeDefinition() != typeof(Option<>)) continue;
                else
                {
                    TypeConverter optionTypeConverter = TypeDescriptor.GetConverter(optionInstanceType);
                    BaseOption defValue = optionTypeConverter.ConvertFrom(defaultValue) as BaseOption;
                    oTreeName = defValue.TreeName;
                    oGroupName = defValue.GroupName;
                    oDescription = defValue.Description;
                    oValue = value != null ? optionInstanceType.GetProperty("Value").GetValue(value, null) : defValue.BaseObject;
                    if (value == null) Default[name] = defValue;
                }

                if ((oTreeName ?? "") == "") oTreeName = "Default";
                if ((oGroupName ?? "") == "") oGroupName = ShowDefaultGroupName ? "Default" : "";
                TreeGroupOptions.Add((oValue, oTreeName, oGroupName, name, oDescription, seq++));
            }

            if (TreeGroupOptions.Count == 0) throw new ArgumentException("InitSettings failed: Options count is zero", "default_");

            TreeGroupOptions.Sort(CompareTreeGroupOption);

            InitPanels();
        }

        private int CompareTreeGroupOption(
            (object Value, string TreeName, string GroupName, string Name, string Description, uint Seq) o1,
            (object Value, string TreeName, string GroupName, string Name, string Description, uint Seq) o2)
        {
            int result;
            result = o1.TreeName.CompareTo(o2.TreeName);
            if (result != 0) return result;

            result = o1.GroupName.CompareTo(o2.GroupName);
            if (result != 0) return result;

            result = o1.Seq.CompareTo(o2.Seq);
            return result;
        }
        #endregion

        #region Init Treeview Panels
        private void InitPanels()
        {
            if (TreeGroupOptions.Count == 0) return;

            Panels.Clear();
            OptionLeftView.Nodes.Clear();
            TableLayoutPanel TablePanelTop = null;
            TableLayoutPanel TablePanelSub = null;
            GroupBox groupBox = null;
            Font numFont = new Font(Font.FontFamily, 8, FontStyle.Bold);
            (object Value, string TreeName, string GroupName, string Name, string Description, uint Seq) tmpOption = (null, null, null, null, null, 0);

            if (ShowExportImportTreePage)
            {
                TreeGroupOptions.Add((Manage.Export, "ExoprtImport", "Managing settings", "Export now", "Export now", (uint)TreeGroupOptions.Count));
                TreeGroupOptions.Add((Manage.Import, "ExoprtImport", "Managing settings", "Import now", "Import now", (uint)TreeGroupOptions.Count));
                TreeGroupOptions.Add((Manage.Restore, "ExoprtImport", "Managing settings", "Restore default", "Restore default", (uint)TreeGroupOptions.Count));

                if (OpenCheatDialog == null) OpenCheatDialog = new OpenFileDialog();
                if (SaveCheatDialog == null) SaveCheatDialog = new SaveFileDialog();
                if (JsonSerializer == null) JsonSerializer = new DataContractJsonSerializer(typeof(List<OptionJson>));
            }

            foreach ((object Value, string TreeName, string GroupName, string Name, string Description, uint Seq) option in TreeGroupOptions)
            {
                if (tmpOption.Name == null || tmpOption.TreeName != option.TreeName)
                { //Create new tree node and TableLayoutPanel when TreeName has changed
                    TreeNode OptionLeftNode = new TreeNode(SortTreeBeforeUnderline && option.TreeName.Contains("_") ? option.TreeName.Split(new char[] { '_' }, 2)[1] : option.TreeName);
                    OptionLeftNode.ForeColor = OptionLeftView.ForeColor;
                    OptionLeftNode.BackColor = OptionLeftView.BackColor;
                    OptionLeftView.Nodes.Add(OptionLeftNode);
                    if (TablePanelTop != null)
                    {
                        TablePanelTop.ResumeLayout(false);
                        TablePanelTop.PerformLayout();
                        Panels.Add(TablePanelTop);
                    }
                    TablePanelTop = new TableLayoutPanel();
                    TablePanelTop.SuspendLayout();
                    SplitContainer1.Panel2.Controls.Add(TablePanelTop);
                    TablePanelTop.ForeColor = base.ForeColor;
                    TablePanelTop.BackColor = base.BackColor;
                    TablePanelTop.AutoScroll = true;
                    TablePanelTop.ColumnCount = 1;
                    TablePanelTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                    TablePanelTop.Dock = DockStyle.Fill;
                    TablePanelTop.Location = SplitContainer1.Panel2.DisplayRectangle.Location;
                    TablePanelTop.Padding = new Padding(3);
                    TablePanelTop.Visible = false;
                }

                if (tmpOption.Name == null || tmpOption.TreeName != option.TreeName || tmpOption.GroupName != option.GroupName)
                { //Create new GroupBox and inner TableLayoutPanel when GroupName has changed
                    if (groupBox != null)
                    {
                        groupBox.ResumeLayout(false);
                        groupBox.PerformLayout();
                        TablePanelSub.ResumeLayout(false);
                        TablePanelSub.PerformLayout();
                    }
                    groupBox = new GroupBox();
                    groupBox.SuspendLayout();
                    TablePanelTop.Controls.Add(groupBox);
                    groupBox.ForeColor = base.ForeColor;
                    groupBox.BackColor = base.BackColor;

                    var groupText = SortGroupBeforeUnderline && option.GroupName.Contains("_") ? option.GroupName.Split(new char[] { '_' }, 2)[1] : option.GroupName;
                    groupBox.Text = InsertSpaceOnCamelCaseGroupName ? InsertSpaceOnCamelCase(groupText) : groupText;
                    groupBox.Dock = DockStyle.Fill;
                    groupBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    groupBox.AutoSize = true;
                    groupBox.Padding = new Padding(10);

                    TablePanelSub = new TableLayoutPanel();
                    groupBox.Controls.Add(TablePanelSub);
                    TablePanelSub.SuspendLayout();
                    TablePanelSub.ColumnCount = 2;
                    TablePanelSub.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, OptionRightLabelSize));
                    TablePanelSub.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                    TablePanelSub.Dock = DockStyle.Fill;
                    TablePanelSub.AutoSize = true;
                }

                Label label = new Label();
                label.SuspendLayout();
                label.MouseHover += Control_MouseHover;
                TablePanelSub.Controls.Add(label);
                TablePanelSub.RowCount++;
                TablePanelSub.RowStyles.Add(new RowStyle());

                label.Text = InsertSpaceOnCamelCaseLabelName ? InsertSpaceOnCamelCase(option.Name) : option.Name;
                label.AutoSize = true;
                label.Tag = option;
                label.Dock = DockStyle.Fill;
                label.Padding = new Padding(0, 5, 0, 0);

                Control control = null;
                bool isKnownColor = false;
                (int DecimalPlaces, decimal Increment, decimal Maximum, decimal Minimum, decimal Value) numeric = (0, 0, 0, 0, 0);
                if (option.Value is Manage manage)
                {
                    control = new Button();
                    control.SuspendLayout();
                    control.Tag = option;
                    control.Text = manage.ToString();
                    control.Click += ExoprtImport_Click;
                }
                else if (option.Value is sbyte sbyteVal    ) numeric = (0, 1, 127, -127, sbyteVal);
                else if (option.Value is short shortVal    ) numeric = (0, 1, 0x7FFF, -0x8000, shortVal);
                else if (option.Value is int intVal        ) numeric = (0, 1, 0x7FFFFFFF, -0x80000000, intVal);
                else if (option.Value is long longVal      ) numeric = (0, 1, 0x7FFFFFFFFFFFFFFF, -0x8000000000000000, longVal);
                else if (option.Value is byte byteVal      ) numeric = (0, 1, 255, 0, byteVal);
                else if (option.Value is ushort ushortVal  ) numeric = (0, 1, 0xFFFF, 0, ushortVal);
                else if (option.Value is uint uintVal      ) numeric = (0, 1, 0xFFFFFFFF, 0, uintVal);
                else if (option.Value is ulong ulongVal    ) numeric = (0, 1, 0xFFFFFFFFFFFFFFFF, 0, ulongVal);
                else if (option.Value is decimal decimalVal) numeric = (FloatingPointDecimalPlaces, 1, 0xFFFFFFFFFFFFFFFF, -0x8000000000000000, decimalVal);
                else if (option.Value is float floatVal    ) numeric = (FloatingPointDecimalPlaces, 1, 0xFFFFFFFFFFFFFFFF, -0x8000000000000000, new decimal(floatVal));
                else if (option.Value is double doubleVal  ) numeric = (FloatingPointDecimalPlaces, 1, 0xFFFFFFFFFFFFFFFF, -0x8000000000000000, new decimal(doubleVal));
                else if (option.Value is Enum enumVal)
                {
                    control = new ComboBox { FormattingEnabled = true };
                    control.Tag = option;
                    foreach (object enumObj in Enum.GetValues(enumVal.GetType())) ((ComboBox)control).Items.Add(enumObj);
                    if (enumVal.GetType().Name == "KnownColor")
                    {
                        isKnownColor = true;
                        ((ComboBox)control).DrawMode = DrawMode.OwnerDrawFixed;
                        ((ComboBox)control).DrawItem += new DrawItemEventHandler(ColorBox_DrawItem);
                        ((ComboBox)control).SelectedIndexChanged += ColorBox_SelectedIndexChanged;
                    }
                    ((ComboBox)control).SelectedItem = enumVal;
                    ((ComboBox)control).SelectedIndexChanged += Control_Changed;
                }
                else if (option.Value is Color color)
                {
                    var numR = new NumericUpDown() { Increment = 5, Maximum = 255, Minimum = 0, Value = color.R };
                    var numG = new NumericUpDown() { Increment = 5, Maximum = 255, Minimum = 0, Value = color.G };
                    var numB = new NumericUpDown() { Increment = 5, Maximum = 255, Minimum = 0, Value = color.B };
                    numR.ValueChanged += ColorNum_ValueChanged;
                    numG.ValueChanged += ColorNum_ValueChanged;
                    numB.ValueChanged += ColorNum_ValueChanged;
                    numR.Name = "ColorR";
                    numG.Name = "ColorG";
                    numB.Name = "ColorB";
                    numR.Dock = DockStyle.Fill;
                    numG.Dock = DockStyle.Fill;
                    numB.Dock = DockStyle.Fill;
                    numR.Margin = new Padding(0, 3, 0, 0);
                    numG.Margin = new Padding(0, 3, 0, 0);
                    numB.Margin = new Padding(0, 3, 0, 0);
                    numR.Font = numFont;
                    numG.Font = numFont;
                    numB.Font = numFont;
                    ComboBoxTransparent colorBox = new ComboBoxTransparent { FormattingEnabled = true };
                    colorBox.DrawMode = DrawMode.OwnerDrawFixed;
                    colorBox.DrawItem += new DrawItemEventHandler(ColorBox_DrawItem);
                    colorBox.SelectedIndexChanged += ColorNum_ValueChanged;
                    colorBox.FlatStyle = FlatStyle.Popup;
                    colorBox.Items.AddRange(rgbs);
                    colorBox.Name = "ColorBox";
                    colorBox.Dock = DockStyle.Fill;
                    colorBox.Margin = new Padding(0, 3, 0, 0);
                    if (color != default) colorBox.BackColor = color;

                    var TablePanelControl = new TableLayoutPanel();
                    TablePanelControl.Size = new Size(100, colorBox.Height);
                    TablePanelControl.SuspendLayout();
                    TablePanelControl.ColumnCount = 4;
                    TablePanelControl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
                    TablePanelControl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
                    TablePanelControl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
                    TablePanelControl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                    TablePanelControl.Tag = option;
                    TablePanelControl.Controls.Add(numR);
                    TablePanelControl.Controls.Add(numG);
                    TablePanelControl.Controls.Add(numB);
                    TablePanelControl.Controls.Add(colorBox);

                    TablePanelControl.ResumeLayout(false);
                    TablePanelControl.PerformLayout();

                    control = TablePanelControl;

                }
                else if (option.Value is FontFamily fontFamily)
                {
                    control = new ComboBox { FormattingEnabled = true };
                    control.Tag = option;
                    foreach (var enumObj in FontFamily.Families)
                    {
                        ((ComboBox)control).Items.Add(enumObj.Name);
                        if (enumObj.Name == fontFamily.Name) ((ComboBox)control).SelectedItem = enumObj.Name;
                    }

                    ((ComboBox)control).DrawMode = DrawMode.OwnerDrawFixed;
                    ((ComboBox)control).DrawItem += new DrawItemEventHandler(FontBox_DrawItem);
                    ((ComboBox)control).SelectedIndexChanged += Control_Changed;
                }
                else if (option.Value is bool boolVal)
                {
                    control = new CheckBox { Checked = boolVal };
                    control.Tag = option;
                    control.Text = "Enable";
                    ((CheckBox)control).CheckedChanged += Control_Changed;
                }
                else
                {
                    control = new TextBox();
                    control.SuspendLayout();
                    control.Tag = option;
                    control.Text = option.Value as string;
                    control.Leave += Control_Changed;
                }

                if (numeric.Maximum != 0)
                {
                    control = new NumericUpDown
                    {
                        DecimalPlaces = numeric.DecimalPlaces,
                        Increment = numeric.Increment,
                        Maximum = numeric.Maximum,
                        Minimum = numeric.Minimum,
                        Value = numeric.Value,
                        ThousandsSeparator = true
                    };
                    control.Tag = option;
                    ((NumericUpDown)control).ValueChanged += Control_Changed;
                }

                TablePanelSub.Controls.Add(control);
                if (!isKnownColor)
                {
                    control.ForeColor = base.ForeColor;
                    control.BackColor = base.BackColor;
                }
                control.ImeMode = ImeMode.Off;
                control.Dock = DockStyle.Fill;
                control.MouseHover += Control_MouseHover;

                tmpOption = option;
            }

            if (groupBox != null)
            {
                groupBox.ResumeLayout(false);
                groupBox.PerformLayout();
                TablePanelSub.ResumeLayout(false);
                TablePanelSub.PerformLayout();
            }
            if (TablePanelTop == null) return;

            TablePanelTop.ResumeLayout(false);
            TablePanelTop.PerformLayout();
            Panels.Add(TablePanelTop);
            DisplayPanel(0);
        }

        /// <summary>
        /// Splitting Pascal/Camel Case with RegEx Enhancements
        /// https://www.codeproject.com/Articles/108996/Splitting-Pascal-Camel-Case-with-RegEx-Enhancement
        /// Insert spaces between words on a camel-cased token
        /// https://stackoverflow.com/a/5796427
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private string InsertSpaceOnCamelCase(string text)
        {
            if (text == null || text.Length <= 2) return text;

            string pattern = InsertSpaceOnCamelCaseNumber ? @"(?<!^)([A-Z][0-9a-z]|(?<=[a-z])[0-9A-Z]|(?<=[0-9])[A-Z])" : @"(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])";
            return Regex.Replace(text, pattern, " $1");
        }

        private void UpdateSettings(string name, object newVal)
        {
            try
            {
                object value = Default[name];
                SettingsProperty property = Default.Properties[name];
                Type optionInstanceType = property.PropertyType; //value.GetType();

                if (!optionInstanceType.IsGenericType) Default[name] = newVal;
                else
                {
                    Type innerType = !optionInstanceType.IsGenericType ? optionInstanceType : optionInstanceType.GetGenericArguments()[0];
                    object newValue;
                    if (newVal is BaseOption baseOption) newValue = baseOption.BaseObject;
                    else if(innerType.Name == "FontFamily") newValue = new FontFamily(newVal.ToString());
                    else if (newVal is Color) newValue = (Color)newVal;
                    else
                    {
                        TypeConverter innerTypeConverter = TypeDescriptor.GetConverter(innerType);
                        newValue = innerTypeConverter.ConvertFrom(newVal.ToString());
                    }
                    optionInstanceType.GetProperty("Value").SetValue(value, newValue);
                }
                if (!Changed) Changed = true;
            }
            catch (Exception exception)
            {
                MessageBox.Show(String.Format("{0}\nname:{1} newVal:{2}\n{3}", exception.Message, name, newVal, exception.StackTrace), exception.Source, MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        /// <summary>
        /// Display the appropriate Panel.
        /// </summary>
        private void DisplayPanel(int index)
        {
            if (Panels.Count < 1 || VisibleIndex != -1 && VisibleIndex == index) return; // If this is the same Panel, do nothing.
            if (VisibleIndex != -1) Panels[VisibleIndex].Visible = false; // Hide the previously visible Panel.

            VisibleIndex = index;
            Panels[index].Visible = true; // Display the appropriate Panel.

            GC.Collect();
        }
        #endregion

        /// <summary>
        /// Customize a ComboBox that supports SupportsTransparentBackColor
        /// </summary>
        public class ComboBoxTransparent : ComboBox
        {
            /// <summary>
            /// Avoid throwing transparent back color not allowed error
            /// </summary>
            public ComboBoxTransparent() => SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }
    }
}
