using SQLServerConsul;
using System;
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
            string info="";
            StoredProcedures.UpdateConsulServices(activeDatabases: textBox3.Text.Replace(Environment.NewLine, ","), suffix: textBox4.Text, ttlSeconds: Int32.Parse(textBox5.Text), useFqdn: checkBox1.Checked);
            textBox2.Text = info;
        }
    }
}
