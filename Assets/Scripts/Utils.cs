/*
 * Copyright (c) 2023, Inexperienced Developer, LLC.
 * All rights reserved.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 *
 * Author: Jacob Berman
 */

namespace InexperiencedDeveloper.Chess.Core
{
    public static class Utils
    {
        /// <summary>
        /// Returns the opposite of the team in the parameter, (i.e input -> White, output -> Black)
        /// </summary>
        /// <param name="team"></param>
        /// <returns></returns>
        public static Team GetOtherTeam(Team team)
        {
            return team == Team.White ? Team.Black : Team.White;
        }
    }
}

