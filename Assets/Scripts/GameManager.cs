/*
 * Copyright (c) 2023, Inexperienced Developer, LLC.
 * All rights reserved.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 *
 * Author: Jacob Berman
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InexperiencedDeveloper.Chess.Core
{
    public static class GameManager
    {
        public static Team Turn { get; private set; }
        public static List<Move> Moves { get; private set; } = new List<Move>();
        private static Dictionary<string, ChessPieceMono> s_chessPieces;
        private static Dictionary<Team, Player> s_players = new Dictionary<Team, Player>();
        public static Dictionary<Team, Player> Players => s_players;
        public static event Action<Player> PlayerAdded;
        public static void OnPlayerAdded(Player player)
        {
            s_players.Add(player.Team, player);
            player.Init();
            PlayerAdded?.Invoke(player);
        }
        public static event Action<Team> PlayerRemoved;
        public static void OnPlayerRemoved(Team team)
        {
            s_players.Remove(team);
            PlayerRemoved?.Invoke(team);
        }
        public static Dictionary<string, ChessPieceMono> ChessPieces
        {
            get
            {
                if (s_chessPieces == null)
                {
                    s_chessPieces = new Dictionary<string, ChessPieceMono>();
                    char row = 'a';
                    int col = 1;
                    for (int i = -4; i < 4; i++)
                    {
                        for (int j = -4; j < 4; j++)
                        {
                            if (col == 9)
                            {
                                row++;
                                col = 1;
                            }
                            s_chessPieces.Add($"{row}{col}", null);
                            col++;
                        }
                    }
                }
                return s_chessPieces;
            }
        }
        public static event Action<ChessPieceMono> PieceSpawned;
        public static void OnPieceSpawned(ChessPieceMono piece)
        {
            ChessPieces[piece.CurrentPos] = piece;
            PieceSpawned?.Invoke(piece);
        }
        public static event Action AllPiecesSpawned;
        public static void OnAllPiecesSpawned() => AllPiecesSpawned?.Invoke();
        public static ChessPieceMono LastMovedPiece { get; private set; }
        public static event Action<ChessPieceMono, string, Move> PieceMoved;
        public static void OnPieceMoved(ChessPieceMono piece, string oldPos, Move move)
        {
            if (piece == null) return;
            ChessPieces[oldPos] = null;
            ChessPieces[move.Pos] = piece;
            LastMovedPiece = piece;
            Turn = Turn == Team.White ? Team.Black : Team.White;
            foreach (var val in ChessSpawner.ActiveGrids)
            {
                val.Renderer.enabled = false;
            }
            ChessSpawner.ActiveGrids.Clear();
            PieceMoved?.Invoke(piece, oldPos, move);
            Moves.Add(move);
        }
        public static event Action<ChessPieceMono, ChessPieceMono> PieceCaptured;
        public static void OnPieceCaptured(ChessPieceMono capturer, ChessPieceMono captured) => PieceCaptured?.Invoke(capturer, captured);
        public static event Action<ChessPieceMono> PieceSelected;
        public static void OnPieceSelected(ChessPieceMono selected) => PieceSelected?.Invoke(selected);
        public static event Action<ChessPieceMono, List<string>, Team> InCheck;
        public static void OnInCheck(ChessPieceMono checkingPiece, List<string> checkingSquares, Team teamInCheck) => InCheck?.Invoke(checkingPiece, checkingSquares, teamInCheck);
        public static event Action<Team> OutCheck;
        public static void OnOutCheck(Team teamInCheck) => OutCheck?.Invoke(teamInCheck);
        public static event Action<Team> KingsideCastle;
        public static void OnKingsideCastle(Team team)
        {
            if (team == Team.White)
            {
                ChessPieces.TryGetValue("h1", out ChessPieceMono rook);
                if (rook == null)
                {
                    Debug.LogError("Error in castling, rook not found");
                    return;
                }
                rook.RookCastle("f1");
            }
            else
            {
                ChessPieces.TryGetValue("h8", out ChessPieceMono rook);
                if (rook == null)
                {
                    Debug.LogError("Error in castling, rook not found");
                    return;
                }
                rook.RookCastle("f8");
            }
            KingsideCastle?.Invoke(team);
        }
        public static event Action<Team> QueensideCastle;
        public static void OnQueensideCastle(Team team)
        {
            if (team == Team.White)
            {
                ChessPieces.TryGetValue("a1", out ChessPieceMono rook);
                if (rook == null)
                {
                    Debug.LogError("Error in castling, rook not found");
                    return;
                }
                rook.RookCastle("d1");
            }
            else
            {
                ChessPieces.TryGetValue("a8", out ChessPieceMono rook);
                if (rook == null)
                {
                    Debug.LogError("Error in castling, rook not found");
                    return;
                }
                rook.RookCastle("d8");
            }
            QueensideCastle?.Invoke(team);
        }
    }

}
