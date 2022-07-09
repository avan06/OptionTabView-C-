using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;

namespace OptionTreeView
{
    [Docking(DockingBehavior.Ask)]
    public partial class OptionTreeView: UserControl
    {
        #region Fields
        bool Changed;
        ToolTip ToolTip1;
        Panel VisiblePanel;
        readonly List<Panel> Panels;
        #endregion

        #region Constructors
        public OptionTreeView()
        {
            InitializeComponent();

            Changed = false;
            ToolTip1 = new ToolTip();
            VisiblePanel = null;
            Panels = new List<Panel>();
            TreeGroupOptions = new List<(object Value, string TreeName, string GroupName, string Name, string Description)>();
        }
        #endregion

        #region Control Event
        private void OptionTreeView_Load(object sender, EventArgs e) => ParentForm.FormClosing += ParentForm_FormClosing;

        private void ParentForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SelectNextControl((Control)sender, true, true, true, true); //Move the focus before closing to confirm whether there is an option in editing
            if (!Changed) return;

            if (MessageBox.Show("Do you want to save settings before leaving??", "OptionFormClosing", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            Default.Save();
            MessageBox.Show("Save", "OptionTreeView", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// https://stackoverflow.com/a/25616698
        /// </summary>
        private void ColorBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            using (Graphics g = e.Graphics)
            {
                Rectangle rect = e.Bounds; //Rectangle of item
                //Get item color name
                string itemName = ((ComboBox)sender).Items[e.Index].ToString();
                //Get instance color from item name
                Color itemColor = Color.FromName(itemName);
                //Get instance brush with Solid style to draw background
                Brush brush = new SolidBrush(itemColor);
                //Draw the background with my brush style and rectangle of item
                g.FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
                //Get instance a font to draw item name with this style
                Font itemFont = new Font(e.Font.FontFamily, e.Font.Size, FontStyle.Bold);
                //Draw the item name
                g.DrawString(itemName, itemFont, itemColor.GetBrightness() >= 0.4 ? Brushes.Black : Brushes.White, rect.X, rect.Top);
            }
        }
        #endregion

        #region properties Appearance
        /// <summary>
        /// Gets or sets the foreground color of OptionLeftView.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the foreground color of OptionLeftView."), DefaultValue(typeof(Color), "White")]
        public Color ForeColorLeftView
        {
            get { return OptionLeftView.ForeColor; }
            set { OptionLeftView.ForeColor = value; }
        }

        /// <summary>
        /// Gets or sets the background color of OptionLeftView.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the background color of OptionLeftView."), DefaultValue(typeof(Color), "White")]
        public Color BackColorLeftView
        {
            get { return OptionLeftView.BackColor; }
            set { OptionLeftView.BackColor = value; }
        }

        /// <summary>
        /// Gets or sets the border style of OptionLeftView.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the border style of OptionLeftView.")]
        public BorderStyle BorderStyleLeftView
        {
            get { return OptionLeftView.BorderStyle; }
            set { OptionLeftView.BorderStyle = value; }
        }

        /// <summary>
        /// Gets or sets the font of OptionLeftView.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the font of OptionLeftView.")]
        public Font FontLeftView
        {
            get { return OptionLeftView.Font; }
            set { OptionLeftView.Font = value; }
        }

        /// <summary>
        /// Gets or sets the height of each tree node in the OptionLeftView control.
        /// </summary>
        [Category("Appearance"), Description("Gets or sets the height of each tree node in the OptionLeftView.")]
        public int ItemHeightLeftView
        {
            get { return OptionLeftView.ItemHeight; }
            set { OptionLeftView.ItemHeight = value; }
        }
        #endregion
        #region properties Behavior
        /// <summary>
        /// Gets or sets the ContextMenuStrip associated with this OptionLeftView.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets the ContextMenuStrip associated with this OptionLeftView.")]
        public ContextMenuStrip ContextMenuStripLeftView
        {
            get { return OptionLeftView.ContextMenuStrip; }
            set { OptionLeftView.ContextMenuStrip = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the selection highlight spans the width of OptionLeftView.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets a value indicating whether the selection highlight spans the width of OptionLeftView.")]
        public bool FullRowSelectLeftView
        {
            get { return OptionLeftView.FullRowSelect; }
            set { OptionLeftView.FullRowSelect = value; }
        }

        /// <summary>
        /// Gets or sets whether to show the left tree view.
        /// </summary>
        [Category("Behavior"), Description("Gets or sets whether to show the left tree view."), DefaultValue(true)]
        public bool ShowLeftView {
            get { return showLeftView; }
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
        [Category("Behavior"), Description("Gets or sets the number of decimal places for floating-point numbers."), DefaultValue(true)]
        public int FloatingPointDecimalPlaces { get; set; } = 2;
        #endregion
        #region properties Layout

        /// <summary>
        /// Gets or sets a value determining whether OptionLeft is collapsed or expanded.
        /// </summary>
        [Category("Layout"), Description("Gets or sets a value determining whether OptionLeft is collapsed or expanded.")]
        public bool OptionLeftCollapsed
        {
            get { return SplitContainer1.Panel1Collapsed; }
            set { SplitContainer1.Panel1Collapsed = value; }
        }

        /// <summary>
        /// Gets or sets the minimum distance in pixels of the OptionLeft.
        /// </summary>
        [Category("Layout"), Description("Gets or sets the minimum distance in pixels of the OptionLeft.")]
        public int OptionLeftMinSize
        {
            get { return SplitContainer1.Panel1MinSize; }
            set { SplitContainer1.Panel1MinSize = value; }
        }

        /// <summary>
        /// Gets or sets a value determining whether OptionRight is collapsed or expanded.
        /// </summary>
        [Category("Layout"), Description("Gets or sets a value determining whether OptionRight is collapsed or expanded.")]
        public bool OptionRightCollapsed
        {
            get { return SplitContainer1.Panel2Collapsed; }
            set { SplitContainer1.Panel2Collapsed = value; }
        }

        /// <summary>
        /// Gets or sets the minimum distance in pixels of the OptionRight.
        /// </summary>
        [Category("Layout"), Description("Gets or sets the minimum distance in pixels of the OptionRight.")]
        public int OptionRightMinSize
        {
            get { return SplitContainer1.Panel2MinSize; }
            set { SplitContainer1.Panel2MinSize = value; }
        }

        /// <summary>
        /// Gets or sets the location of the splitter, in pixels, from the left or top edge of the SplitContainer.
        /// </summary>
        [Category("Layout"), Description("Gets or sets the location of the splitter, in pixels, from the left or top edge of the SplitContainer.")]
        public int SplitterDistance
        {
            get { return SplitContainer1.SplitterDistance;}
            set { SplitContainer1.SplitterDistance = value; }
        }

        /// <summary>
        /// Gets or sets a value representing the increment of splitter movement in pixels.
        /// </summary>
        [Category("Layout"), Description("Gets or sets a value representing the increment of splitter movement in pixels.")]
        public int SplitterIncrement
        {
            get { return SplitContainer1.SplitterIncrement; }
            set { SplitContainer1.SplitterIncrement = value; }
        }

        /// <summary>
        /// Gets or sets the width of the splitter in pixels.
        /// </summary>
        [Category("Layout"), Description("Gets or sets the width of the splitter in pixels.")]
        public int SplitterWidth
        {
            get { return SplitContainer1.SplitterWidth; }
            set { SplitContainer1.SplitterWidth = value; }
        }
        #endregion
        #region properties Hidden
        /// <summary>
        /// Application settings defaultInstance
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SettingsBase Default { get; private set; }

        /// <summary>
        /// Remember the result of parsing the Settings.settings of properties
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<(object Value, string TreeName, string GroupName, string Name, string Description)> TreeGroupOptions { get; private set; }
        #endregion

        #region public method
        #region Init TreeGroupOptions
        /// <summary>
        /// Parse Settings to TreeGroupOptions list
        /// </summary>
        /// <param name="default_">Properties.Settings.Default</param>
        /// <param name="OptionTypeSeparator">Gets or sets the Separator for Option<T>. Default is '|'</param>
        public void InitSettings(SettingsBase default_, char OptionTypeSeparator = '|')
        {
            if (default_ == null || default_.Properties.Count == 0) throw new ArgumentException("InitSettings failed: Settings is null", "default_");
            
            Default = default_;
            OptionTypeConverter.Separator = OptionTypeSeparator;

            foreach (SettingsProperty property in Default.Properties)
            {
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
                TreeGroupOptions.Add((oValue, oTreeName, oGroupName, name, oDescription));
            }

            if (TreeGroupOptions.Count == 0) throw new ArgumentException("InitSettings failed: Options count is zero", "default_");

            TreeGroupOptions.Sort(CompareMemoryEntry);

            InitPanels();
        }

        private int CompareMemoryEntry((object Value, string TreeName, string GroupName, string Name, string Description) o1, (object Value, string TreeName, string GroupName, string Name, string Description) o2)
        {
            int result;
            result = o1.TreeName.CompareTo(o2.TreeName);
            if (result != 0) return result;

            result = o1.GroupName.CompareTo(o2.GroupName);
            if (result != 0) return result;

            result = o1.Name.CompareTo(o2.Name);
            return result;
        }
        #endregion

        #region Init Treeview Panels
        private void InitPanels()
        {
            if (TreeGroupOptions.Count == 0) return;

            TableLayoutPanel TablePanelTop = null;
            TableLayoutPanel TablePanelSub = null;
            GroupBox groupBox = null;
            (object Value, string TreeName, string GroupName, string Name, string Description) tmpOption = (null, null, null, null, null);
            foreach ((object Value, string TreeName, string GroupName, string Name, string Description) option in TreeGroupOptions)
            {
                if (tmpOption.Name == null || tmpOption.TreeName != option.TreeName)
                { //Create new tree node and TableLayoutPanel when TreeName has changed
                    TreeNode OptionLeftNode = new TreeNode(SortTreeBeforeUnderline && option.TreeName.Contains("_") ? option.TreeName.Split(new char[] { '_' }, 2)[1] : option.TreeName);
                    OptionLeftNode.ForeColor = base.ForeColor;
                    OptionLeftNode.BackColor = base.BackColor;
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
                    groupBox.Text = SortGroupBeforeUnderline && option.GroupName.Contains("_") ? option.GroupName.Split(new char[] { '_' }, 2)[1] : option.GroupName;
                    groupBox.Dock = DockStyle.Fill;
                    groupBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    groupBox.AutoSize = true;
                    groupBox.Padding = new Padding(10);

                    TablePanelSub = new TableLayoutPanel();
                    groupBox.Controls.Add(TablePanelSub);
                    TablePanelSub.SuspendLayout();
                    TablePanelSub.ColumnCount = 2;
                    TablePanelSub.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
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

                label.Text = option.Name;
                label.Tag = option;
                label.Dock = DockStyle.Fill;
                label.Padding = new Padding(0, 5, 0, 0);
                Control control = null;
                (int DecimalPlaces, decimal Increment, decimal Maximum, decimal Minimum, decimal Value) numeric = (0, 0, 0, 0, 0);
                if (option.Value is sbyte sbyteVal) numeric = (0, 1, 127, -127, sbyteVal);
                else if (option.Value is short shortVal) numeric = (0, 1, 0x7FFF, -0x8000, shortVal);
                else if (option.Value is int intVal) numeric = (0, 1, 0x7FFFFFFF, -0x80000000, intVal);
                else if (option.Value is long longVal) numeric = (0, 1, 0x7FFFFFFFFFFFFFFF, -0x8000000000000000, longVal);
                else if (option.Value is byte byteVal) numeric = (0, 1, 255, 0, byteVal);
                else if (option.Value is ushort ushortVal) numeric = (0, 1, 0xFFFF, 0, ushortVal);
                else if (option.Value is uint uintVal) numeric = (0, 1, 0xFFFFFFFF, 0, uintVal);
                else if (option.Value is ulong ulongVal) numeric = (0, 1, 0xFFFFFFFFFFFFFFFF, 0, ulongVal);
                else if (option.Value is decimal decimalVal) numeric = (FloatingPointDecimalPlaces, 1, 0xFFFFFFFFFFFFFFFF, -0x8000000000000000, decimalVal);
                else if (option.Value is float floatVal) numeric = (FloatingPointDecimalPlaces, 1, 0xFFFFFFFFFFFFFFFF, -0x8000000000000000, new decimal(floatVal));
                else if (option.Value is double doubleVal) numeric = (FloatingPointDecimalPlaces, 1, 0xFFFFFFFFFFFFFFFF, -0x8000000000000000, new decimal(doubleVal));
                else if (option.Value is Enum enumVal)
                {
                    control = new ComboBox { FormattingEnabled = true };
                    control.Tag = option;
                    foreach (object enumObj in Enum.GetValues(enumVal.GetType())) ((ComboBox)control).Items.Add(enumObj);
                    if (enumVal.GetType().Name == "KnownColor")
                    {
                        ((ComboBox)control).DrawMode = DrawMode.OwnerDrawVariable;
                        ((ComboBox)control).DrawItem += new DrawItemEventHandler(ColorBox_DrawItem);
                    }
                    ((ComboBox)control).SelectedItem = enumVal;
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
                control.ForeColor = base.ForeColor;
                control.BackColor = base.BackColor;
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

        private void Control_MouseHover(object sender, EventArgs e)
        {
            Control control = sender as Control;
            var option = ((object Value, string TreeName, string GroupName, string Name, string Description))control.Tag;
            ToolTip1.SetToolTip(control, option.Description);
        }

        private void Control_Changed(object sender, EventArgs e)
        {
            object newVal = null;
            Control control = sender as Control;
            var option = ((object Value, string TreeName, string GroupName, string Name, string Description))control.Tag;
            if (control is ComboBox comboBox) newVal = comboBox.SelectedItem;
            else if (control is CheckBox checkBox) newVal = checkBox.Checked;
            else if (control is NumericUpDown numericUpDown) newVal = numericUpDown.Value;
            else if (control is TextBox textBox) newVal = textBox.Text;

            if (option.Name.Length > 0 && newVal != null) UpdateSettings(option.Name, newVal);
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
                    TypeConverter innerTypeConverter = TypeDescriptor.GetConverter(innerType);
                    object newValue = innerTypeConverter.ConvertFrom(newVal.ToString());
                    optionInstanceType.GetProperty("Value").SetValue(value, newValue);
                }
                if (!Changed) Changed = true;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message + "\n" + exception.StackTrace, exception.Source, MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        private void OptionLeftView_AfterSelect(object sender, TreeViewEventArgs e) => DisplayPanel(e.Node.Index);

        /// <summary>
        /// Display the appropriate Panel.
        /// </summary>
        private void DisplayPanel(int index)
        {
            if (Panels.Count < 1 || VisiblePanel == Panels[index]) return; // If this is the same Panel, do nothing.
            if (VisiblePanel != null) VisiblePanel.Visible = false; // Hide the previously visible Panel.

            Panels[index].Visible = true; // Display the appropriate Panel.
            VisiblePanel = Panels[index];
        }
        #endregion

        #endregion
    }
}
