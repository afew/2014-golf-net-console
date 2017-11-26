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
		protected List<PGN.TcpCln>	m_vCln	= new List<PGN.TcpCln>();			// client list
		protected Thread			m_thAcp = null;								// accept thread


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

			m_scH.Bind(m_sdH);										// Binding
			m_scH.Listen(1);										// Listen

			m_thAcp = new Thread(new ThreadStart(WorkAcp));			// create accept thread
			m_thAcp.Start();

			return NTC.OK;
		}


		public void CloseSocket()
		{
			if(null == m_scH)
				return;

			lock (m_oLock)
			{
				m_thAcp.Abort();
				m_thAcp = null;

				//m_scH.Shutdown(SocketShutdown.Both);
				m_scH.Close();
				m_scH = null;

				m_sIp = "";
				m_sPt = 0;
			}
		}


		protected int AddNewClient(Socket scH)
		{
			if(NTC.MAX_CONNECT <= m_vCln.Count)
				return NTC.EFAIL;

			EndPoint	sdH   = scH.RemoteEndPoint;
			int			netId = PGN.Util.GetSocketId(ref scH);
			TcpCln		pCln  = new TcpCln(netId);

			pCln.Create(this, scH, sdH);

			//string guid = Guid.NewGuid().ToString().ToUpper();

			string crpKey = netId.ToString() + " PGN_ENCRYPTION_KEY_BYTE_STRING";

			System.Console.WriteLine("Net Client: " + crpKey);

			pCln.Send(crpKey, NTC.OP_DEFAULT);

			m_vCln.Add(pCln);
			pCln.Recv();

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

		protected void WorkAcp()
		{
			try
			{
				while( true)
				{
					Socket scH = null;

					scH = m_scH.Accept();

					Console.WriteLine("IoAcpt::New Client::" + scH);

					lock (m_oLock)
					{
						int hr = AddNewClient(scH);
						if (0 > hr)
							Console.WriteLine("IoAcpt::Client List is Full");
					}
				}
			}

			catch (SocketException)
			{
				Console.WriteLine("WorkAcp::SocketException");
			}
			catch (Exception)
			{
				Console.WriteLine("WorkAcp::Exception");
			}
		}
	}

}// namespace PGN

