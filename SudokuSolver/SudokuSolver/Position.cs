using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SudokuSolver
{
    public class Position
    {
        public Position(int cell)
        {
            Cell = cell;
            Column = cell % 9;
            Row = (int)Math.Floor(cell / 9.0);
        }

        public Position(int row, int column)
        {
            Row = row;
            Column = column;
            Cell = 9 * row + column;
            Section = 3 * (int)Math.Floor(Row / 3.0) + (int)Math.Floor(Column / 3.0);
        }

        public int Cell { get; private set; }

        public int Column { get; private set; }

        public int Row { get; private set; }

        public int Section { get; private set; }

        public override int GetHashCode()
        {
            return Cell;
        }

        public override bool Equals(object obj)
        {
            if (obj is Position)
            {
                var _obj = obj as Position;

                return _obj.Cell == this.Cell;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return string.Format("[Position: Cell={0}, Column={1}, Row={2}, Section={3}]", Cell, Column, Row, Section);
        }
    }
}
