using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Minesweeper
{
    class MinefieldSolver
    {
        private LinkedList<int> flippedNumbers;
        private LinkedList<int> fullyFlaggedNumbers;
        private Size fieldSize;
        private Space[] field;
        private Point solverOrigin;
        private Minefield minefield;

        public bool tryToSolve(ref Space[] _field, Size _fieldSize, Point _solverOrigin, Minefield _minefield = null)
        {
            flippedNumbers = new LinkedList<int>();
            fullyFlaggedNumbers = new LinkedList<int>();
            field = _field;
            fieldSize = _fieldSize;
            solverOrigin = _solverOrigin;
            minefield = _minefield;

            //flipFirstUnflippedZero();

            if (minefield == null)
            {
                flipField(_solverOrigin.X + _solverOrigin.Y * fieldSize.Width);
            }
            else
            {
                for(int i = 0; i < field.Length; i++)
                {
                    if(0 < field[i].number && field[i].state == Space.STATE_FACE_UP)
                    {
                        flippedNumbers.AddFirst(i);
                    }
                }
            }
            solveViaFlippedNumbers();
            
            return CheckWin();
        }

        private void flipFirstUnflippedZero()
        {
            for (int i = 0; i < field.Length; i++)
            {
                if (field[i].number == 0 && !field[i].isMine && field[i].state == Space.STATE_HIDDEN)
                {
                    flipField(i);
                    return;
                }
            }
        }
        
        private void flipField(int index)
        {
            field[index].state = Space.STATE_FACE_UP;
            if (0 < field[index].number) flippedNumbers.AddFirst(index);
            int x = (index % fieldSize.Width);
            int y = (int)Math.Floor(index / (double)fieldSize.Width);

            if (field[index].number == 0)
            {
                for (int adjacentX = ((0 <= x - 1) ? (x - 1) : x); adjacentX <= ((x + 1 < fieldSize.Width) ? (x + 1) : x); adjacentX++)
                {
                    for (int adjacentY = ((0 <= y - 1) ? (y - 1) : y); adjacentY <= ((y + 1 < fieldSize.Height) ? (y + 1) : y); adjacentY++)
                    {
                        if (field[adjacentX + adjacentY * fieldSize.Width].state != Space.STATE_FACE_UP)
                        {
                            flipField(adjacentX + adjacentY * fieldSize.Width);
                        }
                    }
                }
            }

            /*
            walkAdjacentFields(index, (int currentIndex, int adjacentX, int adjacentY) => {
                if (field[index].number == 0 && field[adjacentX + adjacentY * fieldSize.Width].state != Space.STATE_FACE_UP)
                {
                    flipField(adjacentX + adjacentY * fieldSize.Width);
                }
            });*/

        }

        private void walkAdjacentFields(int index,Action<int, int, int> callbackFunction)
        {
            int x = (index % fieldSize.Width);
            int y = (int)Math.Floor(index / (double)fieldSize.Width);
            for (int adjacentX = x - 1; adjacentX <= x + 1; adjacentX++)
            {
                if (0 <= adjacentX && adjacentX < fieldSize.Width)
                {
                    for (int adjacentY = y - 1; adjacentY <= y + 1; adjacentY++)
                    {
                        if (0 <= adjacentY && adjacentY < fieldSize.Height)
                        {
                            callbackFunction.Invoke(index, adjacentX, adjacentY);
                        }
                    }
                }
            }
        }

        private void solveViaFlippedNumbers()
        {
            bool change;
            do
            {
                change = false;
                for (LinkedListNode<int> k = flippedNumbers.First; k != null; k = k.Next)
                {
                    int count = 0;
                    int countFlagged = 0;
                    int countHiddenAdjacentFields = 0;
                    int x = (k.Value % fieldSize.Width);
                    int y = (int)Math.Floor(k.Value / (double)fieldSize.Width);

                    for (int adjacentX = ((0 <= x - 1) ? (x - 1) : x); adjacentX <= ((x + 1 < fieldSize.Width) ? (x + 1) : x); adjacentX++)
                    {
                        for (int adjacentY = ((0 <= y - 1) ? (y - 1) : y); adjacentY <= ((y + 1 < fieldSize.Height) ? (y + 1) : y); adjacentY++)
                        {
                            if (field[adjacentX + adjacentY * fieldSize.Width].isMine)
                            {
                                if (field[adjacentX + adjacentY * fieldSize.Width].state != Space.STATE_FLAGGED)
                                {
                                    count++;
                                }
                                else
                                {
                                    countFlagged++;
                                }
                            }
                            if (field[adjacentX + adjacentY * fieldSize.Width].state == Space.STATE_HIDDEN)
                            {
                                countHiddenAdjacentFields++;
                            }
                        }
                    }


                    if (count == countHiddenAdjacentFields && 0 < count)
                    {

                        for (int adjacentX = ((0 <= x - 1) ? (x - 1) : x); adjacentX <= ((x + 1 < fieldSize.Width) ? (x + 1) : x); adjacentX++)
                        {
                            for (int adjacentY = ((0 <= y - 1) ? (y - 1) : y); adjacentY <= ((y + 1 < fieldSize.Height) ? (y + 1) : y); adjacentY++)
                            {
                                if (field[adjacentX + adjacentY * fieldSize.Width].state == Space.STATE_HIDDEN)
                                {
                                    field[adjacentX + adjacentY * fieldSize.Width].state = Space.STATE_FLAGGED;
                                }
                            }
                        }
                        change = true;
                        flippedNumbers.Remove(k);
                    }
                    else if (countFlagged == field[k.Value].number)
                    {

                        for (int adjacentX = ((0 <= x - 1) ? (x - 1) : x); adjacentX <= ((x + 1 < fieldSize.Width) ? (x + 1) : x); adjacentX++)
                        {
                            for (int adjacentY = ((0 <= y - 1) ? (y - 1) : y); adjacentY <= ((y + 1 < fieldSize.Height) ? (y + 1) : y); adjacentY++)
                            {
                                if (field[adjacentX + adjacentY * fieldSize.Width].state == Space.STATE_HIDDEN)
                                {
                                    flipField(adjacentX + adjacentY * fieldSize.Width);
                                    if (minefield != null)
                                    {
                                        minefield.MainThreadInvoke(() =>
                                        {
                                            minefield.highlightedField = adjacentX + adjacentY * fieldSize.Width;
                                            minefield.disabled = false;
                                            minefield.disabled = true;
                                        });
                                        System.Threading.Thread.Sleep(10);
                                    }
                                }
                            }
                        }
                        change = true;
                        flippedNumbers.Remove(k);
                    }
                }
            } while (change == true);
        }

        private bool CheckWin()
        {
            for (int i = 0; i < field.Length; i++)
            {
                if (!field[i].isMine && field[i].state != Space.STATE_FACE_UP) return false;
            }
            return true;
        }
    }
}
