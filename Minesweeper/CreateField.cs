using System;
using System.Windows.Forms;

namespace Minesweeper
{
    public partial class CreateField : Form
    {
        #region non-static attributes
        public int width = 0;
        public int height = 0;
        public int bombs = 0;
        #endregion

        #region constructors and overrides
        public CreateField(int a, int b,int c)
        {
            InitializeComponent();
            numericUpDown1.Value = a;
            numericUpDown2.Value = b;
            numericUpDown3.Value = c;
        }
        #endregion

        #region events
        private void submitValues(object sender, EventArgs e)
        {
            width = (int)numericUpDown1.Value;
            height = (int)numericUpDown2.Value;
            bombs = (int)numericUpDown3.Value;
            //MessageBox.Show((((float)bombs) / (width * height)).ToString());
            if (((float)bombs / (width * height))> 0.7)
            {
                MessageBox.Show(String.Format("Bombs aren't allowed to make{0}up more than 70% of the field.{0}The maximum for your the currently set width and height is {1}", Environment.NewLine, ((int)((height* width)*0.7f)).ToString()),"Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
                return;
            }
            this.DialogResult = DialogResult.OK;
        }

        private void redirectEnterKeyPress(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = e.SuppressKeyPress = true;
                submitValues(sender, new EventArgs());
            }
        }
        #endregion
    }
}
