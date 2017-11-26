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
	class Udp : PGN.UdpBase
	{
		// for controll
		protected	byte[]				m_rcvB	= new byte[NTC.PCK_DATA];	// receive buffer for packet
		protected	int					m_rcvN	= -1;							// current received count

		protected	List<byte[]>		m_sndB	= new List<byte[]>();			// send queue buffer
		protected	int					m_sndC	= 0;							// sending complete?
		protected	int					m_sndN	= -1;							// current send index

		protected	uint				m_nSqc	= 0;							// packet sequence
		protected	PGN.Packet		m_sPck	= new PGN.Packet();			// for string message
		protected	byte[]				m_vCrp	= new byte[NTC.PCK_KEY];	// key for crypto


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
			Array.Clear(m_vCrp, 0, m_vCrp.Length);
			m_sPck.Reset();
		}


		public int Create(string ip, int pt)
		{
			if(null == m_sIp)
				return NTC.EFAIL;


			bool		hr			= false;
			IPAddress	ipAdd		= null;

			m_sIp					= ip;
			m_sPt					= pt;			//System.Convert.ToInt32(pt);


			ipAdd					= IPAddress.Parse(m_sIp);


			m_scH					= new Socket( AddressFamily.InterNetwork
												, SocketType.Dgram
												, ProtocolType.Udp);


			if(NTC.DNF_CLIENT  == m_bHost)
			{
				m_sdH				= new IPEndPoint(IPAddress.Any, m_sPt+10);
				m_sdR				= new IPEndPoint(ipAdd, m_sPt);
			}
			else
			{
				m_sdH				= new IPEndPoint(ipAdd, m_sPt);
				m_sdR				= new IPEndPoint(IPAddress.None, m_sPt);
			}


			m_scH.Bind(m_sdH);		// Binding


			m_arSnd					= new SocketAsyncEventArgs();
			m_arSnd.RemoteEndPoint	= m_sdR;
			m_arSnd.Completed		+= new EventHandler<SocketAsyncEventArgs>(IoComplete);

			m_arRcv					= new SocketAsyncEventArgs();
			m_arRcv.RemoteEndPoint	= m_sdR;
			m_arRcv.Completed		+= new EventHandler<SocketAsyncEventArgs>(IoComplete);
			m_arRcv.SetBuffer(new byte[NTC.PCK_MAX], 0, NTC.PCK_MAX);

			hr = m_scH.ReceiveFromAsync(m_arRcv);

			return (false == hr)? NTC.EFAIL : NTC.OK;
		}


		////////////////////////////////////////////////////////////////////////////
		// Inner Process...

		protected void IoComplete(object sender, SocketAsyncEventArgs args)
		{
			try
			{
				SocketError				err = args.SocketError;
				byte[]					rcb = args.Buffer;				// buffer
				int						rcn = args.BytesTransferred;	// buffer length
				SocketAsyncOperation	opc = args.LastOperation;

				if(SocketAsyncOperation.ReceiveFrom == opc)
				{
					// close message from server
					if(err != SocketError.Success || 0 == rcn)
					{
						Console.WriteLine("IoComplete::Socket Error");
						CloseSocket();
						return;
					}

					lock(m_oLock)
					{
						// DeCryption
						int		lenD = 0;
						PGN.Util.DeCrypt(ref m_rcvB, ref lenD, rcb, rcn);

						// DeCoding
						PGN.Packet pck = new PGN.Packet(ref m_rcvB, lenD);

						// data buffer and length
						byte[]	b = pck.DataBuf;
						int		l = pck.DataLen;


						string	s_chat = Encoding.Default
											.GetString(b, 0, l).Trim();

						Console.WriteLine(s_chat);
					}

					m_scH.ReceiveFromAsync(m_arRcv);
					return;
				}

				else if(SocketAsyncOperation.SendTo == opc)
				{
					if(err != SocketError.Success)
					{
						Console.WriteLine("IoComplete::Cann't Send to Server");
						CloseSocket();
						return; 
					}

					lock (m_oLock)
					{
						m_sndC = 0;
					}

					Console.WriteLine("IoComplete::Send compelte");
					return;
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



		////////////////////////////////////////////////////////////////////////////
		// Interface ...

		public int SendTo(string str, int op)
		{
			if( 0 < m_sndC)
				return NTC.EFAIL;

			lock (m_oLock)
			{
				++m_nSqc;
				m_sPck.Reset();
				m_sPck.PacketAdd(str);
				m_sPck.EnCode(0, op);
				m_sPck.Sqc = m_nSqc;

				byte[]	s = m_sPck.Buf;
				int		l = m_sPck.Len;
				int		nSnd = 0;


				++m_sndN;
				m_sndN %= m_sndB.Count;

				byte[] buf = m_sndB[m_sndN];
				nSnd = PGN.Util.EnCrypt(ref buf, ref nSnd, s, l);


				m_arSnd.SetBuffer(m_sndB[m_sndN], 0, nSnd);
				m_sndC = 1;

				m_scH.SendToAsync(m_arSnd);
			}

			return NTC.OK;
		}


		public int SendTo(ref PGN.Packet pck)
		{
			lock (m_oLock)
			{
				byte[]	s = pck.Buf;
				int		l = pck.Len;
				int		nSnd = 0;

				++m_nSqc;
				pck.Sqc =m_nSqc;


				++m_sndN;
				m_sndN %= m_sndB.Count;


				byte[] buf = m_sndB[m_sndN];
				nSnd = PGN.Util.EnCrypt(ref buf, ref nSnd, s, l);


				m_arSnd.SetBuffer(m_sndB[m_sndN], 0, nSnd);
				m_sndC = 1;

				m_scH.SendToAsync(m_arSnd);
			}

			return NTC.OK;
		}

		public uint	Sqence		{ get { return m_nSqc;  }	set { m_nSqc = value; }			   }
	}

}// namespace PGN

