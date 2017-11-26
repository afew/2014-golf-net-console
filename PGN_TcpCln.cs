// Packet, util, tcp, udb base class.
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
	class TcpCln : PGN.TcpBase
	{
		// for controll
		protected	byte[]			m_rcvS	= new byte[NTC.PCK_MAX];			// receive buffer for server
		protected	byte[]			m_rcvD	= new byte[NTC.PCK_MAX];			// receive buffer for decription
		protected	int				m_rcvN	= -1;								// current received count

		protected	List<byte[]>	m_sndB	= new List<byte[]>();				// send queue buffer
		protected	int				m_sndC	= NTC.OK;							// sending condition complete?
		protected	int				m_sndN	= -1;								// current send index

		protected	uint			m_nSqc	= 0;								// packet sequence

		protected	PGN.Packet		m_sPck	= new PGN.Packet();					// for string message
		protected	byte[]			m_vCrp	= new byte[NTC.PCK_KEY];			// key for crypto


		override public void Destroy()
		{
			CloseSocket();

			m_sndC	= NTC.OK;
			m_sndN	= -1;

			for(int n=0; n<m_sndB.Count; ++n)
				Array.Clear(m_sndB[n], 0, m_sndB[n].Length);

			m_sndB.Clear();
		}

		override public int Query(string msg, object v)
		{
			return NTC.EFAIL;
		}


		public TcpCln() : this(-1){}

		public TcpCln(int nId)
		{
			m_aId = nId;

			for(int n=0; n<NTC.PCK_LIST; ++n)
			{
				byte[]	buf = new byte[NTC.PCK_MAX];

				Array.Clear(buf, 0, buf.Length);
				m_sndB.Add(buf);
			}

			Array.Clear(m_rcvS, 0, m_rcvS.Length);
			Array.Clear(m_rcvD, 0, m_rcvD.Length);

			Array.Clear(m_vCrp, 0, m_vCrp.Length);
			m_sPck.Reset();
		}


		public int Create(TcpBase parent, string ip, int pt)
		{
			IPAddress ipAdd		= null;
			
			m_pPrn	= parent;
			m_sIp	= ip;
			m_sPt	= pt;			//System.Convert.ToInt32(pt);

			if(null == m_sIp)
			{
				IPAddress[]	vAdd
				= Dns.GetHostEntry(Environment.MachineName).AddressList;

				ipAdd = vAdd[vAdd.Length - 1];
			}
			else
			{
				ipAdd = IPAddress.Parse(m_sIp);
			}

			m_sdH	= new IPEndPoint(ipAdd, m_sPt);
			m_scH	= new Socket( AddressFamily.InterNetwork
								, SocketType.Stream
								, ProtocolType.Tcp);

			return NTC.OK;
		}

		public int Create(TcpBase parent, Socket scH, EndPoint sdH)
		{
			m_pPrn	= parent;
			m_scH	= scH;
			m_sdH	= sdH;

			return NTC.OK;
		}

		public int Connect()
		{
			m_scH.BeginConnect(m_sdH, IoConnect, this);
			return NTC.OK;
		}

		public void CloseSocket()
		{
			if(null == m_scH)
				return;


			lock(m_oLock)
			{
				m_scH.Shutdown(SocketShutdown.Both);
				m_scH.Close();
				m_scH = null;
				m_sdH = null;

				m_sIp = "";
				m_sPt = 0;
			}

			if(null != m_pPrn)
			{
				TcpBase p = (TcpBase)m_pPrn;
				p.Query("Remove Client", this);
			}
		}

		////////////////////////////////////////////////////////////////////////////
		// Inner Process...

		protected void IoConnect(IAsyncResult iar)
		{
			TcpCln	pCln = (TcpCln)iar.AsyncState;

			try
			{
				pCln.m_scH.EndConnect(iar);
			}
			catch(SocketException)
			{
				Console.WriteLine("Err::IoConnect::socket");
				return;
			}
			catch(Exception)
			{
				Console.WriteLine("Err::IoConnect::exception");
				return;
			}

			if(pCln != this)
			{
				Console.WriteLine("Err::IoConnect::Different object");
				return;
			}

			pCln.Recv();
		}

		protected void IoRecv(IAsyncResult iar)
		{
			TcpCln	pCln = (TcpCln)iar.AsyncState;
			int		rcn = -1;

			try
			{
				rcn = pCln.m_scH.EndReceive(iar);
			}
			catch(SocketException)
			{
				Console.WriteLine("Err::IoRecv::socket");
				return;
			}
			catch(Exception)
			{
				Console.WriteLine("Err::IoRecv::exception");
				return;
			}

			if(pCln != this)
			{
				Console.WriteLine("Err::IoRecv::Different object");
				return;
			}

			// closed socket
			if(-1 == rcn)
			{
				Console.WriteLine("IoRecv::disconnected");
				CloseSocket();
				return;
			}


			lock(m_oLock)
			{
				// DeCryption
				int		lenD = 0;
				PGN.Util.DeCrypt(ref m_rcvD, ref lenD, m_rcvS, rcn);

				// DeCoding
				PGN.Packet pck = new PGN.Packet(ref m_rcvD, lenD);

				// data buffer and length
				byte[]	b = pck.DataBuf;
				int		l = pck.DataLen;


				// read the key and id from string
				if(0 > m_aId)
				{
					int		n = 0;
					int		cnt = 0;

					n   =  Array.FindIndex(b, 0, _s => _s == (byte)' ');
					cnt = l - (n+1);

					string	s_uid = Encoding.Default
								.GetString(b, 0, n)
								.Trim();

					m_aId		= System.Convert.ToInt32(s_uid);


					string	s_key = null;
					Array.Copy(b, n+1, m_vCrp, 0, cnt);
					s_key = Encoding.Default.GetString(m_vCrp, 0, cnt).Trim();

					Console.WriteLine("Auth Uid::{0} , Key:{1}", m_aId, s_key);
				}

				// the transformed stream exists
				else
				{
					string	s_chat = Encoding.Default
									.GetString(b, 0, l).Trim();

					Console.WriteLine(s_chat);

					//int op = PGN.NTC.OP_CHAT;
					//this.Send("server send::" + s_chat, op);
				}

				m_rcvN = 1;

				Array.Clear(m_rcvS, 0, m_rcvS.Length);
			}

			pCln.Recv();
		}

		protected void IoSend(IAsyncResult iar)
		{
			TcpCln	pCln = (TcpCln)iar.AsyncState;
			int		sent = -1;

			try
			{
				sent = pCln.m_scH.EndSend(iar);
			}
			catch(SocketException)
			{
				Console.WriteLine("Err::IoSend::socket");
				return;
			}
			catch(Exception)
			{
				Console.WriteLine("Err::IoSend::exception");
				return;
			}

			if(pCln != this)
			{
				Console.WriteLine("Err::IoSend::Different object");
				return;
			}

			Console.WriteLine("Success::Sending byte::" + sent);
			lock (m_oLock)
			{
				m_sndC = NTC.OK;
			}
		}

		////////////////////////////////////////////////////////////////////////////
		// Interface ...

		public int Recv()
		{
			m_scH.BeginReceive(m_rcvS, 0, m_rcvS.Length, SocketFlags.None, IoRecv, this);
			return NTC.OK;
		}

		public int Send(string str, int op)
		{
			//bool hr;

			if( 0 > this.IsConnected || NTC.OK != m_sndC)
				return NTC.EFAIL;

			lock (m_oLock)
			{
				++m_nSqc;
				m_sPck.Reset();
				m_sPck.PacketAdd(str);
				m_sPck.EnCode(0, op);


				byte[]	s = m_sPck.Buf;
				int		l = m_sPck.Len;
				int		nSnd = 0;


				++m_sndN;
				m_sndN %= m_sndB.Count;

				byte[] buf = m_sndB[m_sndN];
				nSnd = PGN.Util.EnCrypt(ref buf, ref nSnd, s, l);


				m_sndC = NTC.WAIT;
				m_scH.BeginSend(m_sndB[m_sndN], 0, nSnd, SocketFlags.None, IoSend, this);

				//if(true == hr)
				//{
				//    // wait...
				//}
			}

			return NTC.OK;
		}


		public int Send(ref PGN.Packet pck)
		{
			if( 0 > this.IsConnected || NTC.OK != m_sndC)
				return NTC.EFAIL;

			lock (m_oLock)
			{
				byte[]	s = pck.Buf;
				int		l = pck.Len;
				int		nSnd = 0;
				byte[]	buf = null;

				++m_nSqc;					// increase packet sequence
				++m_sndN;					// increase buffer index
				m_sndN %= m_sndB.Count;

				buf = m_sndB[m_sndN];
				nSnd = PGN.Util.EnCrypt(ref buf, ref nSnd, s, l);

				m_sndC = NTC.WAIT;
			}

			return NTC.OK;
		}

		public int	IsConnected { get { return (null==m_scH||false==m_scH.Connected)? NTC.EFAIL: NTC.OK; } }
		public uint	Sqence		{ get { return m_nSqc;  }	set { m_nSqc = value; }			   }
	}

}// namespace PGN

