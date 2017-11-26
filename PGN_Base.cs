// PG Packet Struct
//
// type	byte
// len	2	: packet length
// opp	2	: operation protocol
//
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Net;
using System.Net.Sockets;

namespace PGN
{
	////////////////////////////////////////////////////////////////////////////
	// constant
	static class NTC
	{
		// packet header
		public const int PCK_DATA				= 1360					;		// packet data size
		public const int PCK_HEAD				= 2 + 2					;		// len + opp
		public const int PCK_MAX				= PCK_HEAD + PCK_DATA	;		// max mtu for application

		//
		public const int PCK_KEY				= 128+32				;		// Key size
		public const int PCK_LIST				= 10					;		// Buffer List count


		// Network state
		public const int OK						= 0						;		// Network Ok
		public const int EFAIL					= -1					;		// Network fail
		public const int WAIT					= 1						;		// Network wait message after sending data
		public const int DEFAULT				= OK					;		// Network Default state


		public const int MAX_CONNECT	= 3600							;		// packet data size
		public const int DNF_CLIENT		= 0								;		// Net app type: client
		public const int DNF_SERVER		= 1								;		// Net app type: server

		// Operation Protocol
		public const int CS_REQ_LOGIN			= 103					;		// snd: character name: uchar20
		public const int SC_ANS_LOGIN			= 104					;		// rcv: UID(uint32) + character name(char20)
		public const int SC_BROADCAST_USERLIST	= 105					;		// rcv: User Number(uint8) + [UID(uint32) + charcater name(uchar20) + owner(uint8) + ready(uint8)] * N
		public const int SC_BROADCAST_LOGOUT	= 114					;		// rcv: UID

		public const int CS_REQ_READY			= 106					;		// snd:
		public const int SC_BROADCAST_READY		= 107					;		// rcv: UID, ready?(RET_READY:RET_NOTREADY)

		public const int CS_REQ_GO				= 108					;		// snd:
		public const int SC_REQ_GO				= 109					;		// rcv: uint8
		public const int SC_BROADCAST_START		= 110					;		// rcv: UID, ready?(RET_READY:RET_NOTREADY)

		public const int CS_REQ_STOP			= 111					;		// snd:
		public const int SC_BROADCAST_STOP		= 112					;		// rcv: UID
		public const int SC_BROADCAST_QUIT		= 113					;		// rcv:

		public const int CS_REQ_ECHO			= 101					;		// snd/rcv: 1:1 data max(1024byte)
		public const int CS_REQ_BROADCAST		= 102					;		// snd/rcv: 1:n data max(1024byte)

		public const int RST_OWNER_TRUE			= 1						;		// snd/rcv: is owner
		public const int RST_OWNER_FALSE		= 2						;		// snd/rcv: is not owner
		public const int RST_READY_TRUE			= 1						;		// snd/rcv: is ready
		public const int RST_READY_FALSE		= 2						;		// snd/rcv: is not ready
		public const int RST_SUCCESS			= 1						;		// snd/rcv: result success
		public const int RST_FAIL				= 2						;		// snd/rcv: result failed

		// Game play Protocol
		public const int OP_DEFAULT				= 0						;		// Default
		public const int OP_CHAT				= 2						;		// Chatting
	}


	////////////////////////////////////////////////////////////////////////////////
	// Packet
	public class Packet
	{
		// atrribute
		protected	ushort	m_len = 0;								// total length
		protected	int		m_opp = -1;								// op code
		protected	byte[]	m_buf = new byte[NTC.PCK_MAX];			// buffer

		public	ushort	Len	{ get{ return m_len;} set{m_len = value; byte[] b=BitConverter.GetBytes(m_len); Array.Copy(b,0, m_buf,  0, 2); }}
		public	int		Opp	{ get{ return m_opp;} set{m_opp = value; byte[] b=BitConverter.GetBytes(m_opp); Array.Copy(b,0, m_buf,  2, 2); }}
		public	byte[]	Buf { get{ return m_buf;}}

		public	int		DataLen{ get{ return (m_len - NTC.PCK_HEAD); }}
		public	byte[]	DataBuf{ get{
				int l = m_len - NTC.PCK_HEAD;
				var v = new byte[l];
				Array.Copy(m_buf, NTC.PCK_HEAD, v, 0, l);
				return v;
			}
		}

		public Packet()
		{
			this.Reset();
		}

		public Packet(ref byte[] s, int l)
		{
			this.Reset();
			SetupFrom(ref s, l);
		}


		// Methods
		public void Reset()
		{
			Array.Clear(m_buf, 0, m_buf.Length);

			m_len = NTC.PCK_HEAD;
			m_opp = 0;
		}

		public void	PacketAdd(byte[] v, int l)
		{
			Array.Copy(v, 0, m_buf, m_len, l);
			m_len += (ushort)l;
		}


		public void	PacketAdd(float v)
		{
			byte[] b = BitConverter.GetBytes(v);
			this.PacketAdd(b, b.Length);
		}

		public void	PacketAdd(int v)
		{
			byte[] b = BitConverter.GetBytes(v);
			this.PacketAdd(b, b.Length);
		}

		public void	PacketAdd(string v)
		{
			int l = v.Length;

			for(int n=0; n< l; ++n)
				m_buf[m_len + n] = (byte)v[n];

			m_len += (ushort)l;
		}

		public void CopyFrom(ref byte[] src, int offset, int srcIdx, int l)
		{
			Array.Copy(src, srcIdx, m_buf, offset, l);
		}

		public void CopyTo(ref byte[] dst, int offset, int l)
		{
			Array.Copy(m_buf, offset, dst, 0, l);
		}

		public void SetupFrom(ref byte[] s,int l)
		{
			Array.Copy(s, 0, m_buf, 0, l);

			m_len = (ushort)System.BitConverter.ToInt16(m_buf,  0 );
			m_opp = (int   )System.BitConverter.ToInt16(m_buf,  2 );
		}

		public void EnCode(ushort _crp, int _opp)	//, int sqc)
		{
			m_opp = _opp;

			byte[] cLen = BitConverter.GetBytes(m_len);
			byte[] cOpp = BitConverter.GetBytes(m_opp);

			// len	2	: packet length
			// opp	2	: operation protocol
			Array.Copy(cLen, 0, m_buf,  0, 2);
			Array.Copy(cOpp, 0, m_buf,  2, 2);

			//byte[] bbSqc = BitConverter.GetBytes(sequnce);
			//Array.Copy(bbSqc, 0, m_buf, 16, 4);
		}

	}

	// util
	static class Util
	{
		public static int GetSocketId(ref System.Net.Sockets.Socket scH)
		{
			return scH.Handle.ToInt32();
		}


		public static int EnCrypt(ref byte[] dst, ref int lenD, byte[] src, int lenS)
		{
			Array.Clear(dst, 0, dst.Length);
			Array.Copy(src, dst, lenS);

			lenD  = lenS;
			return lenD;
		}


		public static int DeCrypt(ref byte[] dst, ref int lenD, byte[] src, int lenS)
		{
			Array.Clear(dst, 0, dst.Length);
			Array.Copy(src, dst, lenS);

			lenD  = lenS;
			return lenD;
		}



		// generate Perlin noise
		public static byte [] PerlinNoise(int key1, int key2, int key3, int key4)
		{
			return null;
		}
	}

	// Tcp base
	abstract class TcpBase
	{
		// for controll
		protected	int						m_aId	= -1;						// id from server
		protected	TcpBase					m_pPrn	= null;						// Parent instance
		protected	object					m_oLock	= new object();				// synchronizer

		// for network socket
		protected	Socket					m_scH   = null;
		protected	EndPoint				m_sdH	= null;
		protected	SocketAsyncEventArgs	m_arAcp = null;
		protected	SocketAsyncEventArgs	m_arCon = null;
		protected	SocketAsyncEventArgs	m_arRcv = null;
		protected	SocketAsyncEventArgs	m_arSnd = null;
		protected	string					m_sIp	= "";						// default ip
		protected	int						m_sPt	= 0;						// default port

		abstract public void	Destroy();
		virtual  public int		Query(string s, object v){	return PGN.NTC.EFAIL; }
		virtual  public Socket	GetSocket()				{	return m_scH; }

		public int		NetId
		{
			get { return m_aId;  }
			set { m_aId = value; }
		}
	}

	// udp base
	abstract class UdpBase
	{
		// for controll
		protected	int						m_bHost	= 0;						// Client: 0, server: 1
		protected	object					m_oLock	= new object();				// synchronizer

		// for network socket
		protected	Socket					m_scH   = null;
		protected	EndPoint				m_sdH	= null;						// local
		protected	EndPoint				m_sdR	= null;						// remote
		protected	SocketAsyncEventArgs	m_arRcv = null;
		protected	SocketAsyncEventArgs	m_arSnd = null;
		protected	string					m_sIp	= "";						// server ip
		protected	int						m_sPt	= 0;						// server port

		abstract public void	Destroy();
		virtual  public int		Query(string s, object v){	return PGN.NTC.EFAIL; }
		virtual  public Socket	GetSocket()				{	return m_scH; }


		public void CloseSocket()
		{
			if(null == m_scH)
				return;

			lock(m_oLock)
			{
				m_arRcv	= null;
				m_arSnd	= null;

				m_scH.Shutdown(SocketShutdown.Both);
				m_scH.Close();
				m_scH = null;
				m_sdH = null;
				m_sdR = null;

				m_sIp = "";
				m_sPt = 0;
			}
		}
	}
}

