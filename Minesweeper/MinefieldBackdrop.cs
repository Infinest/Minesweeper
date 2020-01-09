using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Minesweeper
{
    class MinefieldBackdrop : UserControl
    {
        #region constants
        public const byte FACE_NORMAL     = 0;
        public const byte FACE_MOUSE_DOWN = 1;
        public const byte FACE_LOST       = 2;
        public const byte FACE_WON        = 3;
        public const byte FACE_HELD_DOWN  = 4;
        public const byte FACE_WAITING    = 5;

        public const byte GAME_NOT_OVER  = 0;
        public const byte GAME_OVER_WON  = 1;
        public const byte GAME_OVER_LOST = 2;
        #endregion

        #region static attributes
        private static readonly int[] facecoords = {
            120,
            96,
            72,
            48,
            24,
            0,
        };
        private static readonly int[] numbercoords = {
            253,
            230,
            207,
            184,
            161,
            138,
            115,
            92,
            69,
            46,
            23,
            0,
        };

        private static readonly Color Backdrop = Color.FromArgb(192, 192, 192);
        private static readonly Pen DarkGrayPen = new Pen(Color.FromArgb(128, 128, 128));
        private static readonly Bitmap NumberfieldText = Minesweeper.Properties.Resources.numbertexture;
        private static readonly Bitmap NumberfieldSuccessText = Minesweeper.Properties.Resources.numbertexturesuccess;
        private static readonly Bitmap FaceText = Minesweeper.Properties.Resources.facetexture;
        private static bool _disabled = false;
        #endregion

        #region non-static attributes
        private int FaceX = 128;
        private byte faceState = 0;
        private bool faceHeldDown = false;
        private byte[] _time = new byte[3];
        private byte[] bombsHidden = new byte[3];
        private byte _gameState = 0;
        #endregion

        #region custom events
        public event EventHandler FaceClicked;
        protected virtual void OnFaceClicked(EventArgs e)
        {
            if (FaceClicked != null)
                FaceClicked(this, e);
        }
        #endregion

        #region getters and setters
        public bool disabled
        {
            get
            {
                return disabled;
            }
            set
            {
                _disabled = value;
                if(!_disabled)
                {
                    this.Refresh();
                }
            }
        }

        private byte getFaceByGameState
        {
            get
            {
                switch (_gameState)
                {
                    case GAME_OVER_LOST:
                        return FACE_LOST;
                    case GAME_OVER_WON:
                        return FACE_WON;
                    default:
                        return FACE_NORMAL;
                }
            }
        }

        public byte gameState
        {
            set
            {
                if (GAME_NOT_OVER <= value && value <= GAME_OVER_LOST)
                {
                    _gameState = value;
                    switch(_gameState)
                    {
                    case GAME_OVER_LOST:
                        face = FACE_LOST;
                        break;
                    case GAME_OVER_WON:
                        face = FACE_WON;
                        break;
                    default:
                        face = FACE_NORMAL;
                        break;
                    }
                }
            }
        }

        public byte face
        {
            set {
                if (FACE_NORMAL <= value && value <= FACE_WAITING)
                {
                    faceState = value;
                    Refresh();
                }
            }
            get {
                return faceState;
            }
        }

        public int BombsHidden
        {
            set {
                bombsHidden = Splitter(value);
                this.Refresh();
            }
        }

        public int Timer
        {
            set {
                _time = Splitter(value);
                this.Refresh();
            }
        }
        #endregion

        #region constructors and overrides
        public MinefieldBackdrop()
        {
            this.DoubleBuffered = true;
        }

        public override void Refresh()
        {
                base.Refresh();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (!_disabled)
            {
                base.OnMouseDown(e);
                if (e.Button == MouseButtons.Left)
                {
                    if (e.X >= FaceX && e.X <= FaceX + 23 && e.Y >= 16 && e.Y <= 39)
                    {
                        faceHeldDown = true;
                        face = FACE_HELD_DOWN;
                    }
                    else if (_gameState == GAME_NOT_OVER)
                    {
                        face = FACE_MOUSE_DOWN;
                        Capture = false;
                    }
                }
            }
        }
        /*
        Unecessary?
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!_disabled)
            {
                base.OnMouseMove(e);
                if (e.X < FaceX || e.X > FaceX + 23 || e.Y < 16 || e.Y > 39)
                {
                    if (faceState != FACE_HELD_DOWN && faceState != FACE_WAITING && !e.Button.HasFlag(MouseButtons.Left))
                    {
                        face = getFaceByGameState;
                    }
                }
                if (e.X >= FaceX && e.X <= FaceX + 23 && e.Y >= 16 && e.Y <= 39 && faceHeldDown)
                {
                    face = FACE_HELD_DOWN;
                }
            }
        }*/
        protected override void OnMouseEnter(EventArgs e)
        {
            if (!_disabled)
            {
                base.OnMouseEnter(e);
                if (!Control.MouseButtons.HasFlag(MouseButtons.Left) && faceHeldDown) faceHeldDown = false;
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (!_disabled)
            {
                base.OnMouseUp(e);
                faceState = FACE_NORMAL;
                if (e.X >= FaceX && e.X <= FaceX + 23 && e.Y >= 16 && e.Y <= 39 && faceHeldDown)
                {
                    OnFaceClicked(new EventArgs());
                }
                faceHeldDown = false;
                Refresh();
            }
        }
        protected override void OnResize(EventArgs e)
        {
            FaceX = ((Width + 16) / 2) - 18;
            base.OnResize(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (System.ComponentModel.LicenseManager.UsageMode != System.ComponentModel.LicenseUsageMode.Designtime)
            {
                e.Graphics.Clear(Backdrop);

                //border white left
                e.Graphics.DrawLine(Pens.White, 0, 0, 0, Height);
                e.Graphics.DrawLine(Pens.White, 1, 0, 1, Height);
                e.Graphics.DrawLine(Pens.White, 2, 0, 2, Height);

                //border white top
                e.Graphics.DrawLine(Pens.White, 0, 0, Width, 0);
                e.Graphics.DrawLine(Pens.White, 0, 1, Width, 1);
                e.Graphics.DrawLine(Pens.White, 0, 2, Width, 2);

                //border white minefield bottom
                e.Graphics.DrawLine(Pens.White, 12, Height - 8, Width - 6, Height - 8);
                e.Graphics.DrawLine(Pens.White, 11, Height - 7, Width - 6, Height - 7);
                e.Graphics.DrawLine(Pens.White, 10, Height - 6, Width - 6, Height - 6);

                //border white minefield right
                e.Graphics.DrawLine(Pens.White, Width - 8, Height - 6, Width - 8, 55);
                e.Graphics.DrawLine(Pens.White, Width - 7, Height - 6, Width - 7, 54);
                e.Graphics.DrawLine(Pens.White, Width - 6, Height - 6, Width - 6, 53);

                //border gray minefield left
                e.Graphics.DrawLine(DarkGrayPen, 9, 52, 9, Height - 7);
                e.Graphics.DrawLine(DarkGrayPen, 10, 52, 10, Height - 8);
                e.Graphics.DrawLine(DarkGrayPen, 11, 52, 11, Height - 9);

                //border gray minefield top
                e.Graphics.DrawLine(DarkGrayPen, 9, 52, Width - 7, 52);
                e.Graphics.DrawLine(DarkGrayPen, 9, 53, Width - 8, 53);
                e.Graphics.DrawLine(DarkGrayPen, 9, 54, Width - 9, 54);

                //border gray top gui
                e.Graphics.DrawLine(DarkGrayPen, 9, 9, Width - 7, 9);
                e.Graphics.DrawLine(DarkGrayPen, 9, 10, Width - 8, 10);

                //border gray left gui
                e.Graphics.DrawLine(DarkGrayPen, 9, 9, 9, 44);
                e.Graphics.DrawLine(DarkGrayPen, 10, 9, 10, 43);

                //border white bottom gui
                e.Graphics.DrawLine(Pens.White, 11, 44, Width - 6, 44);
                e.Graphics.DrawLine(Pens.White, 10, 45, Width - 6, 45);

                //border white right gui
                e.Graphics.DrawLine(Pens.White, Width - 7, 45, Width - 7, 11);
                e.Graphics.DrawLine(Pens.White, Width - 6, 45, Width - 6, 10);

                //border gray top time
                e.Graphics.DrawLine(DarkGrayPen, Width - 55, 15, Width - 16, 15);

                //border gray left time
                e.Graphics.DrawLine(DarkGrayPen, Width - 55, 15, Width - 55, 38);

                //border bottom time
                e.Graphics.DrawLine(Pens.White, Width - 54, 39, Width - 15, 39);

                //border right time
                e.Graphics.DrawLine(Pens.White, Width - 15, 39, Width - 15, 16);

                if (_gameState == GAME_OVER_WON)
                {
                    e.Graphics.DrawImage(NumberfieldSuccessText, new Rectangle(Width - 54, 16, 13, 23), new Rectangle(0, numbercoords[_time[0]], 13, 23), GraphicsUnit.Pixel);
                    e.Graphics.DrawImage(NumberfieldSuccessText, new Rectangle(Width - 41, 16, 13, 23), new Rectangle(0, numbercoords[_time[1]], 13, 23), GraphicsUnit.Pixel);
                    e.Graphics.DrawImage(NumberfieldSuccessText, new Rectangle(Width - 28, 16, 13, 23), new Rectangle(0, numbercoords[_time[2]], 13, 23), GraphicsUnit.Pixel);
                }
                else
                {
                    e.Graphics.DrawImage(NumberfieldText, new Rectangle(Width - 54, 16, 13, 23), new Rectangle(0, numbercoords[_time[0]], 13, 23), GraphicsUnit.Pixel);
                    e.Graphics.DrawImage(NumberfieldText, new Rectangle(Width - 41, 16, 13, 23), new Rectangle(0, numbercoords[_time[1]], 13, 23), GraphicsUnit.Pixel);
                    e.Graphics.DrawImage(NumberfieldText, new Rectangle(Width - 28, 16, 13, 23), new Rectangle(0, numbercoords[_time[2]], 13, 23), GraphicsUnit.Pixel);
                }


                //border gray top hidden
                e.Graphics.DrawLine(DarkGrayPen, 17, 15, 56, 15);

                //border gray left hidden
                e.Graphics.DrawLine(DarkGrayPen, 17, 15, 17, 38);

                //border bottom hidden
                e.Graphics.DrawLine(Pens.White, 18, 39, 57, 39);

                //border right hidden
                e.Graphics.DrawLine(Pens.White, 57, 39, 57, 16);

                if (_gameState == GAME_OVER_WON)
                {
                    e.Graphics.DrawImage(NumberfieldSuccessText, new Rectangle(18, 16, 13, 23), new Rectangle(0, numbercoords[bombsHidden[0]], 13, 23), GraphicsUnit.Pixel);
                    e.Graphics.DrawImage(NumberfieldSuccessText, new Rectangle(31, 16, 13, 23), new Rectangle(0, numbercoords[bombsHidden[1]], 13, 23), GraphicsUnit.Pixel);
                    e.Graphics.DrawImage(NumberfieldSuccessText, new Rectangle(44, 16, 13, 23), new Rectangle(0, numbercoords[bombsHidden[2]], 13, 23), GraphicsUnit.Pixel);
                }
                else
                {
                    e.Graphics.DrawImage(NumberfieldText, new Rectangle(18, 16, 13, 23), new Rectangle(0, numbercoords[bombsHidden[0]], 13, 23), GraphicsUnit.Pixel);
                    e.Graphics.DrawImage(NumberfieldText, new Rectangle(31, 16, 13, 23), new Rectangle(0, numbercoords[bombsHidden[1]], 13, 23), GraphicsUnit.Pixel);
                    e.Graphics.DrawImage(NumberfieldText, new Rectangle(44, 16, 13, 23), new Rectangle(0, numbercoords[bombsHidden[2]], 13, 23), GraphicsUnit.Pixel);
                }

                //border gray top face
                e.Graphics.DrawLine(DarkGrayPen, FaceX - 1, 15, FaceX + 23, 15);

                //border gray left face
                e.Graphics.DrawLine(DarkGrayPen, FaceX - 1, 15, FaceX - 1, 39);

                //border gray bottom face
                e.Graphics.DrawLine(DarkGrayPen, FaceX, 40, FaceX + 24, 40);

                //border gray right face
                e.Graphics.DrawLine(DarkGrayPen, FaceX + 24, 40, FaceX + 24, 16);

                e.Graphics.DrawImage(FaceText, new Rectangle(FaceX, 16, 24, 24), new Rectangle(0, facecoords[faceState], 24, 24), GraphicsUnit.Pixel);
            }
        }
        #endregion

        #region private functions
        /*
            Is used to convert numbers into an array which contains all 3 offsets for the corresponding number texture
        */
        private byte[] Splitter(int input)
        {
            byte[] output = new byte[3];
            if (input < 0)
            {
                output[0] = 11;
                input = -input;
            }
            var digits = input.ToString().Select(t => byte.Parse(t.ToString())).ToArray();
            if (output[0] == 11 && digits.Length>2)
            {
            digits = digits.Skip(digits.Length-2).ToArray();
            }
            else if(digits.Length > 3)
            {
            digits = digits.Skip(digits.Length - 3).ToArray();
            }
            digits.CopyTo(output, 3 - digits.Length);
            return output;
        }
        #endregion
    }
}
