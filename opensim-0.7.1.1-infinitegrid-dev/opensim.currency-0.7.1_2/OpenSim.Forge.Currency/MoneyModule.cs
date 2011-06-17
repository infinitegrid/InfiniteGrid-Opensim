// * Copyright (c) Contributors, http://opensimulator.org/
// * See CONTRIBUTORS.TXT for a full list of copyright holders.
// *
// * Redistribution and use in source and binary forms, with or without
// * modification, are permitted provided that the following conditions are met:
// *	 * Redistributions of source code must retain the above copyright
// *	   notice, this list of conditions and the following disclaimer.
// *	 * Redistributions in binary form must reproduce the above copyright
// *	   notice, this list of conditions and the following disclaimer in the
// *	   documentation and/or other materials provided with the distribution.
// *	 * Neither the name of the OpenSim Project nor the
// *	   names of its contributors may be used to endorse or promote products
// *	   derived from this software without specific prior written permission.
// *
// * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
// * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
// * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Net;

using log4net;
using Nini.Config;
using Nwc.XmlRpc;
using OpenMetaverse;

using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenSim.Services.UserAccountService;

using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework;

using System.Security.Cryptography;
using NSL.XmlRpc;



namespace OpenSim.Forge.Currency
{
	public class MoneyModule : IMoneyModule, IRegionModule
	{
		/* Memebers *************************************************************/
		#region Constant numbers and members.

		// Constant memebers   
		private const int MONEYMODULE_REQUEST_TIMEOUT = 50000;

		public enum TransactionType : int
		{
			MONEY_TRANS_SYSTEMGENERATED = 0,
			MONEY_TRANS_REGIONMONEYREQUEST,
			MONEY_TRANS_GIFT,
			MONEY_TRANS_PURCHASE,
		}

		// Private data members.   
		private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private bool  m_enabled = true;
		private bool  m_sellEnabled = false;

		private IConfigSource m_config;

		private string m_moneyServURL = string.Empty;
		private string m_userServIP   = string.Empty;
		public BaseHttpServer HttpServer;

		/// <summary>   
		/// Scene dictionary indexed by Region Handle   
		/// </summary>   
		private Dictionary<ulong, Scene> m_sceneList = new Dictionary<ulong, Scene>();

		/// <summary>   
		/// To cache the balance data while the money server is not available.   
		/// </summary>   
		private Dictionary<UUID, int> m_moneyServer = new Dictionary<UUID, int>();

		// Events  
		public event ObjectPaid OnObjectPaid;


		// Price
		private int   ObjectCount = 0;
		private int   PriceEnergyUnit = 0;
		private int   PriceGroupCreate = 0;
		private int   PriceObjectClaim = 0;
		private float PriceObjectRent = 0f;
		private float PriceObjectScaleFactor = 0f;
		private int   PriceParcelClaim = 0;
		private float PriceParcelClaimFactor = 0f;
		private int   PriceParcelRent = 0;
		private int   PricePublicObjectDecay = 0;
		private int   PricePublicObjectDelete = 0;
		private int   PriceRentLight = 0;
		private int   PriceUpload = 0;
		private int   TeleportMinPrice = 0;
		private float TeleportPriceExponent = 0f;
		private float EnergyEfficiency = 0f;


		#endregion


		/* Public ***************************************************************/
		#region IRegionModule interface

		///
		public void Initialise(Scene scene, IConfigSource source)
		{
			// Handle the parameters errors.
			if (scene == null || source == null) return;

			try
			{
				m_config = source;

				// [Startup] secion
				IConfig networkConfig = m_config.Configs["Network"];

				m_userServIP = "";
				if (networkConfig.Contains("user_server_url")) {
					m_userServIP = Util.GetHostFromURL(networkConfig.GetString("user_server_url")).ToString();
				}

				// [Economy] section
				IConfig economyConfig = m_config.Configs["Economy"];

				if (economyConfig.GetString("EconomyModule").ToString() != Name)
				{
					m_enabled = false;
					m_log.InfoFormat("[MONEY]: The DTLMoneyModule is disabled.");
					return;
				}
				else
				{
					m_log.InfoFormat("[MONEY]: The DTLMoneyModule is enabled.");
				}

				m_sellEnabled = economyConfig.GetBoolean("SellEnabled", false);

				if (m_userServIP=="") {
					m_userServIP = Util.GetHostFromURL(economyConfig.GetString("UserServer")).ToString();
				}
				m_moneyServURL = economyConfig.GetString("CurrencyServer").ToString();

				// Price
				PriceEnergyUnit 		= economyConfig.GetInt("PriceEnergyUnit", 100);
				PriceObjectClaim 		= economyConfig.GetInt("PriceObjectClaim", 10);
				PricePublicObjectDecay 	= economyConfig.GetInt("PricePublicObjectDecay", 4);
				PricePublicObjectDelete = economyConfig.GetInt("PricePublicObjectDelete", 4);
				PriceParcelClaim 		= economyConfig.GetInt("PriceParcelClaim", 1);
				PriceParcelClaimFactor 	= economyConfig.GetFloat("PriceParcelClaimFactor", 1f);
				PriceUpload 			= economyConfig.GetInt("PriceUpload", 0);
				PriceRentLight 			= economyConfig.GetInt("PriceRentLight", 5);
				PriceObjectRent 		= economyConfig.GetFloat("PriceObjectRent", 1);
				PriceObjectScaleFactor 	= economyConfig.GetFloat("PriceObjectScaleFactor", 10);
				PriceParcelRent 		= economyConfig.GetInt("PriceParcelRent", 1);
				PriceGroupCreate 		= economyConfig.GetInt("PriceGroupCreate", 0);
				TeleportMinPrice 		= economyConfig.GetInt("TeleportMinPrice", 2);
				TeleportPriceExponent 	= economyConfig.GetFloat("TeleportPriceExponent", 2f);
				EnergyEfficiency 		= economyConfig.GetFloat("EnergyEfficiency", 1);

			}
			catch
			{
				m_log.ErrorFormat("[MONEY]: Faile to read configuration file.");
			}
			scene.RegisterModuleInterface<IMoneyModule>(this);

			lock (m_sceneList)
			{
				if (m_sceneList.Count == 0)
				{
					if (!string.IsNullOrEmpty(m_moneyServURL))
					{
						HttpServer = new BaseHttpServer(9000);
						HttpServer.AddStreamHandler(new Region.Framework.Scenes.RegionStatsHandler(scene.RegionInfo));

						HttpServer.AddXmlRPCHandler("UpdateBalance", BalanceUpdateHandler);
						HttpServer.AddXmlRPCHandler("UserAlert", UserAlertHandler);
						HttpServer.AddXmlRPCHandler("OnMoneyTransfered", OnMoneyTransferedHandlered);
						HttpServer.AddXmlRPCHandler("AddBankerMoney", AddBankerMoneyHandler);				// added by Fumi.Iseki
						HttpServer.AddXmlRPCHandler("SendMoneyBalance",  SendMoneyBalanceHandler);			// added by Fumi.Iseki
						HttpServer.AddXmlRPCHandler("GetBalance", GetBalanceHandler);						// added by Fumi.Iseki
						//HttpServer.AddXmlRPCHandler("SendConfirmLink", SendConfirmLinkHandler);

						MainServer.Instance.AddXmlRPCHandler("UpdateBalance", BalanceUpdateHandler);
						MainServer.Instance.AddXmlRPCHandler("UserAlert", UserAlertHandler);
						MainServer.Instance.AddXmlRPCHandler("OnMoneyTransfered", OnMoneyTransferedHandlered);
						MainServer.Instance.AddXmlRPCHandler("AddBankerMoney", AddBankerMoneyHandler);		// added by Fumi.Iseki
						MainServer.Instance.AddXmlRPCHandler("SendMoneyBalance",  SendMoneyBalanceHandler);	// added by Fumi.Iseki
						MainServer.Instance.AddXmlRPCHandler("GetBalance", GetBalanceHandler);				// added by Fumi.Iseki
						//MainServer.Instance.AddXmlRPCHandler("SendConfirmLink", SendConfirmLinkHandler);
					}
				}

				if (m_sceneList.ContainsKey(scene.RegionInfo.RegionHandle))
				{
					m_sceneList[scene.RegionInfo.RegionHandle] = scene;
				}
				else
				{
					m_sceneList.Add(scene.RegionInfo.RegionHandle, scene);
				}

			}

			scene.EventManager.OnNewClient += OnNewClient;
			scene.EventManager.OnMakeRootAgent += OnMakeRootAgent;
			scene.EventManager.OnMoneyTransfer += MoneyTransferAction;
			scene.EventManager.OnClientClosed  += ClientClosed;
			scene.EventManager.OnAvatarEnteringNewParcel += AvatarEnteringParcel;
			scene.EventManager.OnMakeChildAgent  += MakeChildAgent;
			scene.EventManager.OnValidateLandBuy += ValidateLandBuy;
			scene.EventManager.OnLandBuy += processLandBuy;
		}

	
		public void AddRegion(Scene scene)
		{
		}


		public void RemoveRegion(Scene scene)
		{
		}


		public void RegionLoaded(Scene scene)
		{
		}


		public Type ReplaceableInterface
		{
			get { return typeof(IMoneyModule); }
		}


		public bool IsSharedModule
		{
			get { return true; }
		}


		public string Name
		{
			get { return "DTLMoneyModule"; }
		}


		public void PostInitialise()
		{
		}


		public void Close()
		{
		}

		#endregion



		// Since the economy data won't be used anymore,	
		// removed the related legacy code from interface implement.   
		#region IMoneyModule interface.

		///
		// for LSL ObjectGiveMoney() function
		public bool ObjectGiveMoney(UUID objectID, UUID fromID, UUID toID, int amount)
		{
			//m_log.ErrorFormat("[MONEY]: LSL ObjectGiveMoney. UUID = ", objectID.ToString());

			if (!m_sellEnabled) return false;

			string objName = string.Empty;
			string avatarName = string.Empty;

			SceneObjectPart sceneObj = FindPrim(objectID);
			if (sceneObj != null)
			{
				objName = sceneObj.Name;
			}

			Scene scene = null;
			if (m_sceneList.Count > 0)
			{
				//scene = m_sceneList[0];
				foreach (Scene _scene in m_sceneList.Values)
				{
					scene = _scene;
					break;
				}
			}

			if (scene != null)
			{
				UserAccount account = scene.UserAccountService.GetUserAccount(scene.RegionInfo.ScopeID, toID);
				if (account != null)
				{
					avatarName = account.FirstName + " " + account.LastName;
				}

				//CachedUserInfo profile = scene.CommsManager.UserProfileCacheService.GetUserDetails(toID);
				//if (profile != null && profile.UserProfile != null)
				//{
				//	avatarName = profile.UserProfile.FirstName + " " + profile.UserProfile.SurName;
				//}
			}

			bool   ret = false;
			string description = String.Format("Object {0} pays {1}", objName, avatarName);

			if (sceneObj.OwnerID==fromID)
			{
				if (LocateClientObject(fromID)!=null)
				{
					ret = TransferMoney(fromID, toID, amount, 5009, objectID, sceneObj.RegionHandle, description);
				}
				else
				{
					ret = ForceTransferMoney(fromID, toID, amount, 5009, objectID, sceneObj.RegionHandle, description);
				}
			}

			return ret;
		}



		///
		public int GetBalance(UUID agentID)
		{
			IClientAPI client = LocateClientObject(agentID);
			return QueryBalanceFromMoneyServer(client);
		}


		public bool UploadCovered(IClientAPI client, int amount)
		{
			int balance = QueryBalanceFromMoneyServer(client);
			if (balance<amount) return false;
			return true;
		}


		public bool AmountCovered(IClientAPI client, int amount)
		{
			int balance = QueryBalanceFromMoneyServer(client);
			if (balance<amount) return false;
			return true;
		}


		///
		public void ApplyUploadCharge(UUID agentID, int amount, string text)
		{
			ulong region = LocateSceneClientIn(agentID).RegionInfo.RegionHandle;
			PayMoneyCharge(agentID, amount, 1101, region, text);
		}


		///
		public void ApplyCharge(UUID agentID, int amount, string text)
		{
			ulong region = LocateSceneClientIn(agentID).RegionInfo.RegionHandle;
			PayMoneyCharge(agentID, amount, 1002, region, text);
		}



		///
		public int UploadCharge
		{
			get { return PriceUpload; }
		}


		///
		public int GroupCreationCharge
		{
			get { return PriceGroupCreate; }
		}


		#endregion


		#region MoneyModule event handlers

		// 
		private void OnNewClient(IClientAPI client)
		{
			client.OnEconomyDataRequest += OnEconomyDataRequest;
			//client.OnMoneyBalanceRequest += OnMoneyBalanceRequest;
			//client.OnRequestPayPrice += OnRequestPayPrice;
			//client.OnObjectBuy += OnObjectBuy;
			//client.OnLogout += ClientClosed;
		}


		// added by Fumi.Iseki
		// 		for OnMakeRootAgent event
		public void OnMakeRootAgent(ScenePresence agent)
		{
			int balance = 0;
			IClientAPI client = agent.ControllingClient;

			//m_log.Debug("[MONEY]: OnMakeRootAgent.");

			LoginMoneyServer(client, out balance);
			client.SendMoneyBalance(UUID.Zero, true, new byte[0], balance);

			//client.OnEconomyDataRequest  += OnEconomyDataRequest;
			client.OnMoneyBalanceRequest += OnMoneyBalanceRequest;
			client.OnRequestPayPrice += OnRequestPayPrice;
			client.OnObjectBuy += OnObjectBuy;
			client.OnLogout += ClientClosed;
		}	   



		// for OnClientClosed event
		private void ClientClosed(UUID clientID, Scene scene)
		{
			IClientAPI client = LocateClientObject(clientID);
			if (client != null)
			{
				// If the User is just transferred to another region. No need to logoff from money server.
				// LogoffMoneyServer(client);
			}
		}



		// for OnClientClosed event
		private void ClientClosed(IClientAPI client)
		{
			if (client != null)
			{
				LogoffMoneyServer(client);
			}
		}



		// for OnMoneyTransfer event
		private void MoneyTransferAction(Object sender, EventManager.MoneyTransferArgs moneyEvent)
		{
			//m_log.ErrorFormat("[MONEY]: MoneyTransferAction. type = {0}", moneyEvent.transactiontype);
		
			if (!m_sellEnabled) return;

			// Check the money transaction is necessary.   
			if (moneyEvent.sender == moneyEvent.receiver)
			{
				return;
			}

			UUID receiver = moneyEvent.receiver;
			if (moneyEvent.transactiontype==5008)		// Pay for the object.   
			{
				SceneObjectPart sceneObj = FindPrim(moneyEvent.receiver);
				if (sceneObj != null)
				{
					receiver = sceneObj.OwnerID;
				}
				else
				{
					return;
				}
			}

			// Before paying for the object, save the object local ID for current transaction.
			UUID  objectID = UUID.Zero;
			ulong regionHandle = 0;

			if (sender is Scene)
			{
				Scene scene  = (Scene)sender;
				regionHandle = scene.RegionInfo.RegionHandle;

				if (moneyEvent.transactiontype==5008)
				{
					objectID = scene.GetSceneObjectPart(moneyEvent.receiver).UUID;
					m_log.Debug("Paying for object " + objectID.ToString());
				}
			}

			TransferMoney(moneyEvent.sender, receiver, moneyEvent.amount, moneyEvent.transactiontype, objectID, regionHandle, "OnMoneyTransfer event");
		}



		// for OnAvatarEnteringNewParcel event
		private void AvatarEnteringParcel(ScenePresence avatar, int localLandID, UUID regionID)
		{
			ILandObject obj = avatar.Scene.LandChannel.GetLandObject(avatar.AbsolutePosition.X, avatar.AbsolutePosition.Y);

			if ((obj.LandData.Flags & (uint)RegionFlags.AllowDamage) != 0)
			{
				avatar.Invulnerable = false;
			}
			else
			{
				avatar.Invulnerable = true;
			}
		}



		// for OnMakeChildAgent event
		private void MakeChildAgent(ScenePresence avatar)
		{
		}



		// for OnValidateLandBuy event
		private void ValidateLandBuy(Object sender, EventManager.LandBuyArgs landBuyEvent)
		{
			//m_log.ErrorFormat("[MONEY]: ValidateLandBuy:");
			
			IClientAPI senderClient = LocateClientObject(landBuyEvent.agentId);
			if (senderClient != null)
			{
				int balance = QueryBalanceFromMoneyServer(senderClient);
				if (balance >= landBuyEvent.parcelPrice)
				{
					lock (landBuyEvent)
					{
						landBuyEvent.economyValidated = true;
					}
				}
			}
		}



		// for OnLandBuy event
		private void processLandBuy(Object sender, EventManager.LandBuyArgs landBuyEvent)
		{
			//m_log.ErrorFormat("[MONEY]: processLandBuy:");

			if (!m_sellEnabled) return;

			lock (landBuyEvent)
			{
				if (landBuyEvent.economyValidated == true && landBuyEvent.transactionID == 0)
				{
					landBuyEvent.transactionID = Util.UnixTimeSinceEpoch();

					ulong parcelID = (ulong)landBuyEvent.parcelLocalID;
					UUID  regionID = UUID.Zero;
					if (sender is Scene) regionID = ((Scene)sender).RegionInfo.RegionID;

					if (TransferMoney(landBuyEvent.agentId, landBuyEvent.parcelOwnerID, landBuyEvent.parcelPrice, 5002, regionID, parcelID, "Land Purchase"))
					{
						lock (landBuyEvent)
						{
							landBuyEvent.amountDebited = landBuyEvent.parcelPrice;
						}
					}
				}
			}
		}



		// for OnObjectBuy event
		public void OnObjectBuy(IClientAPI remoteClient, UUID agentID, UUID sessionID, 
								UUID groupID, UUID categoryID, uint localID, byte saleType, int salePrice)
		{
			//m_log.ErrorFormat("[MONEY]: OnObjectBuy: agent = {0}, {1}", agentID, remoteClient.AgentId);

			// Handle the parameters error.   
			if (!m_sellEnabled) return;
			if (remoteClient==null || salePrice<0) return;		// for L$0 Sell  by Fumi.Iseki

			// Get the balance from money server.   
			int balance = QueryBalanceFromMoneyServer(remoteClient);
			if (balance < salePrice)
			{
				remoteClient.SendAgentAlertMessage("Unable to buy now. You don't have sufficient funds.", false);
				return;
			}

			Scene scene = LocateSceneClientIn(remoteClient.AgentId);
			if (scene != null)
			{
				SceneObjectPart sceneObj = scene.GetSceneObjectPart(localID);
				if (sceneObj != null)
				{
					IBuySellModule mod = scene.RequestModuleInterface<IBuySellModule>();
					if (mod!=null)
					{
						UUID receiverId = sceneObj.OwnerID;
						if (mod.BuyObject(remoteClient, categoryID, localID, saleType, salePrice))
						{
							TransferMoney(remoteClient.AgentId, receiverId, salePrice, 5008, sceneObj.UUID, sceneObj.RegionHandle, "Object Buy");
						}
					}
				}
				else
				{
					remoteClient.SendAgentAlertMessage("Unable to buy now. The object was not found.", false);
					return;
				}
			}
		}


		#endregion


		#region MoneyModule XML-RPC Handler

		// for UpdateBlance RPC
		// Money Server -> Region Server -> Viewer
		public XmlRpcResponse BalanceUpdateHandler(XmlRpcRequest request, IPEndPoint remoteClient)
		{
			bool ret = false;

			#region Update the balance from money server.

			if (request.Params.Count > 0)
			{
				Hashtable requestParam = (Hashtable)request.Params[0];
				if (requestParam.Contains("clientUUID") &&
					requestParam.Contains("clientSessionID") &&
					requestParam.Contains("clientSecureSessionID"))
				{
					UUID clientUUID = UUID.Zero;
					UUID.TryParse((string)requestParam["clientUUID"], out clientUUID);
					if (clientUUID != UUID.Zero)
					{
						IClientAPI client = LocateClientObject(clientUUID);
						if (client != null &&
							client.SessionId.ToString() == (string)requestParam["clientSessionID"] &&
							client.SecureSessionId.ToString() == (string)requestParam["clientSecureSessionID"])
						{
							if (requestParam.Contains("Balance"))
							{
								// Send notify to the client.   
								string msg = "";
								if (requestParam.Contains("Message")) msg = (string)requestParam["Message"];
								client.SendMoneyBalance(UUID.Random(), true, Utils.StringToBytes(msg), (int)requestParam["Balance"]);
								ret = true;
							}
						}
					}
				}
			}

			#endregion

			// Send the response to money server.
			XmlRpcResponse resp = new XmlRpcResponse();
			Hashtable paramTable = new Hashtable();
			paramTable["success"] = ret;
			if (!ret)
			{
				m_log.ErrorFormat("[MONEY]: Cannot update client balance from MoneyServer.");
			}
			resp.Value = paramTable;

			return resp;
		}



		// for UserAlert RPC
		public XmlRpcResponse UserAlertHandler(XmlRpcRequest request, IPEndPoint remoteClient)
		{
			bool ret = false;

			#region confirm the request and show the notice from money server.

			if (request.Params.Count > 0)
			{
				Hashtable requestParam = (Hashtable)request.Params[0];
				if (requestParam.Contains("clientUUID") &&
					requestParam.Contains("clientSessionID") &&
					requestParam.Contains("clientSecureSessionID"))
				{
					UUID clientUUID = UUID.Zero;
					UUID.TryParse((string)requestParam["clientUUID"], out clientUUID);
					if (clientUUID != UUID.Zero)
					{
						IClientAPI client = LocateClientObject(clientUUID);
						if (client != null &&
							client.SessionId.ToString() == (string)requestParam["clientSessionID"] &&
							client.SecureSessionId.ToString() == (string)requestParam["clientSecureSessionID"])
						{
							if (requestParam.Contains("Description"))
							{
								string description = (string)requestParam["Description"];
								// Show the notice dialog with money server message.
							   	GridInstantMessage gridMsg = new GridInstantMessage(null, UUID.Zero, "MonyServer", new UUID(clientUUID.ToString()),
																	(byte)InstantMessageDialog.MessageFromAgent, description, false, new Vector3());
								client.SendInstantMessage(gridMsg);
								ret = true; 
							}
						}
					}
				}
			}
			//
			#endregion

			// Send the response to money server.
			XmlRpcResponse resp = new XmlRpcResponse();
			Hashtable paramTable = new Hashtable();
			paramTable["success"] = ret;

			resp.Value = paramTable;
			return resp;
		}



		// for OnMoneyTransfered RPC from Money Server
		public XmlRpcResponse OnMoneyTransferedHandlered(XmlRpcRequest request, IPEndPoint remoteClient)
		{
			//m_log.ErrorFormat("[MONEY]: OnMoneyTransferedHandlered:");

			bool ret = false;

			if (request.Params.Count > 0)
			{
				Hashtable requestParam = (Hashtable)request.Params[0];
				if (requestParam.Contains("senderID") &&
					requestParam.Contains("receiverID") &&
					requestParam.Contains("senderSessionID") &&
					requestParam.Contains("senderSecureSessionID"))
				{
					UUID senderID = UUID.Zero;
					UUID receiverID = UUID.Zero;
					UUID.TryParse((string)requestParam["senderID"], out senderID);
					UUID.TryParse((string)requestParam["receiverID"], out receiverID);
					if (senderID != UUID.Zero)
					{
						IClientAPI client = LocateClientObject(senderID);
						if (client != null &&
							client.SessionId.ToString() == (string)requestParam["senderSessionID"] &&
							client.SecureSessionId.ToString() == (string)requestParam["senderSecureSessionID"])
						{
							if (requestParam.Contains("transactionType") &&
								requestParam.Contains("objectID") &&
								requestParam.Contains("amount"))
							{
								//m_log.ErrorFormat("[MONEY]: OnMoneyTransferedHandlered: type = {0}", requestParam["transactionType"]);
								if ((int)requestParam["transactionType"]==5008)	// Pay for the object.
								{
									// Send notify to the client(viewer) for Money Event Trigger.   
									ObjectPaid handlerOnObjectPaid = OnObjectPaid;
									if (handlerOnObjectPaid != null)
									{
										UUID objectID = UUID.Zero;
										UUID.TryParse((string)requestParam["objectID"], out objectID);
										handlerOnObjectPaid(objectID, senderID, (int)requestParam["amount"]);
									}
									ret = true;
								}
							}
						}
					}
				}
			}

			// Send the response to money server.
			XmlRpcResponse resp = new XmlRpcResponse();
			Hashtable paramTable = new Hashtable();
			paramTable["success"] = ret;
			if (!ret)
			{
				m_log.ErrorFormat("[MONEY]: ERROR: Transaction is failed. MoneyServer will rollback.");
			}
			resp.Value = paramTable;

			return resp;
		}



		// for AddBankerMoney RPC
		public XmlRpcResponse AddBankerMoneyHandler(XmlRpcRequest request, IPEndPoint remoteClient)
		{
			bool ret = false;

			if (request.Params.Count > 0)
			{
				Hashtable requestParam = (Hashtable)request.Params[0];
				if (requestParam.Contains("bankerID") &&
					requestParam.Contains("bankerSessionID") &&
					requestParam.Contains("bankerSecureSessionID"))
				{
					UUID bankerID = UUID.Zero;
					UUID.TryParse((string)requestParam["bankerID"], out bankerID);
					if (bankerID != UUID.Zero)
					{
						IClientAPI client = LocateClientObject(bankerID);
						if (client != null &&
							client.SessionId.ToString() == (string)requestParam["bankerSessionID"] &&
							client.SecureSessionId.ToString() == (string)requestParam["bankerSecureSessionID"])
						{
							if (requestParam.Contains("amount"))
							{
								Scene scene = (Scene)client.Scene;
								int amount  = (int)requestParam["amount"];
								ret = AddBankerMoney(bankerID, amount, scene.RegionInfo.RegionHandle);
							}
						}
					}
				}
			}

			// Send the response to caller.
			XmlRpcResponse resp  = new XmlRpcResponse();
			Hashtable paramTable = new Hashtable();
			paramTable["success"] = ret;
			if (!ret) 
			{
				m_log.ErrorFormat("[MONEY]: Add Banker Money transaction is failed.");
			}
			resp.Value = paramTable;

			return resp;
		}



		// for SendMoneyBalance RPC
		public XmlRpcResponse SendMoneyBalanceHandler(XmlRpcRequest request, IPEndPoint remoteClient)
		{
			bool ret = false;

			if (request.Params.Count > 0)
			{
				Hashtable requestParam = (Hashtable)request.Params[0];
				if (requestParam.Contains("avatarID") &&
					requestParam.Contains("secretCode"))
				{
					UUID avatarID = UUID.Zero;
					UUID.TryParse((string)requestParam["avatarID"], out avatarID);
					if (avatarID != UUID.Zero)
					{
						if (requestParam.Contains("amount"))
						{
							int amount  = (int)requestParam["amount"];
							string secretCode = (string)requestParam["secretCode"];
							string scriptIP   = remoteClient.Address.ToString();

							MD5 md5 = MD5.Create();
							byte[] code = md5.ComputeHash(ASCIIEncoding.Default.GetBytes(secretCode + "_" + scriptIP));
							string hash = BitConverter.ToString(code).ToLower().Replace("-","");
							//m_log.ErrorFormat("[MONEY]: SecretCode: {0} + {1} = {2}", secretCode, scriptIP, hash);
							ret = SendMoneyBalance(avatarID, amount, hash);
						}
					}
				}
			}

			// Send the response to caller.
			XmlRpcResponse resp  = new XmlRpcResponse();
			Hashtable paramTable = new Hashtable();
			paramTable["success"] = ret;
			if (!ret) 
			{
				m_log.ErrorFormat("[MONEY]: Send Money transaction is failed.");
			}
			resp.Value = paramTable;

			return resp;
		}



		// for GetBalance RPC
		public XmlRpcResponse GetBalanceHandler(XmlRpcRequest request, IPEndPoint remoteClient)
		{
			bool ret = false;
			int  balance = -1;

			if (request.Params.Count > 0)
			{
				Hashtable requestParam = (Hashtable)request.Params[0];
				if (requestParam.Contains("clientID") &&
					requestParam.Contains("clientSessionID") &&
					requestParam.Contains("clientSecureSessionID"))
				{
					UUID clientID = UUID.Zero;
					UUID.TryParse((string)requestParam["clientID"], out clientID);
					if (clientID != UUID.Zero)
					{
						IClientAPI client = LocateClientObject(clientID);
						if (client != null &&
							client.SessionId.ToString() == (string)requestParam["clientSessionID"] &&
							client.SecureSessionId.ToString() == (string)requestParam["clientSecureSessionID"])
						{
							balance = QueryBalanceFromMoneyServer(client);
						}
					}
				}
			}

			// Send the response to caller.
			if (balance<0) 
			{
				m_log.ErrorFormat("[MONEY]: GetBlance transaction is failed.");
				ret = false;
			}

			XmlRpcResponse resp  = new XmlRpcResponse();
			Hashtable paramTable = new Hashtable();
			paramTable["success"] = ret;
			paramTable["balance"] = balance;
			resp.Value = paramTable;

			return resp;
		}


		#endregion



		#region MoneyModule private help functions

		/// <summary>   
		/// Transfer the money from one user to another. Need to notify money server to update.   
		/// </summary>   
		/// <param name="amount">   
		/// The amount of money.   
		/// </param>   
		/// <returns>   
		/// return true, if successfully.   
		/// </returns>   
		private bool TransferMoney(UUID sender, UUID receiver, int amount, int transactiontype, UUID objectID, ulong regionHandle, string description)
		{
			bool ret = false;
			IClientAPI senderClient = LocateClientObject(sender);

			// Handle the illegal transaction.   
			if (senderClient==null) // receiverClient could be null.
			{
				m_log.ErrorFormat("[MONEY]: Client {0} not found", sender.ToString());
				return false;
			}

			if (QueryBalanceFromMoneyServer(senderClient)<amount)
			{
				m_log.ErrorFormat("[MONEY]: No insufficient balance in client [{0}].", sender.ToString());
				return false;
			}

			#region Send transaction request to money server and parse the resultes.

			if (!string.IsNullOrEmpty(m_moneyServURL))
			{
				// Fill parameters for money transfer XML-RPC.   
				Hashtable paramTable = new Hashtable();
				paramTable["senderUserServIP"] = m_userServIP;
				paramTable["senderID"] = sender.ToString();
				paramTable["receiverUserServIP"] = m_userServIP;
				paramTable["receiverID"] = receiver.ToString();
				paramTable["senderSessionID"] = senderClient.SessionId.ToString();
				paramTable["senderSecureSessionID"] = senderClient.SecureSessionId.ToString();
				paramTable["transactionType"] = transactiontype;
				paramTable["objectID"] = objectID.ToString();
				paramTable["regionHandle"] = regionHandle.ToString();
				paramTable["amount"] = amount;
				paramTable["description"] = description;

				// Generate the request for transfer.   
				Hashtable resultTable = genericCurrencyXMLRPCRequest(paramTable, "TransferMoney");

				// Handle the return values from Money Server.  
				if (resultTable != null && resultTable.Contains("success"))
				{
					if ((bool)resultTable["success"] == true)
					{
						m_log.DebugFormat("[MONEY]: Money transfer from [{0}] to [{1}] is done.", sender.ToString(), receiver.ToString());
						ret = true;
					}
				}
				else
				{
					m_log.ErrorFormat("[MONEY]: Can not money transfer request from [{0}] to [{1}].", sender.ToString(), receiver.ToString());
				}
			}
			else // Money server is not available.
			{
				m_log.ErrorFormat("[MONEY]: Money Server is not available!!");
			}

			#endregion

			return ret;
		}



		/// <summary>   
		/// Force transfer the money from one user to another. 
		/// This function does not check sender login.
		/// Need to notify money server to update.   
		/// </summary>   
		/// <param name="amount">   
		/// The amount of money.   
		/// </param>   
		/// <returns>   
		/// return true, if successfully.   
		/// </returns>   
		private bool ForceTransferMoney(UUID sender, UUID receiver, int amount, int transactiontype, UUID objectID, ulong regionHandle, string description)
		{
			bool ret = false;

			#region Force send transaction request to money server and parse the resultes.

			if (!string.IsNullOrEmpty(m_moneyServURL))
			{
				// Fill parameters for money transfer XML-RPC.   
				Hashtable paramTable = new Hashtable();
				paramTable["senderUserServIP"] = m_userServIP;
				paramTable["senderID"] = sender.ToString();
				paramTable["receiverUserServIP"] = m_userServIP;
				paramTable["receiverID"] = receiver.ToString();
				paramTable["transactionType"] = transactiontype;
				paramTable["objectID"] = objectID.ToString();
				paramTable["regionHandle"] = regionHandle.ToString();
				paramTable["amount"] = amount;
				paramTable["description"] = description;

				// Generate the request for transfer.   
				Hashtable resultTable = genericCurrencyXMLRPCRequest(paramTable, "ForceTransferMoney");

				// Handle the return values from Money Server.  
				if (resultTable != null && resultTable.Contains("success"))
				{
					if ((bool)resultTable["success"] == true)
					{
						m_log.DebugFormat("[MONEY]: Money force transfer from [{0}] to [{1}] is done.", sender.ToString(), receiver.ToString());
						ret = true;
					}
				}
				else
				{
					m_log.ErrorFormat("[MONEY]: Can not money force transfer request from [{0}] to [{1}].", sender.ToString(), receiver.ToString());
				}
			}
			else // Money server is not available.
			{
				m_log.ErrorFormat("[MONEY]: Money Server is not available!!");
			}

			#endregion

			return ret;
		}



		/// <summary>   
		/// Add the money to banker avatar. Need to notify money server to update.   
		/// </summary>   
		/// <param name="amount">   
		/// The amount of money.  
		/// </param>   
		/// <returns>   
		/// return true, if successfully.   
		/// </returns>   
		private bool AddBankerMoney(UUID bankerID, int amount, ulong regionHandle)
		{
			bool ret = false;

			if (!string.IsNullOrEmpty(m_moneyServURL))
			{
				// Fill parameters for money transfer XML-RPC.   
				Hashtable paramTable = new Hashtable();
				paramTable["bankerUserServIP"] = m_userServIP;
				paramTable["bankerID"] = bankerID.ToString();
				paramTable["transactionType"] = 5010;	// BuyMoney
				paramTable["amount"] = amount;
				paramTable["regionHandle"] = regionHandle.ToString();;
				paramTable["description"] = "Add Money to Avatar";

				// Generate the request for transfer.   
				Hashtable resultTable = genericCurrencyXMLRPCRequest(paramTable, "AddBankerMoney");

				// Handle the return values from Money Server.  
				if (resultTable != null && resultTable.Contains("success"))
				{
					if ((bool)resultTable["success"] == true)
					{
						m_log.DebugFormat("[MONEY]: Add the money to banker [{0}] is done.", bankerID.ToString());
						ret = true;
					}
					else
					{
						m_log.ErrorFormat("[MONEY]: Fail Message: {0}", resultTable["message"]);
					}
				}
				else
				{
					m_log.ErrorFormat("[MONEY]: Money Server is not responce.");
				}
			}
			else 
			{
				m_log.ErrorFormat("[MONEY]: AddBankerMoney: Money Server is not available!!");
			}

			return ret;
		}



		/// <summary>   
		/// Send the money to avatar. Need to notify money server to update.   
		/// </summary>   
		/// <param name="amount">   
		/// The amount of money.  
		/// </param>   
		/// <returns>   
		/// return true, if successfully.   
		/// </returns>   
		private bool SendMoneyBalance(UUID avatarID, int amount, string secretCode)
		{
			bool ret = false;

			if (!string.IsNullOrEmpty(m_moneyServURL))
			{
				// Fill parameters for money transfer XML-RPC.   
				Hashtable paramTable = new Hashtable();
				paramTable["avatarUserServIP"] = m_userServIP;
				paramTable["avatarID"] = avatarID.ToString();
				paramTable["transactionType"] = 5003;	// ReferBonus
				paramTable["amount"] = amount;
				paramTable["secretCode"] = secretCode;
				paramTable["description"] = "Bonus to Avatar";

				// Generate the request for transfer.   
				Hashtable resultTable = genericCurrencyXMLRPCRequest(paramTable, "SendMoneyBalance");

				// Handle the return values from Money Server.  
				if (resultTable != null && resultTable.Contains("success"))
				{
					if ((bool)resultTable["success"] == true)
					{
						m_log.DebugFormat("[MONEY]: Send the money to avatar [{0}] is done.", avatarID.ToString());
						ret = true;
					}
					else 
					{
						m_log.ErrorFormat("[MONEY]: Fail Message: {0}", resultTable["message"]);
					}
				}
				else
				{
					m_log.ErrorFormat("[MONEY]: Money Server is not responce.");
				}
			}
			else 
			{
				m_log.ErrorFormat("[MONEY]: SendMoneyBalance: Money Server is not available!!");
			}

			return ret;
		}



		/// <summary>   
		/// Pay the money of charge.
		/// </summary>   
		/// <param name="amount">   
		/// The amount of money.   
		/// </param>   
		/// <returns>   
		/// return true, if successfully.   
		/// </returns>   
		private bool PayMoneyCharge(UUID sender, int amount, int transactiontype, ulong regionHandle, string description)
		{
			bool ret = false;
			IClientAPI senderClient = LocateClientObject(sender);

			// Handle the illegal transaction.   
			if (senderClient==null) // receiverClient could be null.
			{
				m_log.ErrorFormat("[MONEY]: Client {0} not found", sender.ToString());
				return false;
			}

			if (QueryBalanceFromMoneyServer(senderClient)<amount)
			{
				m_log.ErrorFormat("[MONEY]: No insufficient balance in client [{0}].", sender.ToString());
				return false;
			}

			#region Send transaction request to money server and parse the resultes.

			if (!string.IsNullOrEmpty(m_moneyServURL))
			{
				// Fill parameters for money transfer XML-RPC.   
				Hashtable paramTable = new Hashtable();
				paramTable["senderID"] = sender.ToString();
				paramTable["senderSessionID"] = senderClient.SessionId.ToString();
				paramTable["senderSecureSessionID"] = senderClient.SecureSessionId.ToString();
				paramTable["transactionType"] = transactiontype;
				paramTable["amount"] = amount;
				paramTable["regionHandle"] = regionHandle.ToString();
				paramTable["description"] = description;
				paramTable["senderUserServIP"] = m_userServIP;

				// Generate the request for transfer.   
				Hashtable resultTable = genericCurrencyXMLRPCRequest(paramTable, "PayMoneyCharge");

				// Handle the return values from Money Server.  
				if (resultTable != null && resultTable.Contains("success"))
				{
					if ((bool)resultTable["success"] == true)
					{
						m_log.DebugFormat("[MONEY]: Pay money of charge from [{0}] is done.", sender.ToString());
						ret = true;
					}
				}
				else
				{
					m_log.ErrorFormat("[MONEY]: Can not pay money of charge request from [{0}].", sender.ToString());
				}
			}
			else // Money server is not available.
			{
				m_log.ErrorFormat("[MONEY]: Money Server is not available!!");
			}

			#endregion

			return ret;
		}



		/// <summary>   
		/// Login the money server when the new client login.
		/// </summary>   
		/// <param name="userID">   
		/// Indicate user ID of the new client.   
		/// </param>   
		/// <returns>   
		/// return true, if successfully.   
		/// </returns>   
		private bool LoginMoneyServer(IClientAPI client, out int balance)
		{
			bool ret = false;
			balance = 0;

			#region Send money server the client info for login.

			Scene scene = (Scene)client.Scene;
			string userName = string.Empty;

			if (!string.IsNullOrEmpty(m_moneyServURL))
			{
				// Get the username for the login user.
				if (client.Scene is Scene)
				{
					if (scene != null)
					{
						UserAccount account = scene.UserAccountService.GetUserAccount(scene.RegionInfo.ScopeID, client.AgentId);
						if (account != null)
						{
							userName = account.FirstName + " " + account.LastName;
						}

						//CachedUserInfo profile = scene.CommsManager.UserProfileCacheService.GetUserDetails(client.AgentId);
						//if (profile != null && profile.UserProfile != null)
						//{
						//	userName = profile.UserProfile.FirstName + " " + profile.UserProfile.SurName;
						//}
					}
				}

				// Login the Money Server.   
				Hashtable paramTable = new Hashtable();
				paramTable["userServIP"] = m_userServIP;
				paramTable["openSimServIP"] = scene.RegionInfo.ServerURI.Replace(scene.RegionInfo.InternalEndPoint.Port.ToString(), 
																				 scene.RegionInfo.HttpPort.ToString());
				paramTable["userName"] = userName;
				paramTable["clientUUID"] = client.AgentId.ToString();
				paramTable["clientSessionID"] = client.SessionId.ToString();
				paramTable["clientSecureSessionID"] = client.SecureSessionId.ToString();

				// Generate the request for transfer.   
				Hashtable resultTable = genericCurrencyXMLRPCRequest(paramTable, "ClientLogin");

				// Handle the return result 
				if (resultTable != null && resultTable.Contains("success"))
				{
					if ((bool)resultTable["success"] == true)
					{
						balance = (int)resultTable["clientBalance"];
						m_log.InfoFormat("[MONEY]: Client [{0}] login Money Server {1}.", client.AgentId.ToString(), m_moneyServURL);
						ret = true;
					}
				}
				else
				{
					m_log.ErrorFormat("[MONEY]: Unable to login Money Server {0} for client [{1}].", m_moneyServURL, client.AgentId.ToString());
				}
			}
			else
			{
				m_log.ErrorFormat("[MONEY]: Money Server is not available!!");
			}

			#endregion

			return ret;
		}



		/// <summary>   
		/// Log off from the money server.   
		/// </summary>   
		/// <param name="userID">   
		/// Indicate user ID of the new client.   
		/// </param>   
		/// <returns>   
		/// return true, if successfully.   
		/// </returns>   
		private bool LogoffMoneyServer(IClientAPI client)
		{
			bool ret = false;

			if (!string.IsNullOrEmpty(m_moneyServURL))
			{
				// Log off from the Money Server.   
				Hashtable paramTable = new Hashtable();
				paramTable["userServIP"] = m_userServIP;
				paramTable["clientUUID"] = client.AgentId.ToString();
				paramTable["clientSessionID"] = client.SessionId.ToString();
				paramTable["clientSecureSessionID"] = client.SecureSessionId.ToString();

				// Generate the request for transfer.   
				Hashtable resultTable = genericCurrencyXMLRPCRequest(paramTable, "ClientLogout");
				// Handle the return result
				if (resultTable != null && resultTable.Contains("success"))
				{
					if ((bool)resultTable["success"] == true)
					{
						ret = true;
					}
				}
			}

			return ret;
		}



		/// <summary>   
		/// Generic XMLRPC client abstraction   
		/// </summary>   
		/// <param name="ReqParams">Hashtable containing parameters to the method</param>   
		/// <param name="method">Method to invoke</param>   
		/// <returns>Hashtable with success=>bool and other values</returns>   
		private Hashtable genericCurrencyXMLRPCRequest(Hashtable reqParams, string method)
		{
			// Handle the error in parameter list.   
			if (reqParams.Count <= 0 || string.IsNullOrEmpty(method) || string.IsNullOrEmpty(m_moneyServURL))
			{
				return null;
			}

			ArrayList arrayParams = new ArrayList();
			arrayParams.Add(reqParams);
			XmlRpcResponse moneyServResp = null;
			try
			{
				//XmlRpcRequest moneyModuleReq = new XmlRpcRequest(method, arrayParams);
				//moneyServResp = moneyModuleReq.Send(m_moneyServURL, MONEYMODULE_REQUEST_TIMEOUT);
				NSLXmlRpcRequest moneyModuleReq = new NSLXmlRpcRequest(method, arrayParams);
				moneyServResp = moneyModuleReq.xSend(m_moneyServURL, MONEYMODULE_REQUEST_TIMEOUT);
			}
			catch (Exception ex)
			{
				m_log.ErrorFormat( "[MONEY]: Unable to connect to Money Server {0}.  Exception {1}", m_moneyServURL, ex);

				Hashtable ErrorHash = new Hashtable();
				ErrorHash["success"] = false;
				ErrorHash["errorMessage"] = "Unable to manage your money at this time. Purchases may be unavailable";
				ErrorHash["errorURI"] = "";

				return ErrorHash;
			}

			if (moneyServResp.IsFault)
			{
				Hashtable ErrorHash = new Hashtable();
				ErrorHash["success"] = false;
				ErrorHash["errorMessage"] = "Unable to manage your money at this time. Purchases may be unavailable";
				ErrorHash["errorURI"] = "";

				return ErrorHash;
			}
			Hashtable moneyRespData = (Hashtable)moneyServResp.Value;

			return moneyRespData;
		}



		/// <summary>   
		/// Locates a IClientAPI for the client specified   
		/// </summary>   
		/// <param name="AgentID"></param>   
		/// <returns></returns>   
		private IClientAPI LocateClientObject(UUID AgentID)
		{
			ScenePresence tPresence = null;
			IClientAPI rclient = null;

			lock (m_sceneList)
			{
				foreach (Scene _scene in m_sceneList.Values)
				{
					tPresence = _scene.GetScenePresence(AgentID);
					if (tPresence != null)
					{
						if (!tPresence.IsChildAgent)
						{
							rclient = tPresence.ControllingClient;
						}
					}
					if (rclient != null)
					{
						return rclient;
					}
				}
			}

			return null;
		}



		private Scene LocateSceneClientIn(UUID AgentId)
		{
			lock (m_sceneList)
			{
				foreach (Scene _scene in m_sceneList.Values)
				{
					ScenePresence tPresence = _scene.GetScenePresence(AgentId);
					if (tPresence != null)
					{
						if (!tPresence.IsChildAgent)
						{
							return _scene;
						}
					}
				}
			}

			return null;
		}



		private SceneObjectPart FindPrim(UUID objectID)
		{
			lock (m_sceneList)
			{
				foreach (Scene scene in m_sceneList.Values)
				{
					SceneObjectPart part = scene.GetSceneObjectPart(objectID);
					if (part != null)
					{
						return part;
					}
				}
			}

			return null;
		}



		private int QueryBalanceFromMoneyServer(IClientAPI client)
		{
			int ret = -1;

			#region Send the request to get the balance from money server for cilent.

			if (client != null)
			{
				if (!string.IsNullOrEmpty(m_moneyServURL))
				{
					Hashtable paramTable = new Hashtable();
					paramTable["userServIP"] = m_userServIP;
					paramTable["clientUUID"] = client.AgentId.ToString();
					paramTable["clientSessionID"] = client.SessionId.ToString();
					paramTable["clientSecureSessionID"] = client.SecureSessionId.ToString();

					// Generate the request for transfer.   
					Hashtable resultTable = genericCurrencyXMLRPCRequest(paramTable, "GetBalance");

					// Handle the return result
					if (resultTable != null && resultTable.Contains("success"))
					{
						if ((bool)resultTable["success"] == true)
						{
							ret = (int)resultTable["clientBalance"];
						}
					}
				}
				else
				{
					if (m_moneyServer.ContainsKey(client.AgentId))
					{
						ret = m_moneyServer[client.AgentId];
					}
				}

				if (ret < 0)
				{
					m_log.ErrorFormat("[MONEY]: Unable to query balance from Money Server {0} for client [{1}].", 
																					m_moneyServURL, client.AgentId.ToString());
				}
			}

			#endregion

			return ret;
		}



		///
		public void OnEconomyDataRequest(UUID agentId)
		{
			IClientAPI user = LocateClientObject(agentId);

			if (user != null)
			{
				Scene s = LocateSceneClientIn(user.AgentId);

				user.SendEconomyData(EnergyEfficiency, s.RegionInfo.ObjectCapacity, ObjectCount, PriceEnergyUnit, PriceGroupCreate,
									 PriceObjectClaim, PriceObjectRent, PriceObjectScaleFactor, PriceParcelClaim, PriceParcelClaimFactor,
									 PriceParcelRent, PricePublicObjectDecay, PricePublicObjectDelete, PriceRentLight, PriceUpload,
									 TeleportMinPrice, TeleportPriceExponent);
			}
		}



		/// <summary>   
		/// Sends the the stored money balance to the client   
		/// </summary>   
		/// <param name="client"></param>   
		/// <param name="agentID"></param>   
		/// <param name="SessionID"></param>   
		/// <param name="TransactionID"></param>   
		private void OnMoneyBalanceRequest(IClientAPI client, UUID agentID, UUID SessionID, UUID TransactionID)
		{
			if (client.AgentId == agentID && client.SessionId == SessionID)
			{
				int balance = -1;
				if (!string.IsNullOrEmpty(m_moneyServURL))
				{
					balance = QueryBalanceFromMoneyServer(client);
				}
				//else if (m_moneyServer.ContainsKey(agentID))
				//{
				//	balance = m_moneyServer[agentID];
				//}

				if (balance < 0)
				{
					client.SendAlertMessage("Fail to query the balance.");
				}
				else
				{
					client.SendMoneyBalance(TransactionID, true, new byte[0], balance);
				}
			}
			else
			{
				client.SendAlertMessage("Unable to send your money balance.");
			}
		}



		private void OnRequestPayPrice(IClientAPI client, UUID objectID)
		{
			Scene scene = LocateSceneClientIn(client.AgentId);
			if (scene == null) return;
			SceneObjectPart sceneObj = scene.GetSceneObjectPart(objectID);
			if (sceneObj == null) return;
			SceneObjectGroup group = sceneObj.ParentGroup;
			SceneObjectPart root = group.RootPart;

			client.SendPayPrice(objectID, root.PayPrice);
		}

		#endregion

		/* Private **************************************************************/

	}
}
