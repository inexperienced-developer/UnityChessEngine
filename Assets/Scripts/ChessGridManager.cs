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
    public static class ChessGridManager
    {
        private static Dictionary<string, Vector3> m_gridPositions;
        public static Dictionary<string, Vector3> GridPositions
        {
            get
            {
                if(m_gridPositions == null)
                {
                    m_gridPositions = BuildGrid();
                }
                return m_gridPositions;
            }
        }
 
        private static char m_currentRow = 'a';
        private static byte m_currentCol = 1;

        private static Dictionary<string, Vector3> BuildGrid()
        {
            Dictionary<string, Vector3> grid = new Dictionary<string, Vector3>();
            //Chess board is 8x8 so lets create it
            //-4 to 4 is because we are centered at the origin (-4, -4)-(-3,-3) should be a1
            for (int i = -4; i < 4; i++)
            {
                for(int j = -4; j < 4; j++)
                {
                    Vector3 gridStart = new Vector3(i, 0, j);
                    Vector3 gridEnd = new Vector3(i + 1, 0, j + 1);
                    Vector3 spawnPoint = (gridEnd + gridStart) / 2;
                    grid.Add($"{m_currentRow}{m_currentCol}", spawnPoint);
                    m_currentCol++;
                    if(m_currentCol == 9)
                    {
                        m_currentRow++;
                        m_currentCol = 1;
                    }
                }
            }
            m_currentRow = 'a';
            m_currentCol = 1;
            return grid;
        }
    }
}

