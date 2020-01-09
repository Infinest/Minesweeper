using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minesweeper
{
    class Space
    {
        #region constants
        public const byte STATE_HIDDEN = 1;
        public const byte STATE_QUESTION_MARK = 2;
        public const byte STATE_HELD_DOWN = 4;
        public const byte STATE_FACE_UP = 8;
        public const byte STATE_FLAGGED = 16;
        public const byte STATE_BLOWN_UP = 32;
        public const byte STATE_FACE_UP_FLAGGED_INCORRECT = 64;
        public const byte STATE_FACE_UP_FLAGGED_CORRECT = 128;
        #endregion

        #region non-static attributes
        public bool isMine = false;
        public int number = 0;
        public byte state = STATE_HIDDEN;
        #endregion

        #region constructors and overrides
        public Space(bool _Bomb, int _number)
        {
            isMine = _Bomb;
            number = _number;
        }
        #endregion

        #region public functions
        public bool hasState(byte state)
        {
            if ((this.state & state) == state)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion
    }
}
