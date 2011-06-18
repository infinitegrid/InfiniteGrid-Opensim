/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace OpenSim.Framework.Statistics
{
    /// <summary>
    /// Singleton used to provide access to statistics reporters
    /// </summary>
    public class StatsManager
    {
        private static AssetStatsCollector assetStats;
        private static UserStatsCollector userStats;
        private static SimExtraStatsCollector simExtraStats;

        public static AssetStatsCollector AssetStats { get { return assetStats; } }
        public static UserStatsCollector UserStats { get { return userStats; } }
        public static SimExtraStatsCollector SimExtraStats { get { return simExtraStats; } }

        private StatsManager() {}

        /// <summary>
        /// Start collecting statistics related to assets.
        /// Should only be called once.
        /// </summary>
        public static AssetStatsCollector StartCollectingAssetStats()
        {
            assetStats = new AssetStatsCollector();

            return assetStats;
        }

        /// <summary>
        /// Start collecting statistics related to users.
        /// Should only be called once.
        /// </summary>
        public static UserStatsCollector StartCollectingUserStats()
        {
            userStats = new UserStatsCollector();

            return userStats;
        }

        /// <summary>
        /// Start collecting extra sim statistics apart from those collected for the client.
        /// Should only be called once.
        /// </summary>
        public static SimExtraStatsCollector StartCollectingSimExtraStats()
        {
            simExtraStats = new SimExtraStatsCollector();

            return simExtraStats;
        }
    }
}
