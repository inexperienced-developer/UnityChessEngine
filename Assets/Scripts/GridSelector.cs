/*
 * Copyright (c) 2023, Inexperienced Developer, LLC.
 * All rights reserved.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 *
 * Author: Jacob Berman
 */

using UnityEngine;

namespace InexperiencedDeveloper.Chess.Core
{
    public class GridSelector : MonoBehaviour
    {
        public Renderer Renderer { get; private set; }
        public string Pos { get; private set; }
        public ChessPieceMono Piece { get; private set; }

        public void Init(string pos)
        {
            Renderer = GetComponent<Renderer>();
            Renderer.enabled = false;
            Pos = pos;
            Piece = GameManager.ChessPieces[Pos];
            GameManager.PieceMoved += OnPieceMoved;
            GameManager.PieceSpawned += OnPieceSpawned;
        }

        private void OnPieceSpawned(ChessPieceMono piece)
        {
            if (piece.CurrentPos == Pos)
            {
                Piece = piece;
            }
        }

        private void OnPieceMoved(ChessPieceMono piece, string oldPos, Move newMove)
        {
            if (Pos == newMove.Pos)
            {
                Piece = piece;
            }
            if (Pos == oldPos)
            {
                Piece = null;
            }
        }
    }
}

