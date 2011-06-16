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

//using System.Collections.Generic;
//using System.Text;
//using System.Reflection;
//using System.Net;

//using log4net;
//using Nini.Config;
//using Nwc.XmlRpc;
//using OpenMetaverse;

//using OpenSim.Framework;
//using OpenSim.Framework.Servers.HttpServer;
//using OpenSim.Services.Interfaces;
//using OpenSim.Services.UserAccountService;

//using OpenSim.Region.Framework.Interfaces;
//using OpenSim.Region.Framework.Scenes;
//using OpenSim.Region.Framework;

//using OpenSim.Region.CoreModules.Scripting.HttpRequest;


using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Net;
using System.Text;
using System.Reflection;

using log4net;
using Nwc.XmlRpc;


namespace NSL.XmlRpc 
{

	public class NSLXmlRpcRequest : XmlRpcRequest
	{
		//private String _methodName = null;
		private Encoding _encoding = new ASCIIEncoding();
		private XmlRpcRequestSerializer _serializer = new XmlRpcRequestSerializer();
		private XmlRpcResponseDeserializer _deserializer = new XmlRpcResponseDeserializer();


		public NSLXmlRpcRequest()
      	{
      		_params = new ArrayList();
      	}


		public NSLXmlRpcRequest(String methodName, IList parameters)
		{
			MethodName = methodName;
			_params = parameters;
		}



		public XmlRpcResponse xSend(String url, Int32 timeout)
	  	{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

			if (request == null) {
				throw new XmlRpcException(XmlRpcErrorCodes.TRANSPORT_ERROR, 
						XmlRpcErrorCodes.TRANSPORT_ERROR_MSG +": Could not create request with " + url);
			}
			request.Method = "POST";
			request.ContentType = "text/xml";
			request.AllowWriteStreamBuffering = true;

			request.Timeout = timeout;
			request.Headers.Add("NoVerifyCert", "true");

			Stream stream = request.GetRequestStream();
			XmlTextWriter xml = new XmlTextWriter(stream, _encoding);
			_serializer.Serialize(xml, this);
			xml.Flush();
			xml.Close();

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			StreamReader input = new StreamReader(response.GetResponseStream());

			XmlRpcResponse resp = (XmlRpcResponse)_deserializer.Deserialize(input);
			input.Close();
			response.Close();
			return resp;
	  	}

	}

}
