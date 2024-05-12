using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Drawing;
using System;
using OverflowBackend.Enums;

namespace OverflowBackend.Services
{
    public class Cell
    {
        public bool Consumed = false;
        public bool HasPlayerOne = false;
        public bool HasPlayerTwo = false;
        public CellType CellType { get; set; }
    }

    public class Coord
    {
        public int X;
        public int Y;
    }

    public class BoardLogic
    {
        private List<Cell> cells;
        private int playerOnePosition;
        private int playerTwoPosition;
        private bool playerOneMoves = true;
        public List<Coord> playerOneAvailableMoves;
        public List<Coord> playerTwoAvailableMoves;
        public List<double> BoardData;

        public BoardLogic()
        {
            cells = new List<Cell>();
            playerOneAvailableMoves = new List<Coord>();
            playerTwoAvailableMoves = new List<Coord>();

            BoardData = GenerateBoard();



            for (int index = 0; index < 25; index++)
            {
                var cell = new Cell();

                if (BoardData[index] == 2)
                {
                    cell.CellType = CellType.x2;
                }
                else if (BoardData[index] == 3)
                {
                    cell.CellType = CellType.Switch;
                }
                else if (BoardData[index] == 4)
                {
                    cell.CellType = CellType.infinite;
                }
                else if (BoardData[index] == 0.1)
                {
                    // set player 1
                    this.playerOnePosition = index;
                    cell.HasPlayerOne = true;
                    cell.CellType = CellType.Default;
                }
                else if (BoardData[index] == 0.2)
                {
                    // set player 1
                    this.playerTwoPosition = index;
                    cell.HasPlayerTwo = true;
                    cell.CellType = CellType.Default;
                }
                else
                {
                    cell.CellType = CellType.Default;
                }

                cells.Add(cell);
            }



            CellIsAvailableToMove();
        }

        public void CellIsAvailableToMove()
        {
            if (playerOneMoves)
            {
                playerOneAvailableMoves = new List<Coord>();
            }
            else
            {
                playerTwoAvailableMoves = new List<Coord>();
            }

            CellType cellType = cells[playerOneMoves ? playerOnePosition : playerTwoPosition].CellType;

            if (cellType == CellType.Default)
            {
                var position = GetRowAndColumn(playerOneMoves ? playerOnePosition : playerTwoPosition);
                var adjacentCoordinates = new List<Coord>();

                // Calculate adjacent coordinates in columns
                foreach (var colOffset in new[] { -1, 1 })
                {
                    int newX = position.X;
                    int newY = (position.Y + colOffset + 5) % 5;
                    if (!cells[GetIndex(newX, newY)].Consumed && PlayerNotHere(newX, newY))
                    {
                        adjacentCoordinates.Add(new Coord { X = newX, Y = newY });
                    }
                }

                // Calculate adjacent coordinates in rows
                foreach (var rowOffset in new[] { -1, 1 })
                {
                    int newX = (position.X + rowOffset + 5) % 5;
                    int newY = position.Y;
                    if (!cells[GetIndex(newX, newY)].Consumed && PlayerNotHere(newX, newY))
                    {
                        adjacentCoordinates.Add(new Coord { X = newX, Y = newY });
                    }
                }

                // Add adjacent coordinates to player moves
                foreach (var coord in adjacentCoordinates)
                {
                    if (playerOneMoves)
                    {
                        playerOneAvailableMoves.Add(coord);
                    }
                    else
                    {
                        playerTwoAvailableMoves.Add(coord);
                    }
                }
            }

            if (cellType == CellType.x2)
            {
                var position = GetRowAndColumn(playerOneMoves ? playerOnePosition : playerTwoPosition);
                var adjacentCoordinates = new List<Coord>();

                // Calculate adjacent coordinates in columns
                foreach (var colOffset in new[] { -2, 2 })
                {
                    int newX = position.X;
                    int newY = (position.Y + colOffset + 5) % 5;
                    if (!cells[GetIndex(newX, newY)].Consumed && PlayerNotHere(newX, newY))
                    {
                        adjacentCoordinates.Add(new Coord { X = newX, Y = newY });
                    }
                }

                // Calculate adjacent coordinates in rows
                foreach (var rowOffset in new[] { -2, 2 })
                {
                    int newX = (position.X + rowOffset + 5) % 5;
                    int newY = position.Y;
                    if (!cells[GetIndex(newX, newY)].Consumed && PlayerNotHere(newX, newY))
                    {
                        adjacentCoordinates.Add(new Coord { X = newX, Y = newY });
                    }
                }

                // Add adjacent coordinates to player moves
                foreach (var coord in adjacentCoordinates)
                {
                    if (playerOneMoves)
                    {
                        playerOneAvailableMoves.Add(coord);
                    }
                    else
                    {
                        playerTwoAvailableMoves.Add(coord);
                    }
                }
            }
            else if (cellType == CellType.infinite)
            {
                for (int index = 0; index < cells.Count; index++)
                {
                    int row = index / 5;
                    int column = index % 5;

                    if (!cells[index].Consumed && PlayerNotHere(row, column))
                    {
                        var coord = new Coord { X = row, Y = column };
                        if (playerOneMoves)
                        {
                            playerOneAvailableMoves.Add(coord);
                        }
                        else
                        {
                            playerTwoAvailableMoves.Add(coord);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Unhandled cell type");
            }
        }

        public bool MovePlayer(int row, int column)
        {
            if (playerOneMoves)
            {
                if (CellIsAvailable(row, column))
                {
                    int newPositionIndex = GetIndex(row, column);
                    cells[playerOnePosition].HasPlayerOne = false;
                    cells[playerOnePosition].Consumed = true;
                    cells[newPositionIndex].HasPlayerOne = true;
                    playerOnePosition = newPositionIndex;

                    if (cells[playerOnePosition].CellType == CellType.Switch)
                    {
                        cells[playerOnePosition].CellType = CellType.Default;

                        cells[playerOnePosition].HasPlayerOne = false;
                        cells[playerOnePosition].HasPlayerTwo = true;
                        cells[playerTwoPosition].HasPlayerOne = true;
                        cells[playerTwoPosition].HasPlayerTwo = false;

                        int temp = playerOnePosition;
                        playerOnePosition = playerTwoPosition;
                        playerTwoPosition = temp;
                    }

                    playerOneMoves = false;
                    CellIsAvailableToMove();
                    return true;
                }
            }
            else
            {
                if (CellIsAvailable(row, column))
                {
                    int newPositionIndex = GetIndex(row, column);
                    cells[playerTwoPosition].HasPlayerTwo = false;
                    cells[playerTwoPosition].Consumed = true;
                    cells[newPositionIndex].HasPlayerTwo = true;
                    playerTwoPosition = newPositionIndex;

                    if (cells[playerTwoPosition].CellType == CellType.Switch)
                    {
                        cells[playerTwoPosition].CellType = CellType.Default;

                        cells[playerOnePosition].HasPlayerOne = false;
                        cells[playerOnePosition].HasPlayerTwo = true;
                        cells[playerTwoPosition].HasPlayerOne = true;
                        cells[playerTwoPosition].HasPlayerTwo = false;

                        int temp = playerOnePosition;
                        playerOnePosition = playerTwoPosition;
                        playerTwoPosition = temp;
                    }

                    playerOneMoves = true;
                    CellIsAvailableToMove();
                    return true;
                }
            }
            return false;
        }

        public bool CellIsAvailable(int row, int column)
        {
            List<Coord> availableMoves = playerOneMoves ? playerOneAvailableMoves : playerTwoAvailableMoves;

            foreach (var coord in availableMoves)
            {
                if (row == coord.X && column == coord.Y)
                {
                    return true;
                }
            }
            return false;
        }

        private bool PlayerNotHere(int x, int y)
        {
            return playerOnePosition != GetIndex(x, y) && playerTwoPosition != GetIndex(x, y);
        }

        private Coord GetRowAndColumn(int index)
        {
            int row = index / 5;
            int column = index % 5;
            return new Coord { X = row, Y = column };
        }

        private int GetIndex(int row, int column)
        {
            return row * 5 + column;
        }

        private static int GetRandomNumber(int minValue, int maxValue)
        {
            Random random = new Random();

            // Generate a random number within the range
            int randomNumber = random.Next(minValue, maxValue + 1);
            return randomNumber;
        }

        static void Shuffle<T>(List<T> list)
        {
            Random random = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        private List<double> GenerateBoard()
        {
            var grid = new List<double>();
            var numberOf2s = GetRandomNumber(2, 4);

            for (var i = 0; i < numberOf2s; i++)
            {
                grid.Add(2);
            }
            for (var i = 0; i < 21 - numberOf2s; i++)
            {
                grid.Add(1);
            }
            grid.Add(3);
            grid.Add(4);
            grid.Add(0.1);
            grid.Add(0.2);
            Shuffle(grid);

            return grid;
        }
    }
}
