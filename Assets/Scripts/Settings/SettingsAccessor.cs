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
    public static class SettingsAccessor
    {
        private static PrivateSettings s_privateSettings;
        public static PrivateSettings PrivateSettings
        {
            get
            {
                if (s_privateSettings == null)
                {
                    s_privateSettings = Resources.Load<PrivateSettings>("Private/PrivateSettings");
                }
                return s_privateSettings;
            }
        }
    }
}