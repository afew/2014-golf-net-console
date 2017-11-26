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
	class TcpCln : PGN.TcpBase
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

			Array.Clear(m_rcvB, 0, m_rcvB.Length);
			Array.Clear(m_vCrp, 0, m_vCrp.Length);
			m_sPck.Reset();
		}


		public int Create(TcpBase parent, string ip, int pt)
		{
			IPAddress	ipAdd		= null;
			
			m_pPrn	= parent;
			m_sIp	= ip;
			m_sPt	= pt;			//System.Convert.ToInt32(pt);


			if(null == m_sIp)
			{
				//IPHostEntry	host	= Dns.GetHostEntry(Dns.GetHostName());
				//IPAddress[]	vAdd	= host.AddressList;

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


			m_arCon					= new SocketAsyncEventArgs();
			m_arRcv					= new SocketAsyncEventArgs();
			m_arSnd					= new SocketAsyncEventArgs();

			m_arCon.UserToken		= m_scH;
			m_arCon.RemoteEndPoint	= m_sdH;
			m_arCon.Completed		+= new EventHandler<SocketAsyncEventArgs>(IoComplete);

			m_arRcv.UserToken		= m_scH;
			m_arRcv.RemoteEndPoint	= m_sdH;
			m_arRcv.Completed		+= new EventHandler<SocketAsyncEventArgs>(IoComplete);
			m_arRcv.SetBuffer(new byte[NTC.PCK_MAX], 0, NTC.PCK_MAX);

			m_arSnd.UserToken		= m_scH;
			m_arSnd.RemoteEndPoint	= m_sdH;
			m_arSnd.Completed		+= new EventHandler<SocketAsyncEventArgs>(IoComplete);

			return NTC.OK;
		}


		public int Create(TcpBase parent, Socket scH, EndPoint sdH)
		{
			bool	hr				= false;

			m_pPrn					= parent;
			m_scH					= scH;
			m_sdH					= sdH;

			m_arRcv					= new SocketAsyncEventArgs();
			m_arSnd					= new SocketAsyncEventArgs();

			m_arRcv.UserToken		= m_scH;
			m_arRcv.RemoteEndPoint	= m_sdH;
			m_arRcv.Completed		+= new EventHandler<SocketAsyncEventArgs>(IoComplete);
			m_arRcv.SetBuffer(new byte[NTC.PCK_MAX], 0, NTC.PCK_MAX);

			m_arSnd.UserToken		= m_scH;
			m_arSnd.RemoteEndPoint	= m_sdH;
			m_arSnd.Completed		+= new EventHandler<SocketAsyncEventArgs>(IoComplete);

			hr = m_scH.ReceiveAsync(m_arRcv);

			return (false == hr)? NTC.EFAIL : NTC.OK;
		}


		public int Connect()
		{
			if(null == m_arCon)
				return NTC.EFAIL;

			m_scH.ConnectAsync(m_arCon);		// try to asynchronous connect server
			return NTC.OK;
		}

		

		public void CloseSocket()
		{
			if(null == m_scH)
				return;


			lock(m_oLock)
			{
				m_arCon	= null;
				m_arRcv	= null;
				m_arSnd	= null;

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

		protected void IoComplete(object sender, SocketAsyncEventArgs args)
		{
			try
			{
				SocketError				err = args.SocketError;
				byte[]					rcb = args.Buffer;				// buffer
				int						rcn = args.BytesTransferred;	// buffer length
				SocketAsyncOperation	opc = args.LastOperation;

				if(SocketAsyncOperation.Receive == opc)
				{
					// close message from server
					if(err != SocketError.Success || 0 == rcn)
					{
						Console.WriteLine("IoComplete::Disconnected");
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

							int op = PGN.NTC.OP_CHAT;
							this.Send("server send::" + s_chat, op);
						}
					}

					m_scH.ReceiveAsync(m_arRcv);

					return;
				}

				else if(SocketAsyncOperation.Connect == opc)
				{
					if(err != SocketError.Success)
					{
						Console.WriteLine("IoComplete::Cann't Connect to Server");
						CloseSocket();
					}
					else
					{
						Console.WriteLine("IoComplete::Connection Success");

						m_scH.ReceiveAsync(m_arRcv);	// enable the receive event
					}

					m_arCon = null;						// release the connection event instance
					return;
				}

				else if(SocketAsyncOperation.Send == opc)
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

					Console.WriteLine("IoComplete::Send complete");
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

		public int Send(string str, int op)
		{
			bool hr;
			if( 0 > this.IsConnected || 0 < m_sndC)
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

				hr = m_scH.SendAsync(m_arSnd);

				if(true == hr)
				{
					// wait...
				}
			}

			return NTC.OK;
		}


		public int Send(ref PGN.Packet pck)
		{
			if( 0 > this.IsConnected || 0 < m_sndC)
				return NTC.EFAIL;

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

				m_scH.SendAsync(m_arSnd);
			}

			return NTC.OK;
		}

		public int	IsConnected { get { return (null==m_scH||false==m_scH.Connected)? -1: 0; } }
		public uint	Sqence		{ get { return m_nSqc;  }	set { m_nSqc = value; }			   }
	}

}// namespace PGN

