/*
 * Copyright (c) 2023, Inexperienced Developer, LLC.
 * All rights reserved.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 *
 * Author: Jacob Berman
 */

using System.Collections.Generic;
using UnityEngine;

namespace InexperiencedDeveloper.Chess.Core
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private Team m_team = Team.White;
        public Team Team => m_team;
        private bool m_myTurn => GameManager.Turn == m_team;
        private ChessPieceMono m_selectedPiece;
        public (bool inCheck, List<string> checkSquares) InCheck { get; private set; } = (false, null);
        public bool DoubleCheck { get; private set; }
        public string KingPos { get; private set; }

        public List<GridSelector> ActiveGrids;

        public Dictionary<ChessPieceMono, List<Move>> TeamPossibleMoves { get; private set; } = new();
        public Dictionary<ChessPieceMono, List<string>> TeamThreats { get; private set; } = new();
        public Dictionary<ChessPieceMono, List<ChessPieceMono>> TeamAttacks { get; private set; } = new();

        private void Awake()
        {
            GameManager.AllPiecesSpawned += Init;
            GameManager.PieceMoved += OnPieceMoved;
            GameManager.InCheck += OnInCheck;
            GameManager.OutCheck += OnOutCheck;
            GameManager.PieceSelected += OnPieceSelected;
            ChessPieceMono.PossibleMovesChanged += OnPossibleMovesChanged;
            ChessPieceMono.ThreatsChanged += OnThreatsChanged;
            ChessPieceMono.AttacksChanged += OnAttacksChanged;
            GameManager.OnPlayerAdded(this);
        }

        private void OnDestroy()
        {
            GameManager.AllPiecesSpawned -= Init;
            GameManager.PieceMoved -= OnPieceMoved;
            GameManager.InCheck -= OnInCheck;
            GameManager.OutCheck -= OnOutCheck;
            GameManager.PieceSelected -= OnPieceSelected;
            ChessPieceMono.PossibleMovesChanged -= OnPossibleMovesChanged;
            ChessPieceMono.ThreatsChanged -= OnThreatsChanged;
            ChessPieceMono.AttacksChanged -= OnAttacksChanged;
            GameManager.OnPlayerRemoved(Team);
        }

        public void Init()
        {
            if (KingPos == null)
            {
                foreach (var piece in GameManager.ChessPieces.Values)
                {
                    if (piece == null) continue;
                    if (piece.Piece.PieceType == PieceType.King && piece.Team == m_team)
                    {
                        KingPos = piece.CurrentPos;
                        break;
                    }
                }
            }
        }

        private void OnAttacksChanged(ChessPieceMono attacker, List<ChessPieceMono> beingAttacked)
        {
            if (attacker.Team != Team) return;
            if (!TeamAttacks.ContainsKey(attacker))
            {
                TeamAttacks.Add(attacker, beingAttacked);
                return;
            }
            TeamAttacks[attacker] = beingAttacked;
        }

        private void OnThreatsChanged(ChessPieceMono piece, List<string> threats)
        {
            if (piece.Team != Team) return;
            if (!TeamThreats.ContainsKey(piece))
            {
                TeamThreats.Add(piece, threats);
                return;
            }
            TeamThreats[piece] = threats;
        }

        private void OnPieceSelected(ChessPieceMono piece)
        {
            if (GameManager.Turn == Team)
            {
                m_selectedPiece = piece;
            }
        }

        private void OnPossibleMovesChanged(ChessPieceMono piece, List<Move> moves)
        {
            if (piece.Team != Team) return;
            if (!TeamPossibleMoves.ContainsKey(piece))
            {
                TeamPossibleMoves.Add(piece, moves);
                return;
            }
            TeamPossibleMoves[piece] = moves;
        }


        private void OnInCheck(ChessPieceMono checkingPiece, List<string> checkingSquares, Team teamInCheck)
        {
            if (InCheck.inCheck && teamInCheck == Team)
            {
                DoubleCheck = true;
            }
            InCheck = (teamInCheck == Team, checkingSquares);
            if (InCheck.inCheck) Debug.Log("Check!");
        }

        private void OnOutCheck(Team team)
        {
            if (m_team == team)
            {
                InCheck = (false, null);
                DoubleCheck = false;
            }
        }

        private void OnPieceMoved(ChessPieceMono piece, string oldPos, Move newMove)
        {
            //Update our king's position
            if (piece.Piece.PieceType == PieceType.King && piece.Team == Team)
            {
                KingPos = newMove.Pos;
            }
        }

        private void Update()
        {
            if (ActiveGrids != ChessSpawner.ActiveGrids)
                ActiveGrids = ChessSpawner.ActiveGrids;
            if (!m_myTurn) return;
            if (InputManagerMono.Instance.SelectPressed)
            {
                Ray ray = Camera.main.ScreenPointToRay(InputManagerMono.Instance.CursorPos);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, SettingsAccessor.PrivateSettings.PieceMask, QueryTriggerInteraction.Collide))
                {
                    ChessPieceMono piece = hit.collider.GetComponent<ChessPieceMono>();
                    if (piece.Team == m_team && m_selectedPiece != piece)
                    {
                        GameManager.OnPieceSelected(piece);
                    }
                }
                if (m_selectedPiece != null && Physics.Raycast(ray, out RaycastHit hit2, Mathf.Infinity, SettingsAccessor.PrivateSettings.PosMask, QueryTriggerInteraction.Collide))
                {
                    GridSelector grid = hit2.collider.GetComponent<GridSelector>();
                    if (grid != null)
                    {
                        if (grid != null && m_selectedPiece.PossibleMoves.Exists(move => move.Pos == grid.Pos))
                        {
                            Move selectedMove = m_selectedPiece.PossibleMoves.Find(move => move.Pos == grid.Pos);
                            if (grid.Piece != null)
                            {
                                GameManager.OnPieceCaptured(m_selectedPiece, grid.Piece);
                            }
                            if (selectedMove.EnPassant)
                            {
                                GameManager.OnPieceCaptured(m_selectedPiece, selectedMove.CaputuredPiece);
                            }
                            if (selectedMove.Castle)
                            {
                                switch (selectedMove.Pos)
                                {
                                    case "g1":
                                    case "g8":
                                        GameManager.OnKingsideCastle(Team);
                                        break;
                                    case "c1":
                                    case "c8":
                                        GameManager.OnQueensideCastle(Team);
                                        break;
                                    default:
                                        Debug.LogError("Not a castle move");
                                        break;
                                }
                            }
                            m_selectedPiece.Move(selectedMove);
                            GameManager.OnPieceSelected(null);
                            m_selectedPiece = null;
                        }
                    }
                }
            }

        }
    }
}
