using SQLServerConsul;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SQLServerConsulTestGui
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var serviceList = StoredProcedures.GetCurrentServices(textBox4.Text);
            listBox1.DataSource = serviceList;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //textBox2.Text = StoredProcedures.UpdateConsulServices(textBox3.Text.Replace(Environment.NewLine, ","), textBox4.Text);
        }
    }
}
