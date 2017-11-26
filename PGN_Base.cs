// DF Packet Struct
//
// type	byte
// sep	2	: separater	0xcccc =110011001100100
// ver	2	: version year, month, day/2 Exception) 0x134B =>2013/04/22
// crp	2	: crypto method. nothing: 0, method: [1,65535]
//
// len	2	: packet length
// opp	4	: operation protocol
// sqc	4	: packet sequence(auto AddressFamily)
//
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Net;
using System.Net.Sockets;

namespace PGN
{
	static class NTC													// NET CONSTANT
	{
		// packet seperator, version
		public const byte PCK_SEP		= unchecked((byte)0xCC)		;	// Packet seperator
		public const byte PCK_V_M		= unchecked((byte)0x13)		;	// Packet major version(year)
		public const byte PCK_V_N		= unchecked((byte)0x41)		;	// Packet minor version(month and date)

		// packet header
		public const int PCK_DATA		= 1360						;	// packet data size
		public const int PCK_SVC		= 2 + 2 + 2					;	// sep + ver + crp
		public const int PCK_LOS		= 2 + 4 + 4					;	// len + opp + sqc
		public const int PCK_HEAD		= PCK_SVC + PCK_LOS			;	// svc + los
		public const int PCK_MAX		= PCK_HEAD + PCK_DATA		;	// max mtu for application

		//
		public const int PCK_KEY		= 128+32					;	// Key size
		public const int PCK_LIST		= 10						;	// Buffer List count


		// Network state
		public const int OK				= 0							;	// Network Ok
		public const int EFAIL			= -1						;	// Network fail
		public const int DEFAULT		= OK						;	// Network Default state


		// Operation Protocol
		public const int OPCS_ACK		= 0							;	// Ok
		public const int OPCS_EFAIL		= -1						;	// Fail
		public const int OPCS			= unchecked((int)0x10000)	;	// client to server
		public const int OPSC			= unchecked((int)0x70000)	;	// server to client

		public const int OP_DEFAULT		= 0							;	// Default
		public const int OP_MSG			= 1							;	// Simple Message
		public const int OP_CHAT		= 2							;	// Chatting
		public const int OP_LOGIN		= 3							;	// create new character
		public const int OP_CHAR_CREATE	= 4							;	// select the character for game play
		public const int OP_CHAR_SELECT	= 5							;	//
		public const int OP_TOWN		= 6							;	// enter the the town
		public const int OP_DUNGEON		= 7							;	// enter the dungeon

		public const int MAX_CONNECT	= 3600						;	// packet data size
		public const int DNF_CLIENT		= 0							;	// Net app type: client
		public const int DNF_SERVER		= 1							;	// Net app type: server
	}



	////////////////////////////////////////////////////////////////////////////////
	// Packet buffer

	public class Packet
	{
		// atrribute
		protected	ushort	m_crp = 0;								// crypto id
		protected	ushort	m_len = 0;								// total length
		protected	int		m_opp = -1;								// op code
		protected	uint	m_sqc = 0;								// sequence number
		protected	byte[]	m_buf = new byte[NTC.PCK_MAX];	// buffer

		public	ushort	Crp	{ get{ return m_crp;} set{m_crp = value; byte[] b=BitConverter.GetBytes(m_crp); Array.Copy(b,0, m_buf,  4, 2); }}
		public	ushort	Len	{ get{ return m_len;} set{m_len = value; byte[] b=BitConverter.GetBytes(m_len); Array.Copy(b,0, m_buf,  6, 2); }}
		public	int		Opp	{ get{ return m_opp;} set{m_opp = value; byte[] b=BitConverter.GetBytes(m_opp); Array.Copy(b,0, m_buf,  8, 4); }}
		public	uint	Sqc { get{ return m_sqc;} set{m_sqc = value; byte[] b=BitConverter.GetBytes(m_sqc); Array.Copy(b,0, m_buf, 12, 4); }}
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

			m_crp = 0;
			m_len = NTC.PCK_HEAD;
			m_opp = 0;
			m_sqc = 0;
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
			ushort c_sep = 0;
			ushort c_ver = 0;

			Array.Copy(s, 0, m_buf, 0, l);

			c_sep = (ushort)System.BitConverter.ToInt16(m_buf,  0 );
			c_ver = (ushort)System.BitConverter.ToInt16(m_buf,  2 );
			m_crp = (ushort)System.BitConverter.ToInt16(m_buf,  4 );
			m_len = (ushort)System.BitConverter.ToInt16(m_buf,  6 );
			m_opp = (int   )System.BitConverter.ToInt32(m_buf,  8 );
			m_sqc = (uint  )System.BitConverter.ToInt32(m_buf, 12 );
		}

		protected static byte[] cSep = new byte[2]{ NTC.PCK_SEP, NTC.PCK_SEP };
		protected static byte[] cVer = new byte[2]{ NTC.PCK_V_N, NTC.PCK_V_M };

		public void EnCode(ushort _crp, int _opp)	//, int sqc)
		{
			m_crp = _crp;
			m_opp = _opp;

			byte[] cCrp = BitConverter.GetBytes(m_crp);
			byte[] cLen = BitConverter.GetBytes(m_len);
			byte[] cOpp = BitConverter.GetBytes(m_opp);

			// sep	2	: separater	0xcccc =110011001100100
			// ver	2	: version year, month, day/2 Exception) 0x134B =>2013/04/22
			// crp	2	: crypto method. nothing: 0, method: [1,65535]
			// len	2	: packet length
			// opp	4	: operation protocol
			// sqc	4	: packet sequence(auto AddressFamily)

			Array.Copy(cSep, 0, m_buf,  0, 2);
			Array.Copy(cVer, 0, m_buf,  2, 2);
			Array.Copy(cCrp, 0, m_buf,  4, 2);
			Array.Copy(cLen, 0, m_buf,  6, 2);
			Array.Copy(cOpp, 0, m_buf,  8, 4);

			//byte[] bbSqc = BitConverter.GetBytes(sequnce);
			//Array.Copy(bbSqc, 0, m_buf, 16, 4);
		}

	}


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

