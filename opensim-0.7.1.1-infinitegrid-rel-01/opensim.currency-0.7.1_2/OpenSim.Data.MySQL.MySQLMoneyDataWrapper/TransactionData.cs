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
 *     * Neither the name of the OpenSim Project nor the
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace OpenSim.Data.MySQL.MySQLMoneyDataWrapper
{
    public class TransactionData
    {
        UUID m_uuid;
        string m_sender = string.Empty;
        string m_receiver = string.Empty;
        int m_amount;
        int m_type;
        int m_time;
        int m_status;
        string m_description = string.Empty;
        string m_objectID = string.Empty;
        string m_regionHandle = string.Empty;
        string m_secureCode = string.Empty;
        //public TransactionData(string uuid, string sender, string receiver,
        //    int amount, int time, int status, string description)
        //{
        //    this.m_uuid = uuid;
        //    this.m_sender = sender;
        //    this.m_receiver = receiver;
        //    this.m_amount = amount;
        //}
        public UUID TransUUID
        {
            get { return m_uuid; }
            set { m_uuid = value; }
        }
        public string Sender
        {
            get { return m_sender; }
            set { m_sender = value; }
        }
        public string Receiver
        {
            get { return m_receiver; }
            set { m_receiver = value; }
        }
        public int Amount
        {
            get { return m_amount; }
            set { m_amount = value; }
        }
        public int Type
        {
            get { return m_type; }
            set { m_type = value; }
        }
        public int Time
        {
            get { return m_time; }
            set { m_time = value; }
        }
        public int Status
        {
            get { return m_status; }
            set { m_status = value; }
        }
        public string Description
        {
            get { return m_description; }
            set { m_description = value; }
        }
        public string ObjectUUID
        {
            get { return m_objectID; }
            set { m_objectID = value; }
        }
        public string RegionHandle
        {
            get { return m_regionHandle; }
            set { m_regionHandle = value; }
        }
        public string SecureCode
        {
            get { return m_secureCode; }
            set { m_secureCode = value; }
        }

    }

    public enum Status
    { 
        SUCCESS_STATUS = 0, 
        PENDING_STATUS = 1, 
        FAILED_STATUS = 2
    }

    public class UserInfo
    {
        string m_userID = string.Empty;
        string m_simIP = string.Empty;
        string m_avatarName = string.Empty;
        string m_passwordHash = string.Empty;

        public string UserID
        {
            get { return m_userID; }
            set { m_userID = value; }
        }

        public string SimIP
        {
            get { return m_simIP; }
            set { m_simIP = value; }
        }
        public string Avatar
        {
            get { return m_avatarName; }
            set { m_avatarName = value; }
        }
        public string PswHash
        {
            get { return m_passwordHash; }
            set { m_passwordHash = value; }
        }
    }
}
