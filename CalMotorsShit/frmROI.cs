using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CalMotorsShit
{
    public partial class frmROI : Form
    {
        public frmROI()
        {
            InitializeComponent();
        }
        public frmROI(Bitmap bmp) : this()
        {
            this.pictureBox1.Image = bmp;
        }
    }
}
