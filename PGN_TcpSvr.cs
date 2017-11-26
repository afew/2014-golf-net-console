//
//
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace PGN
{
	class TcpSvr : PGN.TcpBase
	{


		List<PGN.TcpCln>					m_vCln	= new List<PGN.TcpCln>();


		override public void Destroy()
		{
			CloseSocket();
		}

		override public int Query(string msg, object v)
		{
			if("Remove Client" == msg)
			{
				TcpBase	p = (TcpBase)v;
				RemoveClient(p);
			}


			return NTC.EFAIL;
		}


		public int Create(string ip, int pt)
		{
			bool		hr			= false;
			IPAddress	ipAdd		= null;


			m_sIp = ip;
			m_sPt = pt;

			if(null == ip)
			{
				IPAddress[]	vAdd	= Dns.GetHostEntry(Environment.MachineName)
										.AddressList;

				ipAdd				= vAdd[vAdd.Length - 1];
			}
			else
			{
				ipAdd				= IPAddress.Parse(m_sIp);
			}


			m_sdH					= new IPEndPoint(ipAdd, m_sPt);
			m_scH					= new Socket( AddressFamily.InterNetwork
												, SocketType.Stream
												, ProtocolType.Tcp);

			m_scH.Bind(m_sdH);		// Binding
			m_scH.Listen(1);		// try to asynchronous connect server

			m_arAcp					= new SocketAsyncEventArgs();
			m_arAcp.Completed		+= new EventHandler<SocketAsyncEventArgs>(WorkAcp);
			hr = m_scH.AcceptAsync(m_arAcp);


			return (false == hr)? NTC.EFAIL : NTC.OK;
		}


		public void CloseSocket()
		{
			if(null == m_scH)
				return;


			lock(m_oLock)
			{
				m_arAcp	= null;

				m_scH.Shutdown(SocketShutdown.Both);
				m_scH.Close();
				m_scH = null;

				m_sIp = "";
				m_sPt = 0;
			}
		}


		protected int AddNewClient(SocketAsyncEventArgs accpt)
		{
			if(NTC.MAX_CONNECT <= m_vCln.Count)
				return NTC.EFAIL;

			Socket		scH = accpt.AcceptSocket;
			EndPoint	sdH = accpt.RemoteEndPoint;

			int			netId = PGN.Util.GetSocketId(ref scH);
			TcpCln		pCln = new TcpCln(netId);


			pCln.Create(this, scH, sdH);

			//string guid = Guid.NewGuid().ToString().ToUpper();


			string crpKey = netId.ToString() + " DFNET_ENCRYPTION_KEY_BYTE_STRING";

			System.Console.WriteLine("Net Client: " + crpKey);

			pCln.Send(crpKey, NTC.OP_DEFAULT);

			m_vCln.Add(pCln);
			return NTC.OK;
		}

		protected void RemoveClient(TcpBase v)
		{
			int n = m_vCln.FindIndex(_cln => _cln.GetSocket() == v.GetSocket());
			if(0 > n)
				return;


			int key = m_vCln[n].NetId;

			m_vCln[n].Destroy();
			m_vCln.RemoveAt(n);

			System.Console.Write("Remove client[" + n +"]: " + key);
			System.Console.WriteLine(", Remain Client :" + m_vCln.Count);
		}


		////////////////////////////////////////////////////////////////////////////
		// Inner Process...

		protected void WorkAcp(object sender, SocketAsyncEventArgs args)
		{
			int hr = 0;

			try
			{
				SocketError				err = args.SocketError;
				int						rcn = args.BytesTransferred;
				byte[]					rcb = args.Buffer;
				SocketAsyncOperation	opc = args.LastOperation;

				if(SocketAsyncOperation.Accept == opc)
				{
					if(err != SocketError.Success)
					{
						Console.WriteLine("WorkAcp::Cann't Accept Client");
						CloseSocket();

						return;
					}

					Socket scH = args.AcceptSocket;

		            if(false == scH.Connected)
					{
						Console.WriteLine("WorkAcp::Cann't Connect to Client");
					}
					else
					{
						Console.WriteLine("WorkAcp::New Client::" + scH);

						lock(m_oLock)
						{
							hr = AddNewClient(args);
							if(0 > hr)
								Console.WriteLine("WorkAcp::Client List is Full");
						}
					}

					lock(m_oLock)
					{
						args.AcceptSocket = null;
						m_scH.AcceptAsync(m_arAcp);
					}
				}
			}
			catch(SocketException)
			{
				Console.WriteLine("IoComplete::SocketException");
			}
			catch(Exception)
			{
				Console.WriteLine("IoComplete::Exception");
			}
		}

	}

}// namespace PGN

