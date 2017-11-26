// UDP
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
	class Udp : PGN.UdpBase
	{
		// for controll
		protected	byte[]			m_rcvB	= new byte[NTC.PCK_DATA];			// receive buffer for packet
		protected	int				m_rcvN	= 0;								// current received count

		protected	List<byte[]>	m_sndB	= new List<byte[]>();				// send queue buffer
		protected	int				m_sndC	= 0;								// sending complete?
		protected	int				m_sndN	= -1;								// current send index

		protected	uint			m_nSqc	= 0;								// packet sequence
		protected	PGN.Packet		m_sPck	= new PGN.Packet();					// for string message

		override public void Destroy()
		{
			CloseSocket();

			m_sndC	= 0;
			m_sndN	= -1;

			for(int n=0; n<m_sndB.Count; ++n)
				Array.Clear(m_sndB[n], 0, m_sndB[n].Length);

			m_sndB.Clear();
		}


		public Udp() : this(0){}

		public Udp(int bClient)
		{
			m_bHost = bClient;

			for(int n=0; n<NTC.PCK_LIST; ++n)
			{
				byte[]	buf = new byte[NTC.PCK_MAX];

				Array.Clear(buf, 0, buf.Length);
				m_sndB.Add(buf);
			}

			Array.Clear(m_rcvB, 0, m_rcvB.Length);
			m_sPck.Reset();
		}


		public int Create(string ip, int pt)
		{
			if(null == m_sIp)
				return NTC.EFAIL;


			IPAddress	ipAdd		= null;

			m_sIp		= ip;
			m_sPt		= pt;		//System.Convert.ToInt32(pt);

			ipAdd		= IPAddress.Parse(m_sIp);
			m_scH		= new Socket( AddressFamily.InterNetwork
									, SocketType.Dgram
									, ProtocolType.Udp);


			if(NTC.PGN_CLIENT  == m_bHost)
			{
				m_sdH	= new IPEndPoint(IPAddress.Any, m_sPt+10);
				m_sdR	= new IPEndPoint(ipAdd, m_sPt);
			}
			else
			{
				m_sdH	= new IPEndPoint(ipAdd, m_sPt);
				m_sdR	= new IPEndPoint(IPAddress.None, m_sPt);
			}

			m_scH.Bind(m_sdH);		// Binding

			return NTC.OK;
		}


		////////////////////////////////////////////////////////////////////////
		// Inner Process...


		////////////////////////////////////////////////////////////////////////
		// Interface ...

		public int SendTo(string str, ushort op)
		{
			if( 0 < m_sndC)
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

				m_sndC = 1;
				m_scH.SendTo(m_sndB[m_sndN], m_sdR);
			}

			return NTC.OK;
		}


		public int SendTo(ref PGN.Packet pck)
		{
			lock (m_oLock)
			{
				byte[]	s = pck.Buf;
				int		l = pck.Len + NTC.PCK_HEAD;
				int		nSnd = 0;

				++m_nSqc;
				++m_sndN;
				m_sndN %= m_sndB.Count;


				byte[] buf = m_sndB[m_sndN];
				nSnd = PGN.Packet.EnCrypt(ref buf, ref nSnd, s, l);

				m_sndC = 1;
				m_scH.SendTo(m_sndB[m_sndN], m_sdR);
			}

			return NTC.OK;
		}

		public uint	Sqence		{ get { return m_nSqc;  }	set { m_nSqc = value; }			   }
	}

}// namespace PGN

