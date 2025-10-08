using System;
using System.Collections.Generic;

public static class TicTacToeAI
{
    public enum AILevel { None = 0, Easy = 1, Hard = 2 }

    private static readonly int[][] WinLines = new int[][]
    {
        new[]{0,1,2}, new[]{3,4,5}, new[]{6,7,8},
        new[]{0,3,6}, new[]{1,4,7}, new[]{2,5,8},
        new[]{0,4,8}, new[]{2,4,6}
    };

    public static int ChooseMove(TicTacToe game, AILevel level, Player aiPlayer)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        if (game.IsGameOver) return -1;

        var board = game.GetBoardCopy();
        var currentPlayer = game.CurrentPlayer;

        int maxDepth = level == AILevel.Hard ? 9 : 2; // depth limit for Easy

        int bestMove = -1;
        int bestScore = int.MinValue;
        foreach (var move in GetAvailableMoves(board))
        {
            board[move] = currentPlayer;
            int score = Minimax(board, Flip(currentPlayer), aiPlayer, 1, maxDepth);
            board[move] = Player.None;
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }
        return bestMove;
    }

    private static int Minimax(Player[] board, Player currentPlayer, Player aiPlayer, int depth, int maxDepth)
    {
        if (IsTerminal(board, out Player winner))
        {
            if (winner == aiPlayer) return 10 - depth;
            if (winner == Player.None) return 0; // draw
            return depth - 10; // opponent win
        }

        if (depth >= maxDepth)
        {
            return Heuristic(board, aiPlayer);
        }

        bool maximizing = currentPlayer == aiPlayer;
        int best = maximizing ? int.MinValue : int.MaxValue;
        foreach (var move in GetAvailableMoves(board))
        {
            board[move] = currentPlayer;
            int score = Minimax(board, Flip(currentPlayer), aiPlayer, depth + 1, maxDepth);
            board[move] = Player.None;
            if (maximizing)
            {
                if (score > best) best = score;
            }
            else
            {
                if (score < best) best = score;
            }
        }
        return best;
    }

    private static int Heuristic(Player[] board, Player aiPlayer)
    {
        // Lightweight positional heuristic for shallow search
        int score = 0;
        int[] corners = {0,2,6,8};
        int[] edges = {1,3,5,7};
        int center = 4;

        score += Positional(board[center], aiPlayer) * 3;
        foreach (var c in corners) score += Positional(board[c], aiPlayer) * 2;
        foreach (var e in edges) score += Positional(board[e], aiPlayer) * 1;

        return score;
    }

    private static int Positional(Player occupant, Player aiPlayer)
    {
        if (occupant == aiPlayer) return 1;
        if (occupant == Player.None) return 0;
        return -1;
    }

    private static bool IsTerminal(Player[] board, out Player winner)
    {
        foreach (var line in WinLines)
        {
            var a = board[line[0]];
            if (a == Player.None) continue;
            if (a == board[line[1]] && a == board[line[2]])
            {
                winner = a;
                return true;
            }
        }
        // draw if no empty cells
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == Player.None)
            {
                winner = Player.None;
                return false;
            }
        }
        winner = Player.None; // draw
        return true;
    }

    private static IEnumerable<int> GetAvailableMoves(Player[] board)
    {
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == Player.None) yield return i;
        }
    }

    private static Player Flip(Player p)
    {
        return p == Player.X ? Player.O : Player.X;
    }
}


