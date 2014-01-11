using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NumberPuzzle
{
    //Moves
    public enum Direction : byte { left, right, up, down};

    //Save Options
    public enum SaveOption : byte { saveSettingsAndBoard, saveSettingsOnly, dontSave }

    //Tiles
    public class Tile
    {
        public readonly int Value;
        //position in board
        public int Index { get; set; } //needs bounds checking now that it's public
        public int Row { get { return Index / this.board.Height; } }
        public int Column { get { return Index % this.board.Width; } }
        //check if tile can move
        public bool canMove(Direction d) { return Board.canMove(this, d); }
        public bool canMove()
        {
            return canMove(Direction.left) || canMove(Direction.right) ||
                  canMove(Direction.up) || canMove(Direction.down);
        }
        //move tile
        public bool move(Direction d) { return Board.move(this, d); }
        //internal use only
        //tile can be used as Content property of button, ToString() displays .Value
        public override string ToString() { return Value.ToString(); }
        //internal use only
        public readonly Board board;
        public Tile(Board board, int index, int value)
        { this.board = board; Index = index; Value = value; }
    }

    public class Board
    {
        //constants
        public const int DEFAULT_WIDTH = 4;
        public const int DEFAULT_HEIGHT = 4;
        public const int MIN_WIDTH = 2, MIN_HEIGHT = 2;
        public const int MAX_WIDTH = 11, MAX_HEIGHT = 11;
        public const SaveOption DEFAULT_SAVE_OPTION = SaveOption.saveSettingsOnly;

        //public constructors

        //from file
        public Board(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    using (FileStream fStream = File.OpenRead(path))
                    {
                        SaveOption saveOption = (SaveOption)(fStream.ReadByte());
                        switch (saveOption)
                        {
                            case SaveOption.dontSave: break;
                            case SaveOption.saveSettingsOnly:
                            case SaveOption.saveSettingsAndBoard:
                                int width = fStream.ReadByte();
                                int height = fStream.ReadByte();
                                initializeBoard(width, height);
                                if (saveOption == SaveOption.saveSettingsOnly)
                                {
                                    unmixPuzzle();
                                    return;
                                }
                                byte[] buffer = new byte[width * height];
                                fStream.Read(buffer, 0, buffer.Length);
                                int[] sequence = new int[buffer.Length];
                                for (int j = 0; j < sequence.Length; ++j)
                                {
                                    sequence[j] = (int)(buffer[j]);
                                }
                                this.Sequence = sequence;
                                return;
                            default: break;
                        }
                    }
                }
                catch { }
            }
            //default on file not found or file fail or dontSave
            initializeBoard(DEFAULT_WIDTH, DEFAULT_HEIGHT);
            unmixPuzzle();
        }
        //creates unmixed board
        public Board(int width = DEFAULT_WIDTH, int height = DEFAULT_HEIGHT) 
        { 
            initializeBoard(width, height);
            unmixPuzzle();
        }
        //copy constructor
        public Board(Board other)
        {
            initializeBoard(other.Width, other.Height);
            for (int i = 0; i < Squares; ++i)
            {
                this[i] = new Tile(this, other[i].Index, other[i].Value);
            }
        }

        //public properties
        public int Width
        {
            get
            {
                return this.width;
            }
            private set
            {
                this.width =
                    value < MIN_WIDTH ? MIN_WIDTH : value > MAX_WIDTH ? MAX_WIDTH : value;
            }
        }
        public int Height
        {
            get
            {
                return this.height;
            }
            private set
            {
                this.height =
                    value < MIN_HEIGHT ? MIN_HEIGHT : value > MAX_HEIGHT ? MAX_HEIGHT : value;
            }
        }
        public int Squares { get { return Width * Height; } }
        public int Tiles { get { return Squares - 1; } }
        public int[] Sequence //get/set tiles as int[] of values
        {
            get
            {
                int[] sequence = new int[this.Squares];
                for (int i = 0; i < Squares; ++i)
                {
                    sequence[i] = tiles[i].Value;
                }
                return sequence;
            }
            set
            {
                if (value != null && value.Distinct<int>().Count<int>() == this.Squares
                    && value.Max<int>() == this.Tiles && value.Min<int>() == 0)
                {
                    for (int i = 0; i < Squares; ++i)
                    {
                        tiles[i] = new Tile(this, i, value[i]);
                    }
                }
                if (OnBoardChanged != null)
                {
                    this.OnBoardChanged(this, new BoardChangedEventArgs(null, null, true));
                }
            }
        }
        public Tile this[int index] //0-based indexes; invalid index returns null
        {
            get
            {
                return (index < 0 || index >= this.Squares) ? null : this.tiles[index];
            }
            private set
            {
                if (index >= 0 && index < this.Squares) this.tiles[index] = value;
            }
        }
        public Tile this[int row, int column] // 0-based indexes; invalid index returns null
        {
            get
            {
                int index = row * Width + column;
                return (row < 0 || column < 0 || row >= Height || column >= Width) ?
                    null : this.tiles[index];     
            }
            private set
            {
                int index = row * Width + column;
                if (row >= 0 && column >= 0 && row < Height && column < Width
                    && value != null
                    && value.Value >= 0 && value.Value <= Tiles
                    && value.board != null
                    && value.Index == index)
                {
                    this.tiles[index] = value;
                }
            }
        }
        
        //public methods
        public void unmixPuzzle() { this.Sequence = orderedSequence(Squares); }
        public void mixPuzzle() 
        {
            Tile emptySquare = null;
            //Find empty tile and get reference
            for (int i = 0; i < Squares; ++i)
            {
                if (this.tiles[i].Value == 0)
                {
                    emptySquare = this.tiles[i];
                    break;
                }
            }
            if (emptySquare == null)
            {
                unmixPuzzle();
                return;
            }
            //number of mixing moves proportional to board size
            int moves = this.Width * this.Height * 10;
            List<Direction> directions = new List<Direction>(4);
            Direction[] fixedDirections = 
                new Direction[4] { Direction.left, Direction.up, Direction.right, Direction.down };
            Random random = new Random();
            int randomRange;
            Direction direction;
            //executes (moves) random moves of empty square
            for(int move = 0; move < moves; ++move)
            {
                directions.AddRange(fixedDirections);
                randomRange = 4;
                //tries to move empty space in random directions until it runs out
                while(!Board.move(emptySquare, (direction = directions[(random.Next(randomRange))]), true)
                    && randomRange > 1 /*included to avoid infinite loop in case of error*/)
                {
                    //if move fails, try another random direction from remaining directions
                    directions.Remove(direction);
                    --randomRange;
                }
            }
            //update view
            if(this.OnBoardChanged != null)
            {
                this.OnBoardChanged(this, new BoardChangedEventArgs(null, null, true));
            }
        }
        //checks if a tile can move
        public static bool canMove(Tile t, Direction d)
        {
            switch (d)
            {
                case Direction.left:
                    return t.Column > 0 && t.board[t.Row, t.Column - 1].Value == 0;
                case Direction.right:
                    return t.Column < t.board.Width - 1 && t.board[t.Row, t.Column + 1].Value == 0;
                case Direction.up:
                    return t.Row > 0 && t.board[t.Row - 1, t.Column].Value == 0;
                case Direction.down:
                    return t.Row < t.board.Height - 1 && t.board[t.Row + 1, t.Column].Value == 0;
                default: return false;
            }
        }
        //makes move if legal; autoMove allows any tile switch with no view update
        public static bool move(Tile t, Direction d, bool autoMove = false)
        {
            Tile other;
            switch (d)
            {
                case Direction.left:
                    other = t.board[t.Row, t.Column - 1];
                    break;
                case Direction.right:
                    other = t.board[t.Row, t.Column + 1];
                    break;
                case Direction.up:
                    other = t.board[t.Row - 1, t.Column];
                    break;
                case Direction.down:
                    other = t.board[t.Row + 1, t.Column];
                    break;
                default: return false;
            }
            //autoMove swaps tiles and does not update view
            if (other != null && (other.Value == 0 || autoMove))
            {
                Board.swap(t, other);
                if (!autoMove && t.board.OnBoardChanged != null)
                {
                    t.board.OnBoardChanged(t.board, new BoardChangedEventArgs(t, other));
                }
                return true;
            }
            return false;
        }
        public bool save(string path, SaveOption saveOption = DEFAULT_SAVE_OPTION)
        {
            try
            {
                switch (saveOption)
                {
                    case SaveOption.saveSettingsAndBoard:
                        this.saveBoard(path, saveOption);
                        return true;
                    case SaveOption.saveSettingsOnly:
                        this.saveBoard(path, saveOption);
                        return true;
                    case SaveOption.dontSave:
                        this.saveBoard(path, saveOption);
                        return true;
                    default: goto case SaveOption.dontSave;
                }
            }
            catch
            {
                try { this.saveBoard(path, SaveOption.dontSave); }
                catch { try { deleteSaveFile(path); } catch { } }
                return false;
            }
        }

        //events
        public event EventHandler<BoardChangedEventArgs> OnBoardChanged;

        //private methods

        //randomizing board does not properly mix it!!!!! do not use!!!
        //generates random sequence of howMany numbers in range [0,howMany)
        //private static int[] randomSequence(int howMany)
        //{
        //    //make a list of values [0,howMany)
        //    List<int> values = new List<int>();
        //    for (int i = 0; i < howMany; ++i)
        //    {
        //        values.Add(i);
        //    }
        //    //select at random from values until sequence is populated
        //    int[] sequence = new int[howMany];
        //    Random randomGenerator = new Random();
        //    int randomIndex, value;
        //    for (int i = 0; i < howMany; ++i)
        //    {
        //        randomIndex = randomGenerator.Next(0, howMany - 1 - i);
        //        sequence[i] = value = values[randomIndex];
        //        values.Remove(value);
        //    }
        //    return sequence;
        //}
        //generates ordered sequence of howMany numbers [1,howMany) with 0 at end
        private static int[] orderedSequence(int howMany)
        {
            int[] sequence = new int[howMany];
            for (int i = 0; i < howMany - 1; ++i)
            {
                sequence[i] = i + 1;
            }
            sequence[howMany - 1] = 0;
            return sequence;
        }
        //swaps 2 tiles in board; updates Tile indices
        private static void swap(Tile t1, Tile t2)
        {
            if (t1.board == t2.board)
            {
                t1.board[t1.Index] = t2;
                t2.board[t2.Index] = t1;
            }
            int temp = t1.Index;
            t1.Index = t2.Index;
            t2.Index = temp;
        }
        //saves game to file at path, no exception handling
        private void saveBoard(string path, SaveOption saveOption)
        {
            using (FileStream fStream = new FileStream(path,
               FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fStream.WriteByte((byte)saveOption);
                if (saveOption == SaveOption.dontSave) return;
                fStream.WriteByte((byte)this.Width);
                fStream.WriteByte((byte)this.Height);
                if (saveOption == SaveOption.saveSettingsOnly) return;
                byte[] buffer = new byte[this.Sequence.Length];
                for (int i = 0; i < buffer.Length; ++i)
                {
                    buffer[i] = (byte)(this.Sequence[i]);
                }
                fStream.Write(buffer, 0, buffer.Length);
            }
        }
        //deletes files at path, no exception handling
        private void deleteSaveFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        //used for constructors; does not set tiles to objects
        private void initializeBoard(int width, int height)
        {
            Width = width;
            Height = height;
            tiles = new Tile[Width * Height];
        }
        //private fields
        private int width;
        private int height;
        private Tile[] tiles;
    }

    public class BoardChangedEventArgs : EventArgs
    {
        public readonly Tile tile1;
        public readonly Tile tile2;
        public readonly bool newBoard;
        public BoardChangedEventArgs(Tile tile1, Tile tile2, bool newBoard = false)
        {
            this.tile1 = tile1;
            this.tile2 = tile2;
            this.newBoard = newBoard;
        }
    }
}
