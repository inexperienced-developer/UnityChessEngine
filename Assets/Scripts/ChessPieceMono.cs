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
using System.Linq;
using UnityEngine;

namespace InexperiencedDeveloper.Chess.Core
{
    public class ChessPieceMono : MonoBehaviour
    {
        #region Variables
        //Private
        private Player m_player;
        private bool m_isSelected;
        private bool m_isCaptured;
        [SerializeField] protected ChessPieceData m_piece;

        //Public
        public ChessPieceData Piece => m_piece;
        public Team Team { get; private set; }
        public string CurrentPos { get; private set; }
        public bool HasMoved { get; private set; } = false;
        public List<Move> PossibleMoves { get; private set; }
        public List<string> ThreatenedSquares { get; private set; } = new List<string>();
        public List<ChessPieceMono> AttackingPieces { get; private set; } = new List<ChessPieceMono>();
        #endregion
        #region Static Events
        public static event Action<ChessPieceMono, List<Move>> PossibleMovesChanged;
        public static void OnPossibleMovesChanged(ChessPieceMono piece, List<Move> possibleMoves) => PossibleMovesChanged?.Invoke(piece, possibleMoves);
        public static event Action<ChessPieceMono, List<string>> ThreatsChanged;
        public static void OnThreatsChanged(ChessPieceMono piece, List<string> threats) => ThreatsChanged?.Invoke(piece, threats);
        public static event Action<ChessPieceMono, List<ChessPieceMono>> AttacksChanged;
        public static void OnAttacksChanged(ChessPieceMono piece, List<ChessPieceMono> attacks) => AttacksChanged?.Invoke(piece, attacks);
        #endregion
        #region Init/Destroy
        public void Init(string pos, Team team)
        {
            PossibleMoves = new List<Move>();
            CurrentPos = pos;
            GameManager.OnPieceSpawned(this);
            Team = team;
            GameManager.PieceSelected += OnPieceSelected;
            GameManager.PieceCaptured += OnPieceCaptured;
            GameManager.PieceMoved += OnPieceMoved;
            GameManager.AllPiecesSpawned += OnAllPiecesSpawned;
            GameManager.PlayerAdded += OnPlayerAdded;
        }

        private void OnDestroy()
        {
            GameManager.PieceSelected -= OnPieceSelected;
            GameManager.PieceCaptured -= OnPieceCaptured;
            GameManager.PieceMoved -= OnPieceMoved;
            GameManager.AllPiecesSpawned -= OnAllPiecesSpawned;
            GameManager.PlayerAdded -= OnPlayerAdded;
            ResetMoveAndAttackLists();
            InvokeMoveAndAttackChanges();
            if (m_isSelected) GameManager.OnPieceSelected(null);
        }
        #endregion
        #region Delegates
        private void OnPieceMoved(ChessPieceMono piece, string oldPos, Move newPos)
        {
            FindPossibleMoves();
        }

        private void OnPlayerAdded(Player player)
        {
            if (player.Team == Team)
                m_player = player;
        }

        private void OnAllPiecesSpawned()
        {
            FindPossibleMoves();
            GameManager.AllPiecesSpawned -= OnAllPiecesSpawned;
        }

        private void OnPieceCaptured(ChessPieceMono capturer, ChessPieceMono captured)
        {
            ClearSquares();
            if (captured == this)
            {
                m_isCaptured = true;
                Destroy(gameObject);
            }
        }
        private void OnPieceSelected(ChessPieceMono piece)
        {
            if (piece == this && !m_isCaptured)
            {
                FindPossibleMoves();
                ShowPossibleMoves();
                m_isSelected = true;
            }
            else
            {
                m_isSelected = false;
            }
        }
        #endregion
        #region Helpers
        /// <summary>
        /// Clears all grid squares (used for resetting possible move visuals)
        /// </summary>
        private void ClearSquares()
        {
            foreach (var val in ChessSpawner.ActiveGrids)
            {
                val.Renderer.enabled = false;
            }
            ChessSpawner.ActiveGrids.Clear();
        }
        private void ResetMoveAndAttackLists()
        {
            PossibleMoves.Clear();
            ThreatenedSquares.Clear();
            AttackingPieces.Clear();
        }
        private int GetMaxSpacesBasedOnPieceType()
        {
            if (m_piece.PieceType == PieceType.Pawn)
            {
                return !HasMoved ? 2 : 1;
            }
            return m_piece.MaxMoveAmount;
        }
        private void InvokeMoveAndAttackChanges()
        {
            OnPossibleMovesChanged(this, PossibleMoves);
            OnThreatsChanged(this, ThreatenedSquares);
            OnAttacksChanged(this, AttackingPieces);
        }
        #endregion
        public void Move(Move newMove)
        {
            //Pretty straight forward
            HasMoved = true;
            string lastPos = CurrentPos;
            CurrentPos = newMove.Pos;
            //Can change to interpolation or whatever depending on use case
            transform.position = ChessGridManager.GridPositions[CurrentPos];
            PossibleMoves.Clear();
            ClearSquares();
            FindPossibleMoves();
            //Make sure to check every move if we are checking the king
            if (PossibleMoves.Any((move) =>
            {
                GameManager.ChessPieces.TryGetValue(move.Pos, out ChessPieceMono piece);
                return piece != null && piece.Piece.PieceType == PieceType.King && piece.Team != Team;
            }))
            {
                Team otherTeam = Team == Team.White ? Team.Black : Team.White;
                (List<string> checkingSquares, List<ChessPieceMono> pieces) check = GetPathBetweenPositions(GameManager.Players[otherTeam].KingPos, CurrentPos);
                GameManager.OnInCheck(this, check.checkingSquares, otherTeam);
            }
            GameManager.OnPieceMoved(this, lastPos, newMove);
            //If we were in check we shouldn't be now
            GameManager.OnOutCheck(Team);
            ClearSquares();
        }

        /// <summary>
        /// A method used to move the rook during castling, since techinically it's the king that has to castle
        /// </summary>
        /// <param name="newPos"></param>
        public void RookCastle(string newPos)
        {
            if (Piece.PieceType != PieceType.Rook) return;
            if (HasMoved) return;
            HasMoved = true;
            string lastPos = CurrentPos;
            CurrentPos = newPos;
            transform.position = ChessGridManager.GridPositions[CurrentPos];
            PossibleMoves.Clear();
            ClearSquares();
            FindPossibleMoves();
            if (PossibleMoves.Any((move) =>
            {
                GameManager.ChessPieces.TryGetValue(move.Pos, out ChessPieceMono piece);
                return piece != null && piece.Piece.PieceType == PieceType.King && piece.Team != Team;
            }))
            {
                Team otherTeam = Team == Team.White ? Team.Black : Team.White;
                (List<string> checkingSquares, List<ChessPieceMono> pieces) check = GetPathBetweenPositions(GameManager.Players[otherTeam].KingPos, CurrentPos);
                GameManager.OnInCheck(this, check.checkingSquares, otherTeam);
            }
        }
        public void FindPossibleMoves()
        {
            ResetMoveAndAttackLists();
            int maxSpaces = GetMaxSpacesBasedOnPieceType();
            char currentRow = CurrentPos[0];
            int currentCol = int.Parse($"{CurrentPos[1]}");
            CheckDirections(maxSpaces, currentRow, currentCol);
            CheckEnPassant(currentRow, currentCol);
            CheckCastling();
            InvokeMoveAndAttackChanges();
        }
        /// <summary>
        /// Displays the moves using the Grid GameObjects
        /// </summary>
        private void ShowPossibleMoves()
        {
            ClearSquares();
            foreach (var move in PossibleMoves)
            {
                var grid = ChessSpawner.GridSelectors[move.Pos];
                grid.Renderer.enabled = true;
                ChessSpawner.ActiveGrids.Add(grid);
            }
        }
        private void CheckCastling()
        {
            (bool kingSide, bool queenSide) castle = CheckCastle();
            if (castle.kingSide)
            {
                string pos = Team == Team.White ? "g1" : "g8";
                Move kingSideCastle = new Move(this, pos, castle: true);
                PossibleMoves.Add(kingSideCastle);
            }
            if (castle.queenSide)
            {
                string pos = Team == Team.White ? "c1" : "c8";
                Move queenSideCastle = new Move(this, pos, castle: true);
                PossibleMoves.Add(queenSideCastle);
            }
        }


        private void CheckEnPassant(char currentRow, int currentCol)
        {
            //If we aren't a pawn
            if (m_piece.PieceType != PieceType.Pawn) return;
            //If we are the first moved piece
            if (GameManager.LastMovedPiece == null) return;
            //If we are the last moved piece wasn't a pawn
            if (GameManager.LastMovedPiece.Piece.PieceType != PieceType.Pawn) return;
            if ((Team == Team.Black && currentCol == 4) ||
                (Team == Team.White && currentCol == 5))
            {
                //Check to our right and left
                int isWhite = Team == Team.White ? 1 : -1;
                char leftRow = (char)(currentRow - isWhite);
                char rightRow = (char)(currentRow + isWhite);
                if (leftRow >= 'a' && leftRow <= 'h')
                {
                    //Search left
                    EnPassantSearch(leftRow, currentCol, isWhite);
                }
                if (rightRow >= 'a' && rightRow <= 'h')
                {
                    //Search right
                    EnPassantSearch(rightRow, currentCol, isWhite);
                }
            }
        }

        private (bool, bool) CheckCastle()
        {
            (bool kingSide, bool queenSide) castleDir = (false, false);
            if (Piece.PieceType != PieceType.King) return castleDir;
            if (HasMoved) return castleDir;
            int isWhite = Team == Team.White ? 1 : 8;
            //King-side
            if (GameManager.ChessPieces[$"f{isWhite}"] == null &&
                GameManager.ChessPieces[$"g{isWhite}"] == null &&
                GameManager.ChessPieces[$"h{isWhite}"] != null &&
                GameManager.ChessPieces[$"h{isWhite}"].Piece.PieceType == PieceType.Rook &&
                !GameManager.ChessPieces[$"h{isWhite}"].HasMoved)
            {
                castleDir.kingSide = true;
                Debug.Log("Empty kingside");
            }
            if (GameManager.ChessPieces[$"d{isWhite}"] == null &&
                GameManager.ChessPieces[$"c{isWhite}"] == null &&
                GameManager.ChessPieces[$"b{isWhite}"] == null &&
                GameManager.ChessPieces[$"a{isWhite}"] != null &&
                GameManager.ChessPieces[$"a{isWhite}"].Piece.PieceType == PieceType.Rook &&
                !GameManager.ChessPieces[$"a{isWhite}"].HasMoved)
            {
                castleDir.queenSide = true;
                Debug.Log("Empty queenside");
            }
            //If neither is clear, then just skip the next part
            if (!castleDir.kingSide && !castleDir.queenSide) return castleDir;
            //If enemy can see the squares
            Player otherPlayer = GameManager.Players[Utils.GetOtherTeam(Team)];
            foreach (var moves in otherPlayer.TeamPossibleMoves.Values)
            {
                string[] kingSideSquares = Team == Team.White ?
                    new string[] { "e1", "f1", "g1", "h1" } :
                    new string[] { "e8", "f8", "g8", "h8" };
                string[] queenSideSquares = Team == Team.White ?
                    new string[] { "e1", "d1", "c1", "b1" } :
                    new string[] { "e8", "d8", "c8", "b8" };
                foreach (string squares in kingSideSquares)
                {
                    if (moves.Any(move => move.Pos == squares && move.MovingPiece != this))
                    {
                        castleDir.kingSide = false;
                        Move selectedMove = moves.Find(move => move.Pos == squares && move.MovingPiece != this);
                        Debug.Log($"My team: {Team}");
                        Debug.Log($"Other team: {otherPlayer.Team}");
                        Debug.Log($"Threatened kingside: {selectedMove.MovingPiece} - {selectedMove.MovingPiece.CurrentPos} - {selectedMove.MovingPiece.Team}");
                        break;
                    }
                }
                foreach (string squares in queenSideSquares)
                {
                    if (moves.Any(move => move.Pos == squares && move.MovingPiece != this))
                    {
                        Move selectedMove = moves.Find(move => move.Pos == squares && move.MovingPiece != this);
                        castleDir.queenSide = false;
                        Debug.Log($"My team: {Team}");
                        Debug.Log($"Other team: {otherPlayer.Team}");
                        Debug.Log($"Threatened queenside: {selectedMove.MovingPiece} - {selectedMove.MovingPiece.CurrentPos}  - {selectedMove.MovingPiece.Team}");
                        break;
                    }
                }
                if (!castleDir.kingSide && !castleDir.queenSide) return castleDir;
            }
            Debug.Log($"Can castle: {castleDir}");
            return castleDir;
        }

        private void CheckDirections(int maxSpaces, char currentRow, int currentCol)
        {
            foreach (MoveDir dir in Enum.GetValues(typeof(MoveDir)))
            {
                if ((m_piece.MoveDir & dir) != 0)
                {
                    switch (dir)
                    {
                        case MoveDir.Forward:
                            CheckDirection(maxSpaces, currentRow, currentCol, dir, m_piece.PieceType, 0, 1);
                            break;
                        case MoveDir.Backward:
                            CheckDirection(maxSpaces, currentRow, currentCol, dir, m_piece.PieceType, 0, -1);
                            break;
                        case MoveDir.Left:
                            CheckDirection(maxSpaces, currentRow, currentCol, dir, m_piece.PieceType, -1, 0);
                            break;
                        case MoveDir.Right:
                            CheckDirection(maxSpaces, currentRow, currentCol, dir, m_piece.PieceType, 1, 0);
                            break;
                        case MoveDir.Diagonal:
                            Diagonal(currentRow, currentCol, maxSpaces, m_piece.PieceType);
                            break;
                        case MoveDir.LShaped:
                            LShaped(currentRow, currentCol);
                            break;
                    }
                }
            }
        }

        private void CheckDirection(int maxMoves, char currentRow, int currentCol, MoveDir dir, PieceType type, int deltaRow, int deltaCol)
        {
            int isWhite = Team == Team.White ? 1 : -1;
            bool movePossible = true;
            bool canMoveHere = true;
            (List<string> squares, bool isPinned) pinned = IsPinned();
            for (int i = 0; i < maxMoves; i++)
            {
                if (!movePossible) return;
                char row = (char)(currentRow + ((i + 1) * isWhite * deltaRow));
                int col = currentCol + ((i + 1) * isWhite * deltaCol);

                if (row < 'a' || row > 'h' || col < 1 || col > 8) return;
                movePossible = SearchSquare($"{row}{col}", ref canMoveHere, pinned.squares);
            }
        }

        private void Diagonal(char currentRow, int currentCol, int maxMoves, PieceType type)
        {
            int isWhite = Team == Team.White ? 1 : -1;
            (int rowChange, int colChange)[] directionOffsets =
            {
                (1, 1),   // Right-up
                (-1, 1),  // Left-up
                (-1, -1), // Left-down
                (1, -1)   // Right-down
            };
            (List<string> squares, bool isPinned) pinned = IsPinned();
            if (type == PieceType.Pawn)
            {
                MakeMoves(currentRow, currentCol, maxMoves, isWhite, directionOffsets[0].rowChange, directionOffsets[0].colChange, type);
                MakeMoves(currentRow, currentCol, maxMoves, isWhite, directionOffsets[1].rowChange, directionOffsets[1].colChange, type);
                return;
            }
            foreach (var offset in directionOffsets)
            {
                if (pinned.isPinned)
                {
                    MakeMovesPinned(currentRow, currentCol, maxMoves, isWhite, offset.rowChange, offset.colChange, type, pinned.squares);
                }
                else
                {
                    MakeMoves(currentRow, currentCol, maxMoves, isWhite, offset.rowChange, offset.colChange, type);
                }
            }
        }


        private void MakeMovesPinned(char currentRow, int currentCol, int maxMoves, int isWhite, int deltaRow, int deltaCol, PieceType type, List<string> squares)
        {
            bool movePossible = true;
            bool isPawn = type == PieceType.Pawn;
            char newRow;
            int newCol;
            bool canMoveHere = true;
            if (!isPawn)
            {
                for (int i = 0; i < maxMoves; i++)
                {
                    if (!movePossible) break;

                    newRow = (char)(currentRow + ((i + 1) * isWhite * deltaRow));
                    newCol = currentCol + ((i + 1) * isWhite * deltaCol);

                    if (newRow < 'a' || newRow > 'h' || newCol < 1 || newCol > 8) break;

                    movePossible = SearchSquare($"{newRow}{newCol}", ref canMoveHere, squares);
                }
                return;
            }
            newRow = (char)(currentRow + 1 * isWhite * deltaRow);
            newCol = currentCol + (1 * isWhite * deltaCol);

            if (newRow < 'a' || newRow > 'h' || newCol < 1 || newCol > 8) return;

            movePossible = PawnDiagSearchSquare($"{newRow}{newCol}", squares);
        }

        private void MakeMoves(char currentRow, int currentCol, int maxMoves, int isWhite, int deltaRow, int deltaCol, PieceType type)
        {
            bool movePossible = true;
            bool isPawn = type == PieceType.Pawn;
            char newRow;
            int newCol;
            bool canMoveHere = true;
            if (!isPawn)
            {
                for (int i = 0; i < maxMoves; i++)
                {
                    if (!movePossible) break;

                    newRow = (char)(currentRow + ((i + 1) * isWhite * deltaRow));
                    newCol = currentCol + ((i + 1) * isWhite * deltaCol);

                    if (newRow < 'a' || newRow > 'h' || newCol < 1 || newCol > 8) break;

                    movePossible = SearchSquare($"{newRow}{newCol}", ref canMoveHere);
                }
                return;
            }
            newRow = (char)(currentRow + 1 * isWhite * deltaRow);
            newCol = currentCol + (1 * isWhite * deltaCol);

            if (newRow < 'a' || newRow > 'h' || newCol < 1 || newCol > 8) return;

            movePossible = PawnDiagSearchSquare($"{newRow}{newCol}");
        }

        private void LShaped(char currentRow, int currentCol)
        {
            if (IsPinned().isPinned) return;
            (int rowChange, int colChange)[] directionOffsets =
            {
                (1, 2),  // Up-right
                (-1, 2), // Up-left
                (-2, 1), // Left-up
                (-2, -1), // Left-down
                (1, -2), // Down-right
                (-1, -2), // Down-left
                (2, 1), // Right-up
                (2, -1) // Right-down
            };

            foreach (var offset in directionOffsets)
            {
                char newRow = (char)(currentRow + offset.rowChange);
                int newCol = currentCol + offset.colChange;

                if (newRow >= 'a' && newRow <= 'h' && newCol >= 1 && newCol <= 8)
                {
                    bool canMoveHere = true;
                    string newPosition = $"{newRow}{newCol}";
                    SearchSquare(newPosition, ref canMoveHere);
                }
            }
        }

        /// <summary>
        /// The default method for searching if a piece can move to a certain square
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="canMoveHere"></param>
        /// <param name="pinnedSquares"></param>
        /// <returns></returns>
        private bool SearchSquare(string pos, ref bool canMoveHere, List<string> pinnedSquares = null)
        {
            //If we are in double check and we aren't the king, we can't move
            if (m_player != null && m_player.DoubleCheck && Piece.PieceType != PieceType.King) return true;
            //If we are in check, we aren't the king, and our move doesn't block/capture we can't move there
            if (m_player != null && m_player.InCheck.inCheck && Piece.PieceType != PieceType.King &&
                m_player.InCheck.checkSquares != null && !m_player.InCheck.checkSquares.Contains(pos)) return true;
            ChessPieceMono piece = GameManager.ChessPieces[pos];
            Move move = new Move(this, pos);
            if (m_player != null && Piece.PieceType == PieceType.King && m_player.InCheck.inCheck)
            {
                Player enemy = GameManager.Players[Utils.GetOtherTeam(Team)];
                foreach (var moves in enemy.TeamPossibleMoves.Values)
                {
                    if (moves.Any(move => move.Pos == pos))
                    {

                        ThreatenedSquares.Add(move.Pos);
                        return true;
                    }
                }
                foreach(var moves in enemy.TeamThreats.Values)
                {
                    if((piece != null && moves.Any(move => move == piece.CurrentPos)))
                    {
                        if (piece != null)
                        {
                            Debug.Log("Piece: " + piece);
                            ThreatenedSquares.Add(move.Pos);
                            return true;
                        }
                    }
                }
            }
            if (piece == null)
            {
                if (m_piece.PieceType != PieceType.Pawn)
                    ThreatenedSquares.Add(move.Pos);
                if (canMoveHere && pinnedSquares == null)
                {
                    PossibleMoves.Add(move);
                }
                else if (pinnedSquares != null && pinnedSquares.Contains(pos))
                {
                    PossibleMoves.Add(move);
                }
                return true;
            }
            else if (m_piece.PieceType != PieceType.Pawn && piece.Team != Team)
            {
                if (m_piece.PieceType != PieceType.Pawn)
                        ThreatenedSquares.Add(move.Pos);
                if (canMoveHere && pinnedSquares == null)
                {
                    canMoveHere = false;
                    AttackingPieces.Add(piece);
                    PossibleMoves.Add(move);
                }
                else if (pinnedSquares != null && pinnedSquares.Contains(pos))
                {
                    AttackingPieces.Add(piece);
                    PossibleMoves.Add(move);
                }
                return true;
            }
            else if(piece.Team == Team)
            {
                canMoveHere = false;
                ThreatenedSquares.Add(move.Pos);
                return true;
            }
            else
            {
                ThreatenedSquares.Add(move.Pos);
                return true;
            }
        }

        private bool PawnDiagSearchSquare(string pos, List<string> pinnedSquares = null)
        {
            if (m_player != null && m_player.DoubleCheck && Piece.PieceType != PieceType.King) return true;
            if (m_player != null && m_player.InCheck.inCheck && Piece.PieceType != PieceType.King &&
                m_player.InCheck.checkSquares != null && !m_player.InCheck.checkSquares.Contains(pos)) return true;
            ChessPieceMono piece = GameManager.ChessPieces[pos];
            Move move = new Move(this, pos);
            if (piece != null && piece.Team != Team)
            {
                ThreatenedSquares.Add(move.Pos);
                if (pinnedSquares == null)
                {
                    AttackingPieces.Add(piece);
                    PossibleMoves.Add(move);
                }
                else if (pinnedSquares != null && pinnedSquares.Contains(pos))
                {
                    AttackingPieces.Add(piece);
                    PossibleMoves.Add(move);
                }
                return true;
            }
            else if (piece == null)
            {
                ThreatenedSquares.Add(move.Pos);
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool EnPassantSearch(char row, int col, int isWhite)
        {
            string pos = $"{row}{col}";
            if (m_player != null && m_player.DoubleCheck && Piece.PieceType != PieceType.King) return true;
            if (m_player != null && m_player.InCheck.inCheck && Piece.PieceType != PieceType.King &&
                m_player.InCheck.checkSquares != null && !m_player.InCheck.checkSquares.Contains(pos)) return true;
            ChessPieceMono piece = GameManager.ChessPieces[$"{row}{col}"];
            if (piece != null && piece == GameManager.LastMovedPiece && piece.Piece.PieceType == PieceType.Pawn)
            {
                Move move = new Move(this, $"{row}{col + isWhite}", true, piece);
                if (!IsPinned().isPinned)
                    PossibleMoves.Add(move);
                return true;
            }
            return false;
        }

        private (List<string> pinnedSquares, bool isPinned) IsPinned()
        {
            if (GameManager.Players == null || GameManager.Players.Count <= 0) return (null, false);
            Player otherPlayer = GameManager.Players[Utils.GetOtherTeam(Team)];
            List<ChessPieceMono> possiblePinners = new List<ChessPieceMono>();
            bool threatenedPiece = false;
            foreach (var moves in otherPlayer.TeamPossibleMoves.Values)
            {
                bool threat = moves.Any(move => move.Pos == CurrentPos);
                if (!threatenedPiece && threat) threatenedPiece = true;
                if (moves.Count <= 0) continue;
                if (threat && !possiblePinners.Contains(moves[0].MovingPiece)) possiblePinners.Add(moves[0].MovingPiece);
            }

            foreach (var pinner in possiblePinners)
            {
                bool threatenedKing = pinner.ThreatenedSquares.Any(move => move == m_player.KingPos);
                if (threatenedKing && threatenedPiece)
                {
                    (List<string> squares, List<ChessPieceMono> pieces) pathToPinner = GetPathBetweenPositions(m_player.KingPos, pinner.CurrentPos);
                    Debug.Log($"Path to pinner count: {pathToPinner.pieces.Count}");
                    if (pathToPinner.pieces.Count == 2 && pathToPinner.squares.Contains(CurrentPos)) // Only one piece in line and that's the current piece
                    {
                        return (pathToPinner.squares, true);
                    }
                }
            }
            return (null, false);
        }
        private (List<string> squares, List<ChessPieceMono> pieces) GetPathBetweenPositions(string pos1, string pos2)
        {
            (List<string> squares, List<ChessPieceMono> pieces) res = new();
            res.squares = new List<string>();
            res.pieces = new List<ChessPieceMono>();
            Debug.Log($"Getting path");
            List<string> path = new List<string>();
            char startRow = pos1[0];
            int startCol = int.Parse(pos1[1].ToString());
            char endRow = pos2[0];
            int endCol = int.Parse(pos2[1].ToString());
            int rowStep = startRow < endRow ? 1 : -1;
            int colStep = startCol < endCol ? 1 : -1;
            // If the positions are in the same row or column, or on the same diagonal, we need to iterate.
            if (startRow == endRow || startCol == endCol || Math.Abs(startRow - endRow) == Math.Abs(startCol - endCol))
            {
                char currentRow = startRow;
                int currentCol = startCol;
                while (true)
                {
                    if (currentRow != endRow) currentRow += (char)rowStep;
                    if (currentCol != endCol) currentCol += colStep;
                    res.squares.Add($"{currentRow}{currentCol}");
                    if (GameManager.ChessPieces.TryGetValue($"{currentRow}{currentCol}", out ChessPieceMono piece))
                    {
                        if (piece != null) res.pieces.Add(piece);
                    }
                    Debug.Log($"Added path: {currentRow}{currentCol}");
                    if (currentRow == endRow && currentCol == endCol) break;
                }
            }
            return res;
        }
    }

    [Serializable]
    public class Move
    {
        public ChessPieceMono MovingPiece { get; private set; }
        public string Pos { get; private set; }
        public bool EnPassant { get; private set; }
        public ChessPieceMono CaputuredPiece { get; private set; }
        public bool Castle { get; private set; }

        public Move(ChessPieceMono movingPiece, string pos, bool enPassant = false, ChessPieceMono caputuredPiece = null)
        {
            MovingPiece = movingPiece;
            Pos = pos;
            EnPassant = enPassant;
            CaputuredPiece = caputuredPiece;
        }
        public Move(ChessPieceMono movingPiece, string pos, bool castle)
        {
            MovingPiece = movingPiece;
            Pos = pos;
            Castle = castle;
        }
    }
}

