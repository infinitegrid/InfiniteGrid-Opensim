/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *	 * Redistributions of source code must retain the above copyright
 *	   notice, this list of conditions and the following disclaimer.
 *	 * Redistributions in binary form must reproduce the above copyright
 *	   notice, this list of conditions and the following disclaimer in the
 *	   documentation and/or other materials provided with the distribution.
 *	 * Neither the name of the OpenSim Project nor the
 *	   names of its contributors may be used to endorse or promote products
 *	   derived from this software without specific prior written permission.
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
using System.Text;
//using OpenSim.Grid.Framework;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using log4net;
using System.Reflection;
using OpenSim.Data;
using System.Timers;
using Nini.Config;
using System.IO;


namespace OpenSim.Grid.MoneyServer
{
	class MoneyServerBase : BaseOpenSimServer,IMoneyServiceCore
	{
		private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private string connectionString = string.Empty;
		private uint m_moneyServerPort = 8008;

		private int DEAD_TIME;
		private int MAX_DB_CONNECTION;

		private MoneyXmlRpcModule m_moneyXmlRpcModule;
		private MoneyDBService m_moneyDBService;

		private Dictionary<string, string> m_sessionDic = new Dictionary<string, string>();
		private Dictionary<string, string> m_secureSessionDic = new Dictionary<string, string>();
		private Dictionary<string, string> m_webSessionDic = new Dictionary<string, string>();

		IConfig m_config;


		public MoneyServerBase()
		{
			//m_console = new ConsoleBase("Money");
			m_console = new LocalConsole("Money");
			MainConsole.Instance = m_console;
		}


		public void Work()
		{
			//m_console.Notice("Enter help for a list of commands\n");

			//The timer checks the transactions table every 60 seconds
			Timer checkTimer = new Timer();
			checkTimer.Interval = 60*1000;
			checkTimer.Enabled = true;
			checkTimer.Elapsed += new ElapsedEventHandler(CheckTransaction);
			checkTimer.Start();
			while (true)
			{
				m_console.Prompt();
			}
		}


		/// <summary>
		/// Check the transactions table,set expired transaction state to failed
		/// </summary>
		private void CheckTransaction(object sender, ElapsedEventArgs e)
		{
			long ticksToEpoch = new DateTime(1970, 1, 1).Ticks;
			int unixEpochTime =(int) ((DateTime.Now.Ticks - ticksToEpoch )/10000000);
			int deadTime = unixEpochTime - DEAD_TIME;
			m_moneyDBService.SetTransExpired(deadTime);
		}


		protected override void StartupSpecific()
		{
			m_log.Info("[Money]: Starting HTTP process");
			ReadIniConfig();
			m_httpServer = new BaseHttpServer(m_moneyServerPort,true);
			SetupMoneyServices();
			m_httpServer.Start();
			base.StartupSpecific();

			//TODO : Add some console commands here
		}


		#region Obsolete method for ini parsing.
		[Obsolete("Now we put all the configs into MoneyServer.ini,please use ReadIniConfig instead")]
		/// <summary>
		/// Read configuration from mysql_connection.ini
		/// </summary>
		protected void ReadConfig()
		{
			m_log.Info("[MySQL] Reading mysql_connection.ini");
			IniFile MySqlMoneyConfig = new IniFile("mysql_connection.ini");
			string hostname = MySqlMoneyConfig.ParseFileReadValue("hostname");
			string database = MySqlMoneyConfig.ParseFileReadValue("database");
			string username = MySqlMoneyConfig.ParseFileReadValue("username");
			string password = MySqlMoneyConfig.ParseFileReadValue("password");
			string pooling 	= MySqlMoneyConfig.ParseFileReadValue("pooling");
			string port 	= MySqlMoneyConfig.ParseFileReadValue("port");
			connectionString = "Server=" + hostname + ";Port=" + port + ";Database=" + database + ";User ID=" +
										   username + ";Password=" + password + ";Pooling=" + pooling + ";";
		}
		#endregion 


		protected void ReadIniConfig()
		{
			MoneyServerConfigSource moneyConfig = new MoneyServerConfigSource();

			IConfig s_config = moneyConfig.m_config.Configs["Startup"];
			string PIDFile = s_config.GetString("PIDFile", "");
			if (PIDFile!="") Create_PIDFile(PIDFile);

			IConfig db_config = moneyConfig.m_config.Configs["MySql"];
			string hostname = db_config.GetString("hostname", "localhost");
			string database = db_config.GetString("database", "OpenSim");
			string username = db_config.GetString("username", "root");
			string password = db_config.GetString("password", "password");
			string pooling 	= db_config.GetString("pooling",  "false");
			string port 	= db_config.GetString("port", 	  "3306");
			MAX_DB_CONNECTION = db_config.GetInt("MaxConnection", 10);
			connectionString = "Server=" + hostname + ";Port=" + port + ";Database=" + database + ";User ID=" +
										   username + ";Password=" + password + ";Pooling=" + pooling + ";";

			m_config = moneyConfig.m_config.Configs["MoneyServer"];
			DEAD_TIME = m_config.GetInt("ExpiredTime", 120);
		}


		// added by skidz
		protected void Create_PIDFile(string path)
		{
			try
			{
				string pidstring = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
				FileStream fs = File.Create(path);
				System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
				Byte[] buf = enc.GetBytes(pidstring);
				fs.Write(buf, 0, buf.Length);
				fs.Close();
				m_pidFile = path;
			}
			catch (Exception)
			{
			}
		}


		protected virtual void SetupMoneyServices()
		{
			m_log.Info("[DATA]: Connecting to Money Storage Server");
			m_moneyDBService = new MoneyDBService();
			m_moneyDBService.Initialise(connectionString,MAX_DB_CONNECTION);
			m_moneyXmlRpcModule = new MoneyXmlRpcModule();
			m_moneyXmlRpcModule.Initialise(m_version,m_config, m_moneyDBService, this);
			m_moneyXmlRpcModule.PostInitialise();
		}


		public BaseHttpServer GetHttpServer()
		{
			return m_httpServer;
		}


		public Dictionary<string, string> GetSessionDic()
		{
			return m_sessionDic;
		}


		public Dictionary<string, string> GetSecureSessionDic()
		{
			return m_secureSessionDic;
		}


		public Dictionary<string, string> GetWebSessionDic()
		{
			return m_webSessionDic;
		}

	}



	class MoneyServerConfigSource
	{
		public IniConfigSource m_config;

		public MoneyServerConfigSource()
		{
			string configPath = Path.Combine(Directory.GetCurrentDirectory(), "MoneyServer.ini");
			if (File.Exists(configPath))
			{
				m_config = new IniConfigSource(configPath);
			}
			else
			{
				//TODO: create default configuration.
				//m_config = DefaultConfig();
			}
		}

		public void Save(string path)
		{
			m_config.Save(path);
		}

	}
}
