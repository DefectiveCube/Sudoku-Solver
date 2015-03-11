using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SudokuSolver
{
    public class Program
    {
        Dictionary<Position, SudokuValue> values;
        Dictionary<Position, SudokuValue> notes;
        Queue<Tuple<Position, SudokuValue>> updateQueue;
        int cellsFilled;
        int updateCount;
        int notesCount;

        static void Main(string[] args)
        {
            var game = new Program();

            game.Run();

            Console.ReadLine();
        }

        static IEnumerable<SudokuValue> GetFlags(SudokuValue input)
        {
            foreach (var value in (SudokuValue[])Enum.GetValues(input.GetType()))
            {
                if (input.HasFlag(value))
                {
                    yield return value;
                }
            }
        }

        static bool IsScalarValue(SudokuValue value)
        {
            switch (value)
            {
                case SudokuValue.One:
                case SudokuValue.Two:
                case SudokuValue.Three:
                case SudokuValue.Four:
                case SudokuValue.Five:
                case SudokuValue.Six:
                case SudokuValue.Seven:
                case SudokuValue.Eight:
                case SudokuValue.Nine:
                    return true;
                default:
                    return false;
            }
        }

        public Program()
        {
            values = new Dictionary<Position, SudokuValue>();
            notes = new Dictionary<Position, SudokuValue>();
            updateQueue = new Queue<Tuple<Position, SudokuValue>>();

            cellsFilled = 0;
        }

        public void Run()
        {
            Load();

            Console.WriteLine("Marking");
            Mark();

            while(updateQueue.Count > 0)
            {
                Update();
            }

            Console.WriteLine();

            while (FindSinglePossibilities())
            {
                while(updateQueue.Count > 0)
                {
                    Update();
                }
            }

            Console.WriteLine("{0} of 81 cells were filled in", cellsFilled);
            Console.WriteLine("{0} note marks remain", notesCount);
        }

        void Update()
        {
            HashSet<Position> ItemsToUpdate = new HashSet<Position>();

            Console.WriteLine();
            Console.WriteLine("[Update Cycle:{0}]", updateCount++);

            while (updateQueue.Count > 0)
            {
                var t = updateQueue.Dequeue();

                if (IsScalarValue(t.Item2) && notes.ContainsKey(t.Item1))
                {
                    Console.WriteLine("Set Value for Row {0} Column {1} Section {3} : {2}", t.Item1.Row, t.Item1.Column, t.Item2, t.Item1.Section);

                    if (values[t.Item1] == t.Item2)
                    {
                        // Ignore as the value being overwritten is the same
                        continue;
                    }
                    else if (values[t.Item1] == SudokuValue.None)
                    {
                        // Check values in row, column, and section to make sure value being inserted doesn't already exist
                        var check = ReadUnion(t.Item1.Row, t.Item1.Column) & t.Item2;

                        if (check == SudokuValue.None)
                        {
                            // Value was not found in other cells
                            values[t.Item1] = t.Item2;
                            notes.Remove(t.Item1);
                            cellsFilled++;

                            // Determine which cells need to be updated
                            var items = from c in notes
                                        where
                                            c.Key.Row == t.Item1.Row ||
                                            c.Key.Column == t.Item1.Column ||
                                            c.Key.Section == t.Item1.Section
                                        select c.Key;

                            foreach (var item in items)
                            {
                                if (!ItemsToUpdate.Contains(item))
                                {
                                    ItemsToUpdate.Add(item);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("ERROR! Unable to insert value");

                            throw new Exception("Value conflicts with a duplicate value in same row/column/section");
                        }

                    }
                    else
                    {
                        throw new Exception("Value was already written to");
                    }

                    if (cellsFilled == 81)
                    {
                        Console.WriteLine("Filled all cells");
                        updateQueue.Clear();
                    }
                }
                else
                {
                    // Update notes
                    if (notes.ContainsKey(t.Item1))
                    {
                        //Console.WriteLine("Updating notes for Row {0} Column {1}", t.Item1.Row, t.Item1.Column);
                        notes[t.Item1] = t.Item2;
                    }
                    else
                    {
                        Console.WriteLine("Operation already occured for Row {0} Column {1}", t.Item1.Row, t.Item1.Column);
                    }
                }
            }

            foreach (var item in ItemsToUpdate)
            {
                Mark(item);
            }
        }

        /// <summary>
        /// Loads sudoku data from a text file
        /// </summary>
        void Load()
        {
            string path = "sudoku.txt";

            using (StreamReader reader = new StreamReader(File.OpenRead(path)))
            {
                for (int row = 0; row < 9; row++)
                {
                    var chars = reader.ReadLine();

                    for (int col = 0; col < 9; col++)
                    {
                        int val;
                        var pos = new Position(row, col);

                        if (int.TryParse(chars.Substring(col, 1), out val))
                        {
                            cellsFilled++;
                        }
                        else
                        {
                            notes.Add(pos, SudokuValue.All);
                            notesCount += 9;
                        }

                        values.Add(pos, val != 0 ? (SudokuValue)(int)Math.Pow(2, val - 1) : SudokuValue.None);
                    }
                }
            }
        }

        /// <summary>
        /// Updates marks for all cells that haven't been assigned values
        /// </summary>
        public void Mark()
        {
            var cellsToMark = from c in values
                              where c.Value == SudokuValue.None
                              select c.Key;

            foreach(var p in cellsToMark)
            {
                Mark(p);
            }

            Console.WriteLine("{0} cell(s) were filled in", updateQueue.Count());
        }

        /// <summary>
        /// Updates marks for a specific cell
        /// </summary>
        /// <param name="position"></param>
        void Mark(Position position)
        {
            //Console.WriteLine("Updating Row {0} Column {1} Section {2}", position.Row, position.Column, position.Section);
            Mark(position.Row, position.Column);
        }

        /// <summary>
        /// Updates marks for a specific cell
        /// </summary>
        void Mark(int row, int col)
        {
            var pos = new Position(row, col);
            var union = ReadUnion(row, col);

            // Assume all values are possible
            var val = SudokuValue.All;

            if(values[pos] != SudokuValue.None || !notes.TryGetValue(pos, out val) || val == SudokuValue.None)
            {
                // 1. Value is already determined
                // 2. Notes do not exist for specific cell
                // 3. Retrieved note value is None, which means there was an error somewhere

                return;
            }

            // Remove (and do not add) all values that appear in the same group (row + column + section)
            val |= union;
            val ^= union;

            // Only update on changes
            if (val != notes[pos])
            {
                notesCount -= GetFlags(notes[pos]).Count() - GetFlags(val).Count();
                updateQueue.Enqueue(new Tuple<Position, SudokuValue>(pos, val));
            }
        }

        /// <summary>
        /// Finds values that can only be in one spot in a row or column
        /// </summary>
        /// <returns></returns>
        bool FindSinglePossibilities()
        {
            return FindSinglePossibilities(ReadRowsNotes(), SudokuType.Row) || FindSinglePossibilities(ReadColumnsNotes(), SudokuType.Column);
        }

        bool FindSinglePossibilities(IEnumerable<KeyValuePair<Position, SudokuValue>[]> values, SudokuType type = SudokuType.None)
        {
            bool result = false;
            Dictionary<SudokuValue, int> tracker = new Dictionary<SudokuValue, int>();

            // Enumerate the notes of each group
            foreach (var notes in values)
            {
                // Flags that appear once
                var singleFlags = SudokuValue.None;

                // Flags that appear more than once
                var ignoreFlags = SudokuValue.None;

                foreach (var note in notes)
                {
                    ignoreFlags |= singleFlags & note.Value;

                    // Enable flags
                    singleFlags |= note.Value;

                    // Disable flags
                    singleFlags |= ignoreFlags;
                    singleFlags ^= ignoreFlags;
                }

                if (singleFlags != SudokuValue.None)
                {
                    Console.WriteLine("Found a single {2} possibility! {0} | Value: {1}", notes.First().Key, singleFlags, type != SudokuType.None ? type.ToString().ToLowerInvariant() : "");

                    var needle = from n in notes
                                 where (n.Value & singleFlags) == singleFlags
                                 select n.Key;

                    if (needle.Count() == 1)
                    {
                        updateQueue.Enqueue(new Tuple<Position, SudokuValue>(needle.First(), singleFlags));
                        result = true;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Finds twins and updates notes
        /// </summary>
        /// <returns></returns>
        bool FindTwins()
        {
            return false;
        }

        bool FindTwoOfThree()
        {
            return false;
        }
        
        /// <summary>
        /// Return the value of the specified cell
        /// </summary>
        SudokuValue ReadCell(int row, int col)
        {
            var val = SudokuValue.None;
            var pos = new Position(row, col);

            if(!values.TryGetValue(pos, out val))
            {
                Console.WriteLine("Warning! Value not found on Row {0} Column {1}", row, col);
            }

            return val;
        }

        /// <summary>
        /// Return the values in the specified row
        /// </summary>
        SudokuValue ReadRow(int row)
        {
            var val = SudokuValue.None;

            for (int i = 0; i < 9; i++)
            {
                val |= ReadCell(row, i);
            }

            return val;
        }

        /// <summary>
        /// Returns the values in the specified column
        /// </summary>
        SudokuValue ReadColumn(int col)
        {
            var val = SudokuValue.None;

            for(int i = 0; i < 9; i++)
            {
                val |= ReadCell(i, col);
            }

            return val;
        }

        SudokuValue ReadSection(int row, int col)
        {
            return ReadSection(3 * (int)Math.Floor(row / 3.0) + (int)Math.Floor(col / 3.0));
        }

        SudokuValue ReadSection(int section)
        {
            var val = SudokuValue.None;

            var cells = from v in values
                        where v.Key.Section == section
                        select v.Value;

            foreach(var cell in cells)
            {
                val |= cell;
            }

            return val;
        }

        IEnumerable<SudokuValue> ReadRows()
        {
            for(int i = 0; i < 9; i++)
            {
                yield return ReadRow(i);
            }
        }

        IEnumerable<SudokuValue> ReadColumns()
        {
            for(int i = 0; i < 9; i++)
            {
                yield return ReadColumn(i);
            }
        }

        IEnumerable<SudokuValue> ReadSections()
        {
            for(int i = 0; i < 9; i++)
            {
                yield return ReadSection(i);
            }
        }

        IEnumerable<KeyValuePair<Position,SudokuValue>> ReadRowNotes(int row)
        {
            return from n in notes
                   where n.Key.Row == row
                   select n;
        }

        IEnumerable<KeyValuePair<Position,SudokuValue>> ReadColumnNotes(int column)
        {
            return from n in notes
                   where n.Key.Column == column
                   select n;
        }

        IEnumerable<KeyValuePair<Position,SudokuValue>> ReadSectionNotes(int section)
        {
            return from n in notes
                   where n.Key.Section == section
                   select n;
        }

        IEnumerable<KeyValuePair<Position,SudokuValue>[]> ReadRowsNotes()
        {
            for(int i = 0; i < 9; i++)
            {
                yield return ReadRowNotes(i).ToArray();
            }
        }

        IEnumerable<KeyValuePair<Position,SudokuValue>[]> ReadColumnsNotes()
        {
            for(int i = 0; i < 9; i++)
            {
                yield return ReadColumnNotes(i).ToArray();
            }
        }

        IEnumerable<KeyValuePair<Position,SudokuValue>[]> ReadSectionsNotes()
        {
            for(int i = 0; i < 9; i++)
            {
                yield return ReadSectionNotes(i).ToArray();
            }
        }

        /// <summary>
        /// Return the union of the row, column, and section
        /// </summary>
        SudokuValue ReadUnion(int row, int col)
        {
            return ReadRow(row) | ReadColumn(col) | ReadSection(row, col);
        }
    }
}
