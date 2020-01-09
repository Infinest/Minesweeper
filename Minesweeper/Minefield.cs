using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Minesweeper
{
    class Minefield : UserControl
    {
        #region constants
        public const byte FIELD_MAX_WIDTH = 60;
        public const byte FIELD_MAX_HEIGHT = 50;
        #endregion

        #region non-static attributes
        private bool _isImageField = false;
        private bool _disabled = false;
        private Space[] FieldSpaces;
        private int cWidth, cHeight, Bombcount;
        private int marked = 0;
        private int[] numbercoords = new int[8]
        {
            112,
            96,
            80,
            64,
            48,
            32,
            16,
            0,
        };
        private bool mouseDownLeft, mouseDownLeftCanFlip, mouseDownRight, firstClick, chording, debugView = false;
        private bool gameover = true;

        public bool mouseEnterDownEnabled, questionMarkEnabled = false;
        public int highlightedField = -1;
        #endregion

        #region static attributes
        private static readonly Font Minefont = new Font("Verdana", 10, FontStyle.Bold);
        private static readonly Bitmap textureSpaceNumbers = Minesweeper.Properties.Resources.spacetextures.Clone(new Rectangle(0, 0, 16, 16 * 8), PixelFormat.Format32bppArgb);
        private static readonly Bitmap textureField = Minesweeper.Properties.Resources.spacetextures.Clone(new Rectangle(0, 8 * 16, 16, 16), PixelFormat.Format32bppArgb);
        private static readonly Bitmap textureFlag = Minesweeper.Properties.Resources.spacetextures.Clone(new Rectangle(0, 9 * 16, 16, 16), PixelFormat.Format32bppArgb);
        private static readonly Bitmap texturequestionmark = Minesweeper.Properties.Resources.spacetextures.Clone(new Rectangle(0, 10 * 16, 16, 16), PixelFormat.Format32bppArgb);
        private static readonly Bitmap textureMine = Minesweeper.Properties.Resources.spacetextures.Clone(new Rectangle(0, 11 * 16, 16, 16), PixelFormat.Format32bppArgb);
        private static readonly Bitmap textureFlaggedCorrect = Minesweeper.Properties.Resources.spacetextures.Clone(new Rectangle(0, 12 * 16, 16, 16), PixelFormat.Format32bppArgb);
        private static readonly Bitmap textureFlaggedWrong = Minesweeper.Properties.Resources.spacetextures.Clone(new Rectangle(0, 13 * 16, 16, 16), PixelFormat.Format32bppArgb);
        #endregion

        #region custom events
        public delegate void GameOverHandler(object sender, GameOverArgs e);
        public event EventHandler Gamestart;
        public event EventHandler SpaceClick;
        public event EventHandler SpaceRightClick;
        public event GameOverHandler GameOver;
        public event EventHandler StartGenerating;
        public event EventHandler EndGenerating;

        public class GameOverArgs : EventArgs
        {
            public bool won;
            public GameOverArgs(bool _won)
            {
                won = _won;
            }
        }
        protected virtual void OnStartGenerating(EventArgs e)
        {
            if (StartGenerating != null)
                StartGenerating(this, e);
        }
        protected virtual void OnEndGenerating(EventArgs e)
        {
            if (EndGenerating != null)
                EndGenerating(this, e);
        }
        protected virtual void OnGameOver(GameOverArgs e)
        {
            if (GameOver != null)
                GameOver(this, e);
        }
        protected virtual void OnGamestart(EventArgs e)
        {
            if (Gamestart != null)
                Gamestart(this, e);
        }
        protected virtual void OnSpaceClick(EventArgs e)
        {
            if (SpaceClick != null)
                SpaceClick(this, e);
        }
        protected virtual void OnSpaceRightClick(EventArgs e)
        {
            if (SpaceRightClick != null)
                SpaceRightClick(this, e);
        }
        #endregion

        #region getters and setters
        public bool isImageField
        {
            get
            {
                return _isImageField;
            }
            set
            {
                _isImageField = value;
            }
        }
        public bool disabled
        {
            get
            {
                return _disabled;
            }
            set
            {
                _disabled = value;
                this.Refresh();
            }
        }
        public int Hidden
        {
            get { return Bombcount - marked; }
        }
        public bool IsFirstClick
        {
            get { return firstClick; }
        }
        public bool IsGameOver
        {
            get { return gameover; }
        }
        public int BombCount
        {
            get { return Bombcount; }
        }
        public Size FieldSize
        {
            get { return new Size(cWidth, cHeight); }
            set
            {
                if (value.Width <= FIELD_MAX_WIDTH && value.Height <= FIELD_MAX_HEIGHT)
                {
                    cWidth = value.Width;
                    cHeight = value.Height;
                    this.Size = new System.Drawing.Size(cWidth * 16, cHeight * 16);
                    this.Refresh();
                }
            }
        }
        public bool cheatmode
        {
            get { return debugView; }
            set { debugView = value; this.Refresh(); }
        }
        #endregion

        #region constructors and overrides
        public Minefield()
        {
            this.DoubleBuffered = true;
            cWidth = 16;
            cHeight = 16;
            Bombcount = 40;
            FieldSpaces = new Space[cWidth * cHeight];
            this.Size = new System.Drawing.Size(cWidth * 16, cHeight * 16);
        }

        public override void Refresh()
        {
            if (!_disabled)
            {
                base.Refresh();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            this.Size = new System.Drawing.Size(cWidth * 16, cHeight * 16);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (!_disabled)
            {
                base.OnMouseDown(e);
                if (gameover) return;
                int x = -1;
                int y = -1;
                int selectedField = -1;

                if (e.X >= 0 && e.Y >= 0 && e.X < this.Width && e.Y < this.Height)
                {
                    x = (int)Math.Floor(e.X / 16.0);
                    y = (int)Math.Floor(e.Y / 16.0);
                    selectedField = x + y * cWidth;
                }
                else return;

                if (e.Button == MouseButtons.Left)
                {
                    mouseDownLeft = true;
                    if (!mouseDownRight)
                    {
                        if (FieldSpaces[selectedField].state < Space.STATE_HELD_DOWN) FieldSpaces[selectedField].state |= Space.STATE_HELD_DOWN;
                        chording = false;
                        mouseDownLeftCanFlip = true;
                    }
                    else
                    {
                        chording = true;
                    }
                }
                else if (e.Button == MouseButtons.Right)
                {
                    mouseDownRight = true;
                    if (mouseDownLeft)
                    {
                        chording = true;
                        mouseDownLeftCanFlip = false;
                    }
                    else
                    {
                        chording = false;
                        if (x >= 0 && y >= 0)
                        {
                            if (FieldSpaces[selectedField].state == Space.STATE_FLAGGED)
                            {
                                FieldSpaces[selectedField].state = questionMarkEnabled ? Space.STATE_QUESTION_MARK : Space.STATE_HIDDEN;
                                marked--;
                                OnSpaceRightClick(new EventArgs());
                            }
                            else if (FieldSpaces[selectedField].state == Space.STATE_QUESTION_MARK)
                            {
                                FieldSpaces[selectedField].state = Space.STATE_HIDDEN;
                            }
                            else if (FieldSpaces[selectedField].state == Space.STATE_HIDDEN)
                            {
                                FieldSpaces[selectedField].state = Space.STATE_FLAGGED;
                                marked++;
                                OnSpaceRightClick(new EventArgs());
                            }
                        }
                    }
                }

                if (chording)
                {
                    doChordFocus(x, y);
                }

                this.Refresh();
            }
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            if (!_disabled)
            {
                base.OnMouseEnter(e);
                if (Control.MouseButtons.HasFlag(MouseButtons.Left) && mouseEnterDownEnabled)
                {
                    mouseDownLeft = true;
                    mouseDownLeftCanFlip = true;
                    mouseEnterDownEnabled = false;
                }
                else
                {
                    mouseDownLeft = false;
                }
                if (Control.MouseButtons.HasFlag(MouseButtons.Right))
                {
                    mouseDownRight = true;
                }
                else
                {
                    mouseDownRight = false;
                }
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (!_disabled)
            {
                base.OnMouseUp(e);
                int x = -1;
                int y = -1;
                if (e.X >= 0 && e.Y >= 0 && e.X < this.Width && e.Y < this.Height)
                {
                    x = (int)Math.Floor(e.X / 16.0);
                    y = (int)Math.Floor(e.Y / 16.0);
                }
                if (e.Button == MouseButtons.Left)
                {
                    if (mouseDownLeftCanFlip)
                    {
                        if (!mouseDownRight)
                        {
                            if (!gameover) flipSpaces(x, y, true);
                        }
                    }
                    else if (chording)
                    {
                        chording = false;
                        doChord(x, y);
                    }
                    mouseDownLeftCanFlip = false;
                    mouseDownLeft = false;
                }
                else if (e.Button == MouseButtons.Right)
                {
                    mouseDownLeftCanFlip = false;
                    mouseDownRight = false;
                    if (chording)
                    {
                        chording = false;
                        doChord(x, y);
                    }
                }
                this.Refresh();
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!_disabled)
            {
                base.OnMouseMove(e);
                if (e.X >= 0 && e.Y >= 0 && e.X < this.Width && e.Y < this.Height)
                {
                    int x = (int)Math.Floor(e.X / 16.0);
                    int y = (int)Math.Floor(e.Y / 16.0);
                    if (e.Button == MouseButtons.Left && mouseDownLeft && mouseDownLeftCanFlip)
                    {
                        if (!gameover)
                        {
                            if (FieldSpaces[x + y * cWidth].state < Space.STATE_HELD_DOWN) FieldSpaces[x + y * cWidth].state |= Space.STATE_HELD_DOWN;
                        }
                    }
                    else if (e.Button == (MouseButtons.Left ^ MouseButtons.Right))
                    {
                        if (!gameover)
                        {
                            chording = true;
                            doChordFocus(x, y);
                        }
                    }
                }
            }
            this.Refresh();
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.Refresh();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!_disabled && System.ComponentModel.LicenseManager.UsageMode != System.ComponentModel.LicenseUsageMode.Designtime)
            {
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                e.Graphics.Clear(System.Drawing.Color.FromArgb(192, 192, 192));
                Pen line = new Pen(Color.FromArgb(128, 128, 128));
                for (int x = 0; x <= this.Width; x += 16)
                {
                    e.Graphics.DrawLine(line, x, 0, x, this.Height);
                }
                for (int y = 0; y <= this.Height; y += 16)
                {
                    e.Graphics.DrawLine(line, 0, y, this.Width, y);
                }

                if (FieldSpaces != null)
                {
                    for (int i = 0; i < FieldSpaces.Length; i++)
                    {
                        if (FieldSpaces[i] != null)
                        {
                            drawSpace(i, e.Graphics);
                            if (i == highlightedField)
                            {
                                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(175, 255, 0, 255)), new System.Drawing.Rectangle((i % cWidth) * 16 + 1, (int)Math.Floor(i / (double)cWidth) * 16 + 1, 15, 15));
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region private functions
        private void drawToField(Graphics g, Bitmap image, int fieldIndex, Rectangle? sourceRectangle = null)
        {
            if (sourceRectangle.HasValue)
            {
                g.DrawImage(textureSpaceNumbers, new Rectangle((fieldIndex % cWidth) * 16, (int)Math.Floor(fieldIndex / (double)cWidth) * 16, 16, 16), sourceRectangle.Value, GraphicsUnit.Pixel);
            }
            else
            {
                g.DrawImage(image, new Rectangle((fieldIndex % cWidth) * 16, (int)Math.Floor(fieldIndex / (double)cWidth) * 16, 16, 16));
            }
        }

        /*
            helper function for OnPaint override
            Draws one single field
        */
        private void drawSpace(int i, Graphics g)
        {
            if (!FieldSpaces[i].hasState(Space.STATE_HELD_DOWN) && (FieldSpaces[i].hasState(Space.STATE_HIDDEN) || FieldSpaces[i].hasState(Space.STATE_FLAGGED) || FieldSpaces[i].hasState(Space.STATE_QUESTION_MARK)))
            {
                drawToField(g, textureField, i);
            }
            switch (FieldSpaces[i].state & ~Space.STATE_HELD_DOWN & ~Space.STATE_HIDDEN)
            {
                case Space.STATE_FACE_UP:
                    if (FieldSpaces[i].isMine)
                    {
                        drawToField(g, textureMine, i);
                    }
                    else if (FieldSpaces[i].number > 0)
                    {

                        #region oldnumbers
                        //Color Drawcol;
                        //switch (FieldSpaces[i].number)
                        //{
                        //    case 1:
                        //        Drawcol = Color.FromArgb(0, 0, 255);
                        //        break;
                        //    case 2:
                        //        Drawcol = Color.FromArgb(0, 128, 0);
                        //        break;
                        //    case 3:
                        //        Drawcol = Color.FromArgb(255, 0, 0);
                        //        break;
                        //    case 4:
                        //        Drawcol = Color.FromArgb(0, 0, 128);
                        //        break;
                        //    case 5:
                        //        Drawcol = Color.FromArgb(128, 0, 0);
                        //        break;
                        //    case 6:
                        //        Drawcol = Color.FromArgb(0, 128, 128);
                        //        break;
                        //    case 7:
                        //        Drawcol = Color.FromArgb(0, 0, 0);
                        //        break;
                        //    default:
                        //        Drawcol = Color.FromArgb(128, 128, 128);
                        //        break;
                        //}
                        //g.DrawString(FieldSpaces[i].number.ToString(), Minefont, new SolidBrush(Drawcol), new Rectangle((i % cWidth) * 16 + 2, (int)Math.Floor(i / (double)cWidth) * 16, 16, 16));
                        #endregion
                        drawToField(g, textureSpaceNumbers, i, new Rectangle(0, numbercoords[FieldSpaces[i].number - 1], 16, 16));
                    }
                    break;
                case Space.STATE_QUESTION_MARK:
                    drawToField(g, texturequestionmark, i);
                    break;
                case Space.STATE_FLAGGED:
                    drawToField(g, textureFlag, i);
                    break;
                case Space.STATE_BLOWN_UP:
                    g.FillRectangle(System.Drawing.Brushes.Red, new System.Drawing.Rectangle((i % cWidth) * 16 + 1, (int)Math.Floor(i / (double)cWidth) * 16 + 1, 15, 15));
                    drawToField(g, textureMine, i);
                    break;
                case Space.STATE_FACE_UP_FLAGGED_INCORRECT:
                    drawToField(g, textureFlaggedWrong, i);
                    break;
                case Space.STATE_FACE_UP_FLAGGED_CORRECT:
                    drawToField(g, textureFlaggedCorrect, i);
                    break;
                default:
                    if (debugView)
                    {
                        if (FieldSpaces[i].isMine) g.FillRectangle(System.Drawing.Brushes.Red, new System.Drawing.Rectangle((i % cWidth) * 16, (int)Math.Floor(i / (double)cWidth) * 16, 5, 5));
                        else
                            g.DrawString(FieldSpaces[i].number.ToString(), Minefont, Brushes.Black, new Rectangle((i % cWidth) * 16 + 1, (int)Math.Floor(i / (double)cWidth) * 16, 16, 16));
                    }
                    break;
            }
            if ((FieldSpaces[i].state & Space.STATE_HELD_DOWN) == Space.STATE_HELD_DOWN)
            {
                FieldSpaces[i].state = (byte)(FieldSpaces[i].state & ~Space.STATE_HELD_DOWN);
            }
            //g.DrawString(mouseDownLeft.ToString() + " " + mouseDownRight + " " + chording, Minefont, Brushes.Magenta, 5, 5);
        }

        /*
            Outputs the number of bombs in direct vicinity of the given field
        */
        private int GenerateNumber(int x, int y)
        {
            int output = 0;
            for (int fieldToRegenerateX = x - 1; fieldToRegenerateX <= x + 1; fieldToRegenerateX++)
            {
                if (0 <= fieldToRegenerateX && fieldToRegenerateX < cWidth)
                {
                    for (int fieldToRegenerateY = y - 1; fieldToRegenerateY <= y + 1; fieldToRegenerateY++)
                    {
                        if (0 <= fieldToRegenerateY && fieldToRegenerateY < cHeight)
                        {
                            //Don't generate new number for field given via parameters or fields with bomb on it
                            if (!(fieldToRegenerateX == x && fieldToRegenerateY == y) && FieldSpaces[fieldToRegenerateX + fieldToRegenerateY * cWidth].isMine)
                            {
                                output++;
                            }
                        }
                    }
                }
            }
            return output;
        }

        private int GenerateNumber(int x, int y, Space[] space)
        {
            int output = 0;
            for (int fieldToRegenerateX = x - 1; fieldToRegenerateX <= x + 1; fieldToRegenerateX++)
            {
                if (0 <= fieldToRegenerateX && fieldToRegenerateX < cWidth)
                {
                    for (int fieldToRegenerateY = y - 1; fieldToRegenerateY <= y + 1; fieldToRegenerateY++)
                    {
                        if (0 <= fieldToRegenerateY && fieldToRegenerateY < cHeight)
                        {
                            //Don't generate new number for field given via parameters or fields with bomb on it
                            if (!(fieldToRegenerateX == x && fieldToRegenerateY == y) && space[fieldToRegenerateX + fieldToRegenerateY * cWidth] != null && space[fieldToRegenerateX + fieldToRegenerateY * cWidth].isMine)
                            {
                                output++;
                            }
                        }
                    }
                }
            }
            return output;
        }

        /*
            Recalculates the numbers for all fields in direct vicinity to the one given via the parameters
        */
        private void ReGenerateNextTo(int x, int y)
        {
            for (int fieldToRegenerateX = x - 1; fieldToRegenerateX <= x + 1; fieldToRegenerateX++)
            {
                if (0 <= fieldToRegenerateX && fieldToRegenerateX < cWidth)
                {
                    for (int fieldToRegenerateY = y - 1; fieldToRegenerateY <= y + 1; fieldToRegenerateY++)
                    {
                        if (0 <= fieldToRegenerateY && fieldToRegenerateY < cHeight)
                        {
                            //Don't generate new number for field given via parameters or fields with bomb on it
                            if (!(fieldToRegenerateX == x && fieldToRegenerateY == y) && !FieldSpaces[fieldToRegenerateX + fieldToRegenerateY * cWidth].isMine)
                            {
                                FieldSpaces[fieldToRegenerateX + fieldToRegenerateY * cWidth].number = GenerateNumber(fieldToRegenerateX, fieldToRegenerateY);
                            }
                        }
                    }
                }
            }
        }

        /*
            checks wether or not the current field has been cleared successfully
        */
        private bool CheckWin()
        {
            for (int i = 0; i < FieldSpaces.Length; i++)
            {
                if (!FieldSpaces[i].isMine && FieldSpaces[i].state != Space.STATE_FACE_UP) return false;
            }
            return true;
        }

        /*
            Flips the space given via parameters.
            If space has 0 as number, flips all adjacent fields recursively.
            If all numbered fields have been flipped call RevealBoard and end game.

            If player flips first field and field is a bomb, the bomb will be moved to the first free field. This is to prevent the player from dying on the first click
        */
        private void flipSpaces(int x, int y, bool triggerEvent = false)
        {
            if (x >= 0 && y >= 0 && FieldSpaces[x + y * cWidth].state < Space.STATE_FLAGGED)
            {
                if (firstClick && !_isImageField && Properties.Settings.Default.alwaysSolvableEnabled)
                {
                    disabled = true;
                    OnStartGenerating(new EventArgs());
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        GenerateField(BombCount, new Point(x, y));
                        this.MainThreadInvoke(() =>
                        {
                            OnEndGenerating(new EventArgs());
                            firstClick = false;
                            flipSpaces(x, y, true);
                            disabled = false;
                        });
                    });
                    return;
                }

                if (triggerEvent)
                {
                    OnSpaceClick(new EventArgs());
                }

                List<Point> toclear = new List<Point>();
                if (!FieldSpaces[x + y * cWidth].isMine)
                {
                    FieldSpaces[x + y * cWidth].state = Space.STATE_FACE_UP;
                    if (FieldSpaces[x + y * cWidth].number == 0)
                    {

                        for (int adjacentX = x - 1; adjacentX <= x + 1; adjacentX++)
                        {
                            if (0 <= adjacentX && adjacentX < cWidth)
                            {
                                for (int adjacentY = y - 1; adjacentY <= y + 1; adjacentY++)
                                {
                                    if (0 <= adjacentY && adjacentY < cHeight)
                                    {
                                        if (FieldSpaces[adjacentX + adjacentY * cWidth].state < Space.STATE_FACE_UP)
                                        {
                                            flipSpaces(adjacentX, adjacentY);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (CheckWin())
                    {
                        RevealBoard(true);
                    }
                }
                else
                {
                    if (firstClick)
                    {
                        for (int yy = 0; yy < cHeight; yy++)
                        {
                            for (int xx = 0; xx < cWidth; xx++)
                            {
                                if (!FieldSpaces[xx + yy * cWidth].isMine)
                                {
                                    FieldSpaces[x + y * cWidth].isMine = false;
                                    FieldSpaces[x + y * cWidth].number = GenerateNumber(x, y);
                                    FieldSpaces[xx + yy * cWidth].number = 0;
                                    FieldSpaces[xx + yy * cWidth].isMine = true;
                                    ReGenerateNextTo(x, y);
                                    ReGenerateNextTo(xx, yy);
                                    flipSpaces(x, y);
                                    return;
                                }
                            }
                        }
                    }
                    FieldSpaces[x + y * cWidth].state = Space.STATE_BLOWN_UP;
                    RevealBoard(false);
                }
                if (firstClick) firstClick = false;
            }
        }

        /*
            Sets state STATE_HELD_DOWN for field given via parameters and adjacent fields
        */
        private void doChordFocus(int x, int y)
        {
            for (int chordSpaceX = x - 1; chordSpaceX <= x + 1; chordSpaceX++)
            {
                if (chordSpaceX < 0 || cWidth - 1 < chordSpaceX)
                {
                    continue;
                }

                for (int chordSpaceY = y - 1; chordSpaceY <= y + 1; chordSpaceY++)
                {
                    if (chordSpaceY < 0 || cHeight - 1 < chordSpaceY)
                    {
                        continue;
                    }
                    if (FieldSpaces[chordSpaceX + chordSpaceY * cWidth].state < Space.STATE_FACE_UP)
                    {
                        FieldSpaces[chordSpaceX + chordSpaceY * cWidth].state |= Space.STATE_HELD_DOWN;
                    }
                }
            }
        }

        /*
            Performs a chord:
            Check if the number of flags in direct vicinity of the field given via parameters matches the fields number.
            If they match, flip all non-flagged fields in direct vicinity
        */
        private void doChord(int x, int y)
        {
            OnSpaceClick(new EventArgs());
            if (0 <= x && 0 <= y && FieldSpaces[x + y * cWidth].state == Space.STATE_FACE_UP && 0 < FieldSpaces[x + y * cWidth].number)
            {
                int flagCounter = 0;
                for (int chordSpaceX = x - 1; chordSpaceX <= x + 1; chordSpaceX++)
                {
                    if (0 <= chordSpaceX && chordSpaceX < cWidth)
                    {
                        for (int chordSpaceY = y - 1; chordSpaceY <= y + 1; chordSpaceY++)
                        {
                            if (0 <= chordSpaceY && chordSpaceY < cHeight)
                            {
                                if (FieldSpaces[chordSpaceX + chordSpaceY * cWidth].state == Space.STATE_FLAGGED)
                                {
                                    flagCounter++;
                                }
                            }
                        }
                    }
                }

                if (FieldSpaces[x + y * cWidth].number == flagCounter)
                {
                    for (int chordSpaceX = x - 1; chordSpaceX <= x + 1; chordSpaceX++)
                    {
                        if (0 <= chordSpaceX && chordSpaceX < cWidth)
                        {
                            for (int chordSpaceY = y - 1; chordSpaceY <= y + 1; chordSpaceY++)
                            {
                                if (0 <= chordSpaceY && chordSpaceY < cHeight)
                                {
                                    if (!gameover)
                                    {
                                        flipSpaces(chordSpaceX, chordSpaceY);
                                    }
                                    else return;
                                }
                            }
                        }
                    }
                }
            }
        }

        private Space[] findValidField(CancellationToken? token, Point? solverOrigin = null)
        {
            Random rnd = new Random(new Random().Next(999999999));
            MinefieldSolver solver = new MinefieldSolver();
            Space[] fieldToCalculate = new Space[cWidth * cHeight];
            do
            {
                fieldToCalculate = new Space[cWidth * cHeight];

                for (int i = 0; i < Bombcount; i++)
                {
                    int x = rnd.Next(0, cWidth);
                    int y = rnd.Next(0, cHeight);
                    if (fieldToCalculate[x + y * cWidth] == null)
                    {
                        fieldToCalculate[x + y * cWidth] = new Space(true, 0);
                    }
                    else
                    {
                        i--;
                    }
                }

                for (int x = 0; x < cWidth; x++)
                {
                    for (int y = 0; y < cHeight; y++)
                    {
                        if (fieldToCalculate[y * cWidth + x] == null)
                        {
                            fieldToCalculate[y * cWidth + x] = new Space(false, GenerateNumber(x, y, fieldToCalculate));
                        }
                    }
                }
            } while (
            solverOrigin.HasValue && !(token.HasValue && token.Value.IsCancellationRequested) && !(
            fieldToCalculate[solverOrigin.Value.X + solverOrigin.Value.Y * cWidth].number == 0 &&
            !fieldToCalculate[solverOrigin.Value.X + solverOrigin.Value.Y * cWidth].isMine &&
            solver.tryToSolve(ref fieldToCalculate, FieldSize, solverOrigin.Value)
            ));
            return fieldToCalculate;
        }
        #endregion

        #region public functions
        /*
            generates a field from an input bitmap
        */
        public void GenerateFieldfromInput(Bitmap input)
        {
            if (FIELD_MAX_WIDTH < input.Width || FIELD_MAX_HEIGHT < input.Height)
            {
                throw new ArgumentException(String.Format("Images may only be {0}px by {1}px at max", FIELD_MAX_WIDTH, FIELD_MAX_HEIGHT));
            }

            FieldSize = new Size(input.Width, input.Height);
            FieldSpaces = new Space[cWidth * cHeight];
            marked = 0;

            for (int x = 0; x < cWidth; x++)
            {
                for (int y = 0; y < cHeight; y++)
                {
                    FieldSpaces[x + y * cWidth] = new Space(false, 0);
                }
            }

            input = input.Clone(new Rectangle(0, 0, input.Width, input.Height), PixelFormat.Format32bppRgb);
            BitmapData bData = input.LockBits(new Rectangle(0, 0, input.Width, input.Height), ImageLockMode.ReadWrite, input.PixelFormat);
            int bitsPerPixel = Image.GetPixelFormatSize(input.PixelFormat);
            int size = bData.Stride * input.Height;
            byte[] data = new byte[size];
            System.Runtime.InteropServices.Marshal.Copy(bData.Scan0, data, 0, size);
            int b = 0;
            for (int y = 0; y < input.Height; y++)
            {
                for (int x = 0; x < input.Width; x++)
                {
                    if (!(data[y * bData.Stride + x * (bitsPerPixel / 8) + 2] == 255 && data[y * bData.Stride + x * (bitsPerPixel / 8) + 1] == 255 && data[y * bData.Stride + x * (bitsPerPixel / 8)] == 255))
                    {
                        FieldSpaces[x + y * cWidth] = new Space(true, 0);
                        b++;
                    }
                }
            }

            input.UnlockBits(bData);
            input.Dispose();
            Bombcount = b;
            for (int x = 0; x < cWidth; x++)
            {
                for (int y = 0; y < cHeight; y++)
                {
                    if (!FieldSpaces[(y == 0) ? x : x + y * cWidth].isMine)
                    {
                        FieldSpaces[(y == 0) ? x : x + y * cWidth] = new Space(false, GenerateNumber(x, y));
                    }
                }
            }
            _isImageField = true;
            gameover = false;
            firstClick = true;
            this.Refresh();
            OnGamestart(new EventArgs());
        }

        /*
            generates a new random field with the given amount of bombs
        */
        public void GenerateField(int bombs, Point? solverOrigin = null)
        {
            Bombcount = bombs;
            marked = 0;
            FieldSpaces = null;

            if (solverOrigin.HasValue)
            {
                Task[] tasks = new Task[50];
                CancellationTokenSource ct = new CancellationTokenSource();
                ct.CancelAfter(5000);
                for (int i = 0; i < 50; i++)
                {
                    tasks[i] = Task.Factory.StartNew(() =>
                    {
                        Space[] temp = findValidField(ct.Token, solverOrigin);
                        if (FieldSpaces == null)
                        {
                            FieldSpaces = temp;
                        }
                    }, ct.Token);
                }
                Task.WaitAny(tasks);
                if (ct.IsCancellationRequested)
                {
                    System.Threading.Thread.Sleep(50);
                    MessageBox.Show("Couldn't find solvable field in under 5 Seconds." + Environment.NewLine + "The generated board will require guessing at some point.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                else
                {
                    ct.Cancel();
                }
                for (int i = 0; i < FieldSpaces.Length; i++)
                {
                    FieldSpaces[i].state = Space.STATE_HIDDEN;
                }
            }
            else
            {
                FieldSpaces = findValidField(null);
            }

            _isImageField = false;
            gameover = false;
            firstClick = true;
            this.MainThreadInvoke(() =>
            {
                OnGamestart(new EventArgs());
            });
        }

        /*
            Ends the game and reveals fields in a manner dependant on wether game was won or not

            won: bombs will get STATE_FACE_UP_FLAGGED_CORRECT
            lost: bombs will simply get STATE_FACE_UP

            incorrectly flagged bombs will always get STATE_FACE_UP_FLAGGED_INCORRECT
        */
        public void RevealBoard(bool won)
        {
            gameover = true;
            OnGameOver(new GameOverArgs(won));
            for (int yy = 0; yy < cHeight; yy++)
            {
                for (int xx = 0; xx < cWidth; xx++)
                {
                    if ((FieldSpaces[xx + yy * cWidth].state == Space.STATE_FLAGGED || won) && FieldSpaces[xx + yy * cWidth].isMine)
                    {
                        FieldSpaces[xx + yy * cWidth].state = Space.STATE_FACE_UP_FLAGGED_CORRECT;
                    }
                    else if (FieldSpaces[xx + yy * cWidth].state == Space.STATE_FLAGGED && !FieldSpaces[xx + yy * cWidth].isMine)
                    {
                        FieldSpaces[xx + yy * cWidth].state = Space.STATE_FACE_UP_FLAGGED_INCORRECT;
                    }
                    else if (FieldSpaces[xx + yy * cWidth].state != Space.STATE_BLOWN_UP && FieldSpaces[xx + yy * cWidth].isMine)
                    {
                        FieldSpaces[xx + yy * cWidth].state = Space.STATE_FACE_UP;
                    }
                }
            }
            this.Refresh();
        }

        /*
            reveals all numbered fields
        */
        public void RevealNumbers()
        {
            for (int i = 0; i < FieldSpaces.Length; i++)
            {
                if (FieldSpaces[i].number > 0 && !FieldSpaces[i].isMine) FieldSpaces[i].state = Space.STATE_FACE_UP;
            }
            Refresh();
        }

        /*
            flags all fields that have a bomb on them
        */
        public void FlagAllBombs()
        {
            if (!gameover && Bombcount > 0)
            {
                for (int i = 0; i < FieldSpaces.Length; i++)
                {
                    if (FieldSpaces[i].isMine && FieldSpaces[i].state == Space.STATE_HIDDEN)
                    {
                        FieldSpaces[i].state = Space.STATE_FLAGGED;
                        marked++;
                    }
                }
                Refresh();
            }
        }

        public void runVisualSolver()
        {
            OnStartGenerating(new EventArgs());
            MinefieldSolver mfs = new MinefieldSolver();
            this.disabled = true;
            Task.Factory.StartNew(() =>
            {
                mfs.tryToSolve(ref FieldSpaces, FieldSize, Point.Empty, this);
                this.MainThreadInvoke(() =>
                {
                    highlightedField = -1;
                    disabled = false;
                    OnEndGenerating(new EventArgs());
                    if (CheckWin())
                    {
                        RevealBoard(true);
                    }
                });
            });
        }
        #endregion
    }
}
