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
    public enum Team : byte
    {
        White,
        Black
    }

    public enum PieceType : byte
    {
        King,
        Queen,
        Pawn,
        Bishop,
        Rook,
        Knight
    }

    public class ChessSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject m_gridSelectorPrefab;
        private static Dictionary<string, GridSelector> m_gridSelectors = new Dictionary<string, GridSelector>();
        public static Dictionary<string, GridSelector> GridSelectors => m_gridSelectors;
        public static List<GridSelector> ActiveGrids = new List<GridSelector>();

        [SerializeField] private Material m_whitePieces, m_blackPieces;
        [SerializeField] private GameObject m_pawn, m_bishop, m_knight, m_rook, m_queen, m_king;

        private void Awake()
        {
            Spawn();
            SetupGrid();
        }

        private void SetupGrid()
        {
            foreach (var key in ChessGridManager.GridPositions.Keys)
            {
                Vector3 pos = ChessGridManager.GridPositions[key];
                pos.y += 0.1f;
                m_gridSelectors[key] = Instantiate(m_gridSelectorPrefab, pos, Quaternion.identity).GetComponent<GridSelector>();
                m_gridSelectors[key].Init(key);
            }
        }

        public void Spawn()
        {
            SpawnPawns();
            SpawnKingsAndQueens();
            SpawnKnightRookBishop(PieceType.Bishop);
            SpawnKnightRookBishop(PieceType.Rook);
            SpawnKnightRookBishop(PieceType.Knight);
            GameManager.OnAllPiecesSpawned();
        }

        private void SpawnPawns()
        {
            for (char row = 'a'; row < 'i'; row++)
            {
                GameObject white = Instantiate(m_pawn, ChessGridManager.GridPositions[$"{row}2"], Quaternion.identity);
                GameObject black = Instantiate(m_pawn, ChessGridManager.GridPositions[$"{row}7"], Quaternion.identity);
                white.GetComponentInChildren<Renderer>().material = m_whitePieces;
                white.GetComponent<ChessPieceMono>().Init($"{row}2", Team.White);
                black.GetComponentInChildren<Renderer>().material = m_blackPieces;
                black.GetComponent<ChessPieceMono>().Init($"{row}7", Team.Black);
            }
        }

        private void SpawnKingsAndQueens()
        {
            GameObject whiteKing = Instantiate(m_king, ChessGridManager.GridPositions[$"e1"], Quaternion.identity);
            GameObject whiteQueen = Instantiate(m_queen, ChessGridManager.GridPositions[$"d1"], Quaternion.identity);
            GameObject blackKing = Instantiate(m_king, ChessGridManager.GridPositions[$"e8"], Quaternion.identity);
            GameObject blackQueen = Instantiate(m_queen, ChessGridManager.GridPositions[$"d8"], Quaternion.identity);
            whiteKing.GetComponentInChildren<Renderer>().material = m_whitePieces;
            whiteKing.GetComponent<ChessPieceMono>().Init($"e1", Team.White);
            whiteQueen.GetComponentInChildren<Renderer>().material = m_whitePieces;
            whiteQueen.GetComponent<ChessPieceMono>().Init($"d1", Team.White);
            blackKing.GetComponentInChildren<Renderer>().material = m_blackPieces;
            blackKing.GetComponent<ChessPieceMono>().Init($"e8", Team.Black);
            blackQueen.GetComponentInChildren<Renderer>().material = m_blackPieces;
            blackQueen.GetComponent<ChessPieceMono>().Init($"d8", Team.Black);
        }

        private void SpawnKnightRookBishop(PieceType pieceType)
        {
            GameObject prefab = pieceType == PieceType.Bishop ? m_bishop : pieceType == PieceType.Rook ? m_rook : m_knight;
            char[] rows = pieceType == PieceType.Bishop ? new char[2] { 'c', 'f' } :
                pieceType == PieceType.Rook ? new char[2] { 'a', 'h' } :
                new char[2] { 'b', 'g' };
            GameObject white1 = Instantiate(prefab, ChessGridManager.GridPositions[$"{rows[0]}1"], Quaternion.identity);
            GameObject white2 = Instantiate(prefab, ChessGridManager.GridPositions[$"{rows[1]}1"], Quaternion.identity);
            GameObject black1 = Instantiate(prefab, ChessGridManager.GridPositions[$"{rows[0]}8"], Quaternion.identity);
            GameObject black2 = Instantiate(prefab, ChessGridManager.GridPositions[$"{rows[1]}8"], Quaternion.identity);
            white1.GetComponent<ChessPieceMono>().Init($"{rows[0]}1", Team.White);
            white2.GetComponent<ChessPieceMono>().Init($"{rows[1]}1", Team.White);
            black1.GetComponent<ChessPieceMono>().Init($"{rows[0]}8", Team.Black);
            black2.GetComponent<ChessPieceMono>().Init($"{rows[1]}8", Team.Black);
            white1.GetComponentInChildren<Renderer>().material = m_whitePieces;
            white2.GetComponentInChildren<Renderer>().material = m_whitePieces;
            black1.GetComponentInChildren<Renderer>().material = m_blackPieces;
            black2.GetComponentInChildren<Renderer>().material = m_blackPieces;
        }
    }
}
