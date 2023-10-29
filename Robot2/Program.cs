using System;
using System.Collections.Generic;

namespace Robot2
{
    public class World
    {
        class Cell
        {
            public Cell(bool occupied = false)
            {
                _isOccupied = occupied;
            }

            public bool Visited { get; set; } = false;
            public uint NumVisits { get; set; } = 0;
            
            public bool IsOccupied { get { return _isOccupied;  } }
            private bool _isOccupied;
        }

        public World(uint width, uint height, float density)
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());

            _width = width + 2;
            _height = height + 2;
            _field = new Cell [_height, _width];

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    bool blocked = false;
                    if (y == 0 || y == _height - 1|| x == 0 || x == _width - 1)
                        blocked = true;
                    else
                        blocked = (rand.Next(100) < 100* density);

                    _field[y, x] = new Cell(blocked);
                }
            }
        }

        public uint Width { get { return _width; } }
        public uint Height { get { return _height; } }

        public bool IsCellOccupied(uint x, uint y)
        {
            return _field[y, x].IsOccupied;
        }

        public bool IsCellVisited(uint x, uint y)
        {
            return _field[y, x].Visited;
        }

        public uint GetNumVisits(uint x, uint y)
        {
            return _field[y, x].NumVisits;
        }

        public void visitCell(uint x, uint y)
        {
            _field[y, x].Visited = true;
            _field[y, x].NumVisits++;
        }
        
        private uint _width;
        private uint _height;

         
        private Cell[,] _field = null;
    }

    public enum CellState
    {
        Blocked,
        Unblocked
    }
    
    public interface IScanner
    {
        CellState ScanDirection(Direction d);
    }

    public interface IMover
    {
        void MoveInDirection(Direction d);
    }

    public enum Direction
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3
    }

    class Arbiter : IScanner, IMover
    {
        uint _robotX;
        uint _robotY;
        World _world;

        public Arbiter(uint robotX, uint robotY, World world)
        {
            _robotX = robotX;
            _robotY = robotY;
            _world = world;

            world.visitCell(robotX, robotY);
        }

        public CellState ScanDirection(Direction d)
        {
            if (d == Direction.Left)
                return ScanLeft();
            else if(d == Direction.Right)
                return ScanRight();
            else if (d == Direction.Up)
                return ScanUp();
            else
                return ScanDown();
        }

        public CellState ScanDown()
        {
            if(_robotY == _world.Height - 1)
                return CellState.Blocked;

            return (_world.IsCellOccupied(_robotX, _robotY + 1) == true) ? CellState.Blocked : CellState.Unblocked;
        }

        public CellState ScanLeft()
        {
            if (_robotX == 1)
                return CellState.Blocked;

            return (_world.IsCellOccupied(_robotX - 1, _robotY) == true) ? CellState.Blocked : CellState.Unblocked;
        }

        public CellState ScanRight()
        {
            if (_robotX == _world.Width - 1)
                return CellState.Blocked;

            return (_world.IsCellOccupied(_robotX + 1, _robotY) == true) ? CellState.Blocked : CellState.Unblocked;
        }

        public CellState ScanUp()
        {
            if (_robotY == 1)
                return CellState.Blocked;

            return (_world.IsCellOccupied(_robotX, _robotY - 1) == true) ? CellState.Blocked : CellState.Unblocked;
        }

        public void MoveUp()
        {
            _robotY--;
            _world.visitCell(_robotX, _robotY);
        }

        public void MoveDown()
        {
            _robotY++;
            _world.visitCell(_robotX, _robotY);
        }

        public void MoveLeft()
        {
            _robotX--;
            _world.visitCell(_robotX, _robotY);
        }

        public void MoveRight()
        {
            _robotX++;
            _world.visitCell(_robotX, _robotY);
        }

        public void MoveInDirection(Direction d)
        {
            if (d == Direction.Left)
                MoveLeft();
            else if (d == Direction.Right)
                MoveRight();
            else if (d == Direction.Up)
                MoveUp();
            else
                MoveDown();
        }
    }

    public class Robot
    {
        public IScanner _scanner;
        private IMover _mover;

        public Robot(IScanner scanner, IMover mover)
        {
            _scanner = scanner;
            _mover = mover;
        }

        private Direction GetOppositDirection(Direction dir)
        {
            if (dir == Direction.Up)
                return Direction.Down;
            else
            if (dir == Direction.Down)
                return Direction.Up;
            else
            if (dir == Direction.Left)
                return Direction.Right;
            else
                return Direction.Left;
        }

        public class Coordinates
        {
            public Coordinates(int x, int y) => (X, Y) = (x, y);
            public int X { get; set; }
            public int Y { get; set; }
             
            public UInt64 GetHash()
            {
                //return (UInt64)Tuple.Create(X, Y).GetHashCode();
                UInt64 hashval = (UInt64)X << 32 | (UInt64)(UInt32)Y;
                return (ulong)hashval;
            }
        }

        // Map of visited coordinates
        private Dictionary<UInt64, bool> _map = new Dictionary<UInt64, bool>();
        
        // Travel tree for backtracking
        private Cell _travelTree;

        class Cell
        {
            public Cell(Coordinates coords) => (Coordinates) = coords;

            //public bool Visited { get; set; } = false;
            //public bool Blocked { get; set; } = false;
            public Cell [] AdjacentCells = null;

            public Coordinates Coordinates;

            public Cell(/*bool visited,*/ Cell[] adjacentCells, Coordinates coordinates)
            {
                //Visited = visited;
                AdjacentCells = adjacentCells;
                Coordinates = coordinates;            }
        }

        private Coordinates CalculateRelCoordinates(Coordinates c, Direction d)
        {
            int x = c.X;
            int y = c.Y;

            if (d == Direction.Up)
                y++;
            else
            if (d == Direction.Down)
                y--;
            else
            if (d == Direction.Right)
                x++;
            else
                x--;
        
            return new Coordinates(x, y);
        }

        private void Step(Cell currentCell, Direction d)
        {
            var comingFromDirection = GetOppositDirection(d);

            //currentCell.Visited = true;

            // Mark current location as visited
            _map[currentCell.Coordinates.GetHash()] = true;

            // Scan adjacent cells
            currentCell.AdjacentCells = new Cell[4];
            for(uint i = 0; i < 4; i++)
            {
                Direction dir = (Direction)i;
                Coordinates c = CalculateRelCoordinates(currentCell.Coordinates, dir);

                if (i != (uint)comingFromDirection && _scanner.ScanDirection((Direction)i) == CellState.Unblocked)
                {
                    // Skip already discovered cells
                    if (_map.ContainsKey(c.GetHash()))
                        continue;

                    // Record new cell in both, search tree and on the map. Mark it as not visited.
                    currentCell.AdjacentCells[i] = new Cell(c);
                    _map[c.GetHash()] = false;
                }
            }

            // Step through the adjacent cells
            for (int i = 0; i < 4; i++)
            {
                Direction newDirection = (Direction)i;
                Coordinates c = CalculateRelCoordinates(currentCell.Coordinates, newDirection);

                // Skip blocked or already visited cells
                if (currentCell.AdjacentCells[i] != null && _map[c.GetHash()] != true)
                {
                    // Execute move
                    _mover.MoveInDirection(newDirection);
                    Step(currentCell.AdjacentCells[i], newDirection);
                }
            }

            // Trace back
            _mover.MoveInDirection(comingFromDirection);
        }
        
        public void ExploreTheWorld()
        {
            _travelTree = new Cell(new Coordinates(0, 0));
            Direction dir = Direction.Right;
            Step(_travelTree, dir);
        }
    }


    internal class Program
    {

        static UInt64 GenerateHash(int a, int b)
        {
            UInt64 wideA = (UInt64)a;
            UInt64 wideB = (UInt64)(UInt32)b;

            UInt64 result = wideA << 32;
            result = result | wideB;

            return result;
        }

        static void TestHashingFunction()
        {
            Console.WriteLine(GenerateHash(0, -1));
            Console.WriteLine(GenerateHash(1, -1));
            Console.WriteLine(GenerateHash(2, -1));
            Console.WriteLine(GenerateHash(3, -1));


            for (int i = -1; i <= 3; i++)
                for (int j = -1; j <= 3; j++)
                {
                    Robot.Coordinates c = new Robot.Coordinates(i, j);
                    Console.WriteLine($"Coordinates: {i}, {j} \t: hash: {c.GetHash()}");
                }

            //var hash1 = Tuple.Create<uint, uint>(unchecked((uint) -1), 1).GetHashCode();
            //var hash2 = Tuple.Create<uint, uint>(1, unchecked((uint)-1)).GetHashCode();
            //var hash2 = Tuple.Create(1, -1).GetHashCode();

            var hash1 = new Robot.Coordinates(-1, 1).GetHash();
            var hash2 = new Robot.Coordinates(1, -1).GetHash();

            Console.WriteLine(hash1);
            Console.WriteLine(hash2);
        }

        static void PlayWithRobot()
        {
            const uint worldWidth = 25;
            const uint worldHeight = 25;
            const float obstacleDescity = 0.2f;

            var world = new World(worldWidth, worldHeight, obstacleDescity);

            for (uint i = 0; i < world.Width; i++)
            {
                for (uint j = 0; j < world.Height; j++)
                    Console.Write((world.IsCellOccupied(i, j) == true) ? "#" : " ");
                Console.WriteLine();
            }

            const uint LandingX = worldWidth / 2;
            const uint LandingY = worldHeight / 2;


            var arbiter = new Arbiter(LandingX, LandingY, world);
            var robot = new Robot(arbiter, arbiter);
            robot.ExploreTheWorld();

            Console.WriteLine("-----------------------------------------");

            for (uint i = 0; i < world.Width; i++)
            {
                for (uint j = 0; j < world.Height; j++)
                {
                    char c = ' ';

                    if (world.IsCellOccupied(i, j) == true)
                        c = '#';
                    else
                    //if (world.IsCellVisited(i, j) == true)
                    //    c = '@';
                    if (world.IsCellVisited(i, j) == true)
                        c = (char)('0' + (byte)(world.GetNumVisits(i, j)));


                    Console.Write(c);
                }
                Console.WriteLine();
            }
        }

        
        static void Main(string[] args)
        {
            PlayWithRobot();
        }
    }
}