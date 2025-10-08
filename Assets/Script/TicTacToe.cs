using System;
using System.Collections.Generic;

public enum Player { None = 0, X = 1, O = 2 }

public sealed class TicTacToe
{
    private struct Move
    {
        public int Index;
        public Player Player;
    }

    private static readonly int[][] WinLines = new int[][]
    {
        new[]{0,1,2}, new[]{3,4,5}, new[]{6,7,8},
        new[]{0,3,6}, new[]{1,4,7}, new[]{2,5,8},
        new[]{0,4,8}, new[]{2,4,6}
    };

    public const int BoardSize = 9;

    private readonly Player[] board = new Player[BoardSize];
    private readonly List<Move> moveHistory = new List<Move>(BoardSize);

    public Player CurrentPlayer { get; private set; } = Player.X;
    public Player Winner { get; private set; } = Player.None;
    public bool IsGameOver { get; private set; }
    public int WinningLineIndex { get; private set; } = -1;

    public void Reset()
    {
        for (int i = 0; i < BoardSize; i++) board[i] = Player.None;
        CurrentPlayer = Player.X;
        Winner = Player.None;
        IsGameOver = false;
        WinningLineIndex = -1;
        moveHistory.Clear();
    }

    public Player GetCell(int index)
    {
        if (index < 0 || index >= BoardSize) throw new ArgumentOutOfRangeException(nameof(index));
        return board[index];
    }

    public bool MakeMove(int index)
    {
        if (IsGameOver) return false;
        if (index < 0 || index >= BoardSize) return false;
        if (board[index] != Player.None) return false;

        board[index] = CurrentPlayer;
        moveHistory.Add(new Move { Index = index, Player = CurrentPlayer });
        EvaluateGameEnd();

        if (!IsGameOver)
        {
            CurrentPlayer = (CurrentPlayer == Player.X) ? Player.O : Player.X;
        }

        return true;
    }

    public List<int> GetAvailableMoves()
    {
        var moves = new List<int>(9);
        if (IsGameOver) return moves;
        for (int i = 0; i < BoardSize; i++)
        {
            if (board[i] == Player.None) moves.Add(i);
        }
        return moves;
    }

    private void EvaluateGameEnd()
    {
        // check wins
        for (int i = 0; i < WinLines.Length; i++)
        {
            int a = WinLines[i][0];
            int b = WinLines[i][1];
            int c = WinLines[i][2];

            var pa = board[a];
            if (pa == Player.None) continue;

            if (pa == board[b] && pa == board[c])
            {
                Winner = pa;
                IsGameOver = true;
                WinningLineIndex = i;
                return;
            }
        }
        // check draw
        bool anyEmpty = false;
        for (int i = 0; i < BoardSize; i++)
        {
            if (board[i] == Player.None)
            {
                anyEmpty = true;
                break;
            }
        }
        if (!anyEmpty)
        {
            Winner = Player.None;
            IsGameOver = true;
            WinningLineIndex = -1;
        }
    }

    public Player[] GetBoardCopy()
    {
        var copy = new Player[BoardSize];
        Array.Copy(board, copy, BoardSize);
        return copy;
    }

    public int UndoMoves(int count)
    {
        int undone = 0;
        while (count > 0 && moveHistory.Count > 0)
        {
            var last = moveHistory[moveHistory.Count - 1];
            moveHistory.RemoveAt(moveHistory.Count - 1);
            board[last.Index] = Player.None;
            // Revert to the player who made that move
            CurrentPlayer = last.Player;
            Winner = Player.None;
            IsGameOver = false;
            undone++;
            count--;
        }
        return undone;
    }

    public bool TryPeekLastMove(out Player player)
    {
        if (moveHistory.Count == 0)
        {
            player = Player.None;
            return false;
        }
        player = moveHistory[moveHistory.Count - 1].Player;
        return true;
    }

    public bool TryPeekPreviousMove(out Player player)
    {
        if (moveHistory.Count < 2)
        {
            player = Player.None;
            return false;
        }
        player = moveHistory[moveHistory.Count - 2].Player;
        return true;
    }

    public int GetMoveCount()
    {
        return moveHistory.Count;
    }

    public Player[] GetMovePlayersHistory()
    {
        if (moveHistory.Count == 0) return Array.Empty<Player>();
        var arr = new Player[moveHistory.Count];
        for (int i = 0; i < moveHistory.Count; i++)
        {
            arr[i] = moveHistory[i].Player;
        }
        return arr;
    }
}


