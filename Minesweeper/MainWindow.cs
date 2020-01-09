using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Minesweeper
{
    public partial class MainWindow : Form
    {
        #region non-static attributes
        private DateTime GameStartedAt;
        private int delta = 0;
        private bool newgame = false;
        private string file = "";
        private BackgroundWorker fieldGeneratorWorker = new BackgroundWorker();
        #endregion

        #region getters and setters
        private int TimeChange
        {
            get { return delta; }
            set { delta = value; try { this.Invoke((MethodInvoker)delegate { minefieldBackDropInstance.Timer = value; }); } catch { } }
        }
        #endregion

        #region constructors and overrides
        public MainWindow()
        {
            InitializeComponent();
        }
        #endregion

        #region events
        private void Custom_Click(object sender, EventArgs e)
        {
            CreateField GetInput = new CreateField(minefieldInstance.FieldSize.Width,minefieldInstance.FieldSize.Height,minefieldInstance.BombCount);
            if (GetInput.ShowDialog() == DialogResult.OK)
            {
                minefieldBackDropInstance.gameState = MinefieldBackdrop.GAME_NOT_OVER;
                BeginnerItem.Checked = false;
                IntermediateItem.Checked = false;
                ExpertItem.Checked = false;
                CustomItem.Checked = true;
                LoadFromItem.Checked = false;
                generateNewField(GetInput.bombs, new Size(GetInput.width, GetInput.height));
            }
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            minefieldInstance.Parent = this;
            minefieldBackDropInstance.Parent = this;
            minefieldInstance.questionMarkEnabled = Properties.Settings.Default.questionMarkEnabled;
            MarksItem.Checked = minefieldInstance.questionMarkEnabled;
            Intermediate_Click(null, null);
            System.Threading.ThreadPool.QueueUserWorkItem(mouseLeftUpChecker);
        }

        private void New_Click(object sender, EventArgs e)
        {
            minefieldBackDropInstance.gameState = MinefieldBackdrop.GAME_NOT_OVER;
            if (!LoadFromItem.Checked)
            {
                generateNewField(minefieldInstance.BombCount, minefieldInstance.FieldSize);
            }
            else if (System.IO.File.Exists(file))
            {
                minefieldBackDropInstance.Refresh();
                minefieldInstance.GenerateFieldfromInput(new Bitmap(file));
                this.Width = minefieldInstance.FieldSize.Width * 16 + 36;
                this.Height = minefieldInstance.FieldSize.Height * 16 + 121;
                minefieldInstance.Refresh();
                minefieldBackDropInstance.Refresh();
            }
        }

        private void Beginner_Click(object sender, EventArgs e)
        {
            minefieldBackDropInstance.gameState = MinefieldBackdrop.GAME_NOT_OVER;
            BeginnerItem.Checked = true;
            IntermediateItem.Checked = false;
            ExpertItem.Checked = false;
            CustomItem.Checked = false;
            LoadFromItem.Checked = false;
            generateNewField(10, new Size(9, 9));
        }

        private void Intermediate_Click(object sender, EventArgs e)
        {
            minefieldBackDropInstance.gameState = MinefieldBackdrop.GAME_NOT_OVER;
            BeginnerItem.Checked = false;
            IntermediateItem.Checked = true;
            ExpertItem.Checked = false;
            CustomItem.Checked = false;
            LoadFromItem.Checked = false;
            generateNewField(40, new Size(16, 16));
        }

        private void Expert_Click(object sender, EventArgs e)
        {
            minefieldBackDropInstance.gameState = MinefieldBackdrop.GAME_NOT_OVER;
            BeginnerItem.Checked = false;
            IntermediateItem.Checked = false;
            ExpertItem.Checked = true;
            CustomItem.Checked = false;
            LoadFromItem.Checked = false;
            generateNewField(99, new Size(30, 16));
        }

        private void Debug_Click(object sender, EventArgs e)
        {
            DebugItem.Checked = !DebugItem.Checked;
            minefieldInstance.cheatmode = !minefieldInstance.cheatmode;
        }

        private void minefieldInstance_Gamestart(object sender, EventArgs e)
        {
            minefieldBackDropInstance.BombsHidden = minefieldInstance.Hidden;
            TimeChange = 0;
            newgame = true;
        }
        private void minefieldInstance_SpaceClick(object sender, EventArgs e)
        {
            if (newgame)
            {
                newgame = false;
                GameStartedAt = DateTime.Now - TimeSpan.FromSeconds(1);
                System.Threading.ThreadPool.QueueUserWorkItem(CountSeconds);
            }
        }
        private void minefieldInstance_SpaceRightClick(object sender, EventArgs e)
        {
            minefieldBackDropInstance.BombsHidden = minefieldInstance.Hidden;
        }

        private void minefieldInstance_MouseDown(object sender, MouseEventArgs e)
        {
            if (!minefieldInstance.IsGameOver && e.Button == MouseButtons.Left)
                minefieldBackDropInstance.face = MinefieldBackdrop.FACE_MOUSE_DOWN;
        }

        private void minefieldInstance_OnStartGenerating(object sender, EventArgs e)
        {
            minefieldBackDropInstance.face = MinefieldBackdrop.FACE_WAITING;
            minefieldBackDropInstance.disabled = minefieldInstance.disabled = true;
            foreach(MenuItem mi in mainMenu1.MenuItems)
            {
                mi.Enabled = false;
            }
        }

        private void minefieldInstance_OnEndGenerating(object sender, EventArgs e)
        {
            minefieldBackDropInstance.face = MinefieldBackdrop.FACE_NORMAL;
            minefieldBackDropInstance.disabled = minefieldInstance.disabled = false;
            foreach (MenuItem mi in mainMenu1.MenuItems)
            {
                mi.Enabled = true;
            }
        }

        private void MainWindow_MouseDown(object sender, MouseEventArgs e)
        {
            minefieldInstance.mouseEnterDownEnabled = true;
        }

        private void minefieldBackdrop1_MouseDown(object sender, MouseEventArgs e)
        {
            minefieldInstance.mouseEnterDownEnabled = true;
        }

        private void minefieldInstance_GameOver(object sender, Minefield.GameOverArgs e)
        {
            if (e.won)
            {
                minefieldBackDropInstance.gameState = MinefieldBackdrop.GAME_OVER_WON;
            }
            else
            {
                minefieldBackDropInstance.gameState = MinefieldBackdrop.GAME_OVER_LOST;
            }
        }

        private void minefieldInstance_MouseUp(object sender, MouseEventArgs e)
        {
            if (minefieldBackDropInstance.face == MinefieldBackdrop.FACE_MOUSE_DOWN)
            {
                minefieldBackDropInstance.face = MinefieldBackdrop.FACE_NORMAL;
            }
        }

        private void LoadFrom_Click(object sender, EventArgs e)
        {
            OpenFileDialog k = new OpenFileDialog();
            k.FileName = @"C:\";
            if(k.ShowDialog() == DialogResult.OK && System.IO.File.Exists(k.FileName))
            {
                file = k.FileName;
                try
                {
                    minefieldInstance.GenerateFieldfromInput(new Bitmap(k.FileName));
                    BeginnerItem.Checked = false;
                    IntermediateItem.Checked = false;
                    ExpertItem.Checked = false;
                    CustomItem.Checked = false;
                    LoadFromItem.Checked = true;
                    minefieldBackDropInstance.gameState = MinefieldBackdrop.GAME_NOT_OVER;
                    this.Width = minefieldInstance.FieldSize.Width * 16 + 36;
                    this.Height = minefieldInstance.FieldSize.Height * 16 + 101 + 20;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                minefieldInstance.Refresh();
                minefieldBackDropInstance.Refresh();
            }
        }

        private void FlagBombs_Click(object sender, EventArgs e)
        {
            minefieldInstance.FlagAllBombs();
            minefieldBackDropInstance.BombsHidden = minefieldInstance.Hidden;
        }

        private void RevealItem_Click(object sender, EventArgs e)
        {
            minefieldInstance.RevealNumbers();
        }

        private void Win_Click(object sender, EventArgs e)
        {
            minefieldInstance.RevealBoard(true);
        }

        private void MarksItem_Click(object sender, EventArgs e)
        {
            MarksItem.Checked = !MarksItem.Checked;
            minefieldInstance.questionMarkEnabled = MarksItem.Checked;
            Properties.Settings.Default.questionMarkEnabled = MarksItem.Checked;
            Properties.Settings.Default.Save();
        }

        private void SolvableItem_Click(object sender, EventArgs e)
        {
            SolvableItem.Checked = !SolvableItem.Checked;
            Properties.Settings.Default.alwaysSolvableEnabled = SolvableItem.Checked;
            Properties.Settings.Default.Save();
        }

        private void AboutItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Minesweeper application trying to partially mimic Microsoft's WinXP version." + Environment.NewLine + "Created by Infinest @ www.infine.st" + Environment.NewLine + Environment.NewLine + "Try chording (left + right mouse button). Google it for further info.", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SolveAction(object sender, EventArgs e)
        {
            minefieldInstance.runVisualSolver();
        }
        #endregion

        #region separate threads
        /*
            Constantly checks wether the left mouse has been released
            if so, set back face in minefieldBackdrop back to normal and make sure the minefield won't register the left mouse as down on enter event
        */
        private void mouseLeftUpChecker(object threadContext)
        {
            while(true)
            {
                if (!Control.MouseButtons.HasFlag(MouseButtons.Left))
                {
                    minefieldInstance.mouseEnterDownEnabled = false;
                    if (minefieldBackDropInstance.face == MinefieldBackdrop.FACE_MOUSE_DOWN)
                    {
                        minefieldBackDropInstance.MainThreadInvoke(() => {
                            minefieldBackDropInstance.face = MinefieldBackdrop.FACE_NORMAL;
                        });
                    }
                }
                System.Threading.Thread.Sleep(50);
            }
         }

        /*
            Counts up the game timer displayed in minefieldBackdrop
        */
        private void CountSeconds(object threadContext)
        {
            while (!minefieldInstance.IsGameOver && !newgame)
            {
                if (TimeChange < 0) return;
                else if (TimeChange > 999) { TimeChange = 999; return; }
                TimeChange = (int)(DateTime.Now - GameStartedAt).TotalSeconds;
                System.Threading.Thread.Sleep(500);
            }
        }
        #endregion

        #region private functions
        public void generateNewField(int bombCount, Size newSize)
        {
            minefieldBackDropInstance.face = MinefieldBackdrop.FACE_WAITING;
            minefieldBackDropInstance.disabled = minefieldInstance.disabled = true;
            minefieldInstance.FieldSize = newSize;
            minefieldInstance.GenerateField(bombCount);
            this.Width = minefieldInstance.FieldSize.Width * 16 + 36;
            this.Height = minefieldInstance.FieldSize.Height * 16 + 121;
            minefieldBackDropInstance.face = MinefieldBackdrop.FACE_NORMAL;
            minefieldBackDropInstance.disabled = minefieldInstance.disabled = false;
        }
        #endregion
    }
}
