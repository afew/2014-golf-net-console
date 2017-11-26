// Tcp Client
//
//+++++++1+++++++++2+++++++++3+++++++++4+++++++++5+++++++++6+++++++++7+++++++++8

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace PGN
{
	public class TcpCln : PGN.TcpBase
	{
		// for controll
		protected	byte[]			m_rcvS	= new byte[NTC.PCK_MAX];			// receive buffer for server
		protected	int				m_rcvN	= 0;								// current received count

		protected	List<byte[]>	m_sndB	= new List<byte[]>();				// send queue buffer
		protected	int				m_sndC	= NTC.OK;							// sending condition complete?
		protected	int				m_sndN	= -1;								// current send index

		protected	uint			m_nSqc	= 0;								// packet sequence
		protected	PGN.Packet		m_sPck	= new PGN.Packet();					// for string message


		public TcpCln() : this(NTC.OK){}
		public TcpCln(uint nId)
		{
			m_aId = nId;

			for(int n=0; n<NTC.PCK_LIST; ++n)
			{
				byte[]	buf = new byte[NTC.PCK_MAX];

				Array.Clear(buf, 0, buf.Length);
				m_sndB.Add(buf);
			}

			Array.Clear(m_rcvS, 0, m_rcvS.Length);
			m_sPck.Reset();
		}

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

		public int Connect()
		{
			m_scH.BeginConnect(m_sdH, IoConnect, this);
			return NTC.OK;
		}


		////////////////////////////////////////////////////////////////////////
		// Inner Process...

		protected void IoConnect(IAsyncResult iar)
		{
			int		err = NTC.OK;
			TcpCln	pCln = (TcpCln)iar.AsyncState;

			try
			{
				pCln.m_scH.EndConnect(iar);
			}
			catch(SocketException e0)
			{
				PGLog.LOGW("IoConnect::SocketException::" + e0.ToString() );
				err = NTC.EFAIL_SOCK;
				goto ERR;
			}
			catch(Exception e1)
			{
				PGLog.LOGW("IoConnect::Exception::" + e1.ToString() );
				err = NTC.EFAIL;
				goto ERR;
			}

			if(pCln != this)
			{
				PGLog.LOGW("IoConnect::Different object");
				err = NTC.EFAIL;
				goto ERR;
			}


			lock(m_oLock)
			{
				PGLog.LOGW("IoConnect::Success");
				IoEvent(NTC.EV_CONNECT, NTC.OK, pCln.m_scH, this);
			}

			pCln.Recv();
			return;

			ERR:
			lock(m_oLock)
			{
				IoEvent(NTC.EV_CONNECT, err, pCln.m_scH, this);
			}
		}


		protected void IoRecv(IAsyncResult iar)
		{
			int		err = NTC.OK;
			TcpCln	pCln = (TcpCln)iar.AsyncState;
			int		rcn = -1;

			try
			{
				rcn = pCln.m_scH.EndReceive(iar);
			}
			catch(SocketException e0)
			{
				PGLog.LOGW("IoRecv::SocketException::" + e0.ToString() );
				err = NTC.EFAIL_SOCK;
				goto ERR;
			}
			catch(Exception e1)
			{
				PGLog.LOGW("IoRecv::Exception::" + e1.ToString() );
				err = NTC.EFAIL;
				goto ERR;
			}

			if(pCln != this)
			{
				PGLog.LOGW("IoRecv::Different object");
				err = NTC.EFAIL;
				goto ERR;
			}

			// closed socket
			if(-1 == rcn || 0 == rcn)
			{
				PGLog.LOGW("IoRecv::disconnected");

				lock(m_oLock)
				{
					IoEvent(NTC.EV_CLOSE, NTC.OK, pCln.m_scH, this);
				}
				CloseSocket();
				return;
			}


			lock(m_oLock)
			{
				PGLog.LOGW("IoRecv::Received:" + rcn);
				IoEvent(NTC.EV_RECV, NTC.OK, pCln.m_scH, m_rcvS);
			}

			pCln.Recv();
			return;

			ERR:
			lock(m_oLock)
			{
				IoEvent(NTC.EV_RECV, err, pCln.m_scH, this);
			}
		}

		protected void IoSend(IAsyncResult iar)
		{
			int		err = NTC.OK;
			TcpCln	pCln = (TcpCln)iar.AsyncState;
			int		sent = -1;

			try
			{
				sent = pCln.m_scH.EndSend(iar);
			}
			catch(SocketException e0)
			{
				PGLog.LOGW("IoSend::SocketException::" + e0.ToString() );
				err = NTC.EFAIL_SOCK;
				goto ERR;
			}
			catch(Exception e1)
			{
				PGLog.LOGW("IoSend::Exception::" + e1.ToString() );
				err = NTC.EFAIL;
				goto ERR;
			}

			if(pCln != this)
			{
				PGLog.LOGW("IoSend::Different object");
				err = NTC.EFAIL;
				goto ERR;
			}

			PGLog.LOGI("IoSend::Success::Sending byte::" + sent);
			lock (m_oLock)
			{
				m_sndC = NTC.OK;
			}
			return;

			ERR:
			lock(m_oLock)
			{
				IoEvent(NTC.EV_SEND, err, pCln.m_scH, this);
			}
		}

		////////////////////////////////////////////////////////////////////////////
		// Interface ...

		public int Recv()
		{
			m_scH.BeginReceive(m_rcvS, 0, m_rcvS.Length, SocketFlags.None, IoRecv, this);
			return NTC.OK;
		}

		public int Send(string str, ushort op)
		{
			//bool hr;

			if( 0 > this.IsConnected || NTC.OK != m_sndC)
				return NTC.EFAIL;

			lock (m_oLock)
			{
				++m_nSqc;
				m_sPck.Reset();
				m_sPck.AddData(str);
				m_sPck.EnCode(op);


				byte[]	s = m_sPck.Buf;
				int		l = m_sPck.Len + NTC.PCK_HEAD;
				int		nSnd = 0;


				++m_sndN;
				m_sndN %= m_sndB.Count;

				byte[] buf = m_sndB[m_sndN];
				nSnd = PGN.Packet.EnCrypt(ref buf, ref nSnd, s, l);


				m_sndC = NTC.WAIT;
				m_scH.BeginSend(buf, 0, nSnd, SocketFlags.None, IoSend, this);

				//if(true == hr)
				//{
				//    // wait...
				//}
			}

			return NTC.OK;
		}


		public int Send(PGN.Packet pck)
		{
			if( 0 > this.IsConnected || NTC.OK != m_sndC)
				return NTC.EFAIL;

			lock (m_oLock)
			{
				int		snd = 0;
				byte[]	buf = null;

				++m_nSqc;					// increase packet sequence
				++m_sndN;					// increase buffer index
				m_sndN %= m_sndB.Count;

				buf = m_sndB[m_sndN];
				snd = pck.EnCrypt(ref buf);

				m_sndC = NTC.WAIT;
				m_scH.BeginSend(buf, 0, snd, SocketFlags.None, IoSend, this);
			}

			return NTC.OK;
		}

		public int	IsConnected { get { return (null==m_scH||false==m_scH.Connected)? NTC.EFAIL: NTC.OK; } }
		public uint	Sqence		{ get { return m_nSqc;  }	set { m_nSqc = value; }			   }		
	}

}// namespace PGN

