using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SudokuSolver
{
    [Flags]
    public enum SudokuValue
    {
        None    = 0,
        One     = 1,
        Two     = 2,
        Three   = 4,
        Four    = 8,
        Five    = 16,
        Six     = 32,
        Seven   = 64,
        Eight   = 128,
        Nine    = 256,
        All     = One | Two | Three | Four | Five | Six | Seven | Eight | Nine
    }
}
