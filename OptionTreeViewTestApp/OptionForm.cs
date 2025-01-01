using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OptionTreeViewTestApp
{
    public enum ImageType
    {
        Jpeg,
        Png,
        Tiff,
        Bmp,
        Gif,
    }
    public partial class OptionForm : Form
    {
        public OptionForm()
        {
            InitializeComponent();

            Properties.Settings.Default.Upgrade();

            optionTreeView1.InitSettings(Properties.Settings.Default);
        }
    }
}
