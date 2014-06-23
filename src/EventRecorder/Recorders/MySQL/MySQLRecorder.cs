/*
 * Copyright (c) Contributors
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

using System;
using System.Data;
using System.Reflection;
using FluentMigrator.Runner;
using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using OpenSim.Region.Framework.Scenes;

namespace EventRecorder
{
    public class MySQLRecorder : IRecorder
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private string m_connectionString;

        public MySQLRecorder(IConfigSource configSource)
        {
            IConfig config = configSource.Configs["EventRecorder"];
            if (config == null)
                throw new Exception("No [EventRecorder] section found for configuring MySQLRecorder.");

            m_connectionString = config.GetString("ConnectionString");

            if (m_connectionString == null)
                throw new Exception("No ConnectionString parameter found in [EventRecorder] config for MySQLRecorder");

            m_log.DebugFormat("[MYSQL EVENT RECORDER]: Got connection string '{0}'", m_connectionString);

            Migrator migrator = new Migrator(m_connectionString);
            migrator.Migrate(runner => runner.MigrateUp());           
        }

        private bool RecordUserEvent(ScenePresence sp, string eventType)
        {
            try
            {
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    using (MySqlCommand cmd = new MySqlCommand(
                        "insert into Events (UserId, UserName, Type, Region, DateTime) values (?UserId, ?UserName, ?Type, ?Region, ?DateTime)",
                        dbcon))
                    {
                        cmd.Parameters.AddWithValue("?UserId", sp.UUID);
                        cmd.Parameters.AddWithValue("?UserName", sp.Name);
                        cmd.Parameters.AddWithValue("?Type", eventType);
                        cmd.Parameters.AddWithValue("?Region", sp.Scene.Name);
                        cmd.Parameters.AddWithValue("?DateTime", DateTime.UtcNow);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat(
                    "[MYSQL EVENT RECORDER]: Could not record {0} of avatar {1} {2} to {3}, error {4}", 
                    eventType, sp.Name, sp.UUID, sp.Scene.Name, e);

                return false;
            }

            return true;
        }

        public void RecordUserLogin(ScenePresence sp)
        {
            RecordUserEvent(sp, "login");

//            m_log.DebugFormat(
//                "[EVENT RECORDER]: Notified of avatar {0} {1} logging into scene {2}", 
//                sp.Name, sp.UUID, sp.Scene.Name);
        }

        public void RecordUserLogout(ScenePresence sp)
        {
            RecordUserEvent(sp, "logout");

//            m_log.DebugFormat(
//                "[EVENT RECORDER]: Notified of avatar {0} {1} logging out of scene {2}", 
//                sp.Name, sp.UUID, sp.Scene.Name);
        }

        public void RecordUserEntrance(ScenePresence sp)
        {
            RecordUserEvent(sp, "enter");

//            m_log.DebugFormat(
//                "[EVENT RECORDER]: Notified of avatar {0} {1} moving into scene {2}", 
//                sp.Name, sp.UUID, sp.Scene.Name);  
        }
    }
}