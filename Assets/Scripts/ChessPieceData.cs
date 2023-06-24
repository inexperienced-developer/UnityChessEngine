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
using UnityEngine;

namespace InexperiencedDeveloper.Chess.Core
{
    [Flags]
    public enum MoveDir : byte
    {
        None = 0,
        Forward = 1 << 0,
        Backward = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
        Diagonal = 1 << 4,
        LShaped = 1 << 5,
    }

    [CreateAssetMenu(menuName = "Game/New Piece", fileName = "New Piece")]
    public class ChessPieceData : ScriptableObject
    {
        [SerializeField] private PieceType m_pieceType;
        [SerializeField] private MoveDir m_moveDir;
        [SerializeField] private int m_maxMoveAmount;
        public MoveDir MoveDir => m_moveDir;
        public PieceType PieceType => m_pieceType;
        public int MaxMoveAmount => m_maxMoveAmount;
    }
}