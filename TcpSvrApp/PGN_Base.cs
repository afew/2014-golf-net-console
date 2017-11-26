// PGN Base
//
// type	byte
// len	2byte : packet length
// opp	2byte : operation protocol
//
//+++++++1+++++++++2+++++++++3+++++++++4+++++++++5+++++++++6+++++++++7+++++++++8

#define DEBUG

#if UNITY_EDITOR
using UnityEngine;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public static class PGLog
{
#if UNITY_EDITOR
	public static void LOGI(string str)
	{
		#if DEBUG
		Debug.Log(str);
		#endif
	}

	public static void LOGW(string str)
	{
		#if DEBUG
		Debug.LogWarning(str);
		#endif
	}

	public static void LOGE(string str)
	{
		#if DEBUG
		Debug.LogError(str);
		#endif
	}

#else
	public static void LOGI(string str)
	{
		#if DEBUG
		Console.WriteLine("[I]:" + str);
		#endif
	}

	public static void LOGW(string str)
	{
		#if DEBUG
		Console.WriteLine("[W]:" + str);
		#endif
	}

	public static void LOGE(string str)
	{
		#if DEBUG
		Console.WriteLine("[E]:" + str);
		#endif
	}
#endif
}


namespace PGN
{
	////////////////////////////////////////////////////////////////////////////
	// Packet
	public class Packet
	{
		// atrribute
		protected ushort	m_len = 0;								// total length
		protected ushort	m_opp = 0;								// op code
		protected byte[]	m_buf = new byte[NTC.PCK_MAX];			// buffer = Header + Payload

		public	ushort		Len	{ get{ return m_len;} set{m_len = value; byte[] b=BitConverter.GetBytes(m_len); Array.Copy(b,0, m_buf,  0, 2); }}
		public	ushort		Opp	{ get{ return m_opp;} set{m_opp = value; byte[] b=BitConverter.GetBytes(m_opp); Array.Copy(b,0, m_buf,  2, 2); }}
		public	byte[]		Buf { get{ return m_buf;}}

		public	byte[]		Payload{ get{
				int l = m_len;
				var v = new byte[l];
				Array.Copy(m_buf, 0 + NTC.PCK_HEAD, v, 0, l);
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

			m_len = 0;
			m_opp = 0;
		}

		// byte array
		public void	AddData(byte[] v, int l)
		{
			Array.Copy(v, 0, m_buf, NTC.PCK_HEAD + m_len, l);
			m_len += (ushort)l;
		}

		// float
		public void	AddData(float v)
		{
			byte[] b = BitConverter.GetBytes(v);
			this.AddData(b, b.Length);
		}

		// double
		public void	AddData(double v)
		{
			byte[] b = BitConverter.GetBytes(v);
			this.AddData(b, b.Length);
		}

		// int
		public void	AddData(int v)
		{
			byte[] b = BitConverter.GetBytes(v);
			this.AddData(b, b.Length);
		}

		// short
		public void	AddData(short v)
		{
			byte[] b = BitConverter.GetBytes(v);
			this.AddData(b, b.Length);
		}

		// short
		public void	AddData(byte v)
		{
			byte[] b = BitConverter.GetBytes(v);
			this.AddData(b, b.Length);
		}


		// string
		public void	AddData(string v)
		{
			int l = v.Length;

			for(int n=0; n< l; ++n)
				m_buf[m_len + n] = (byte)v[n];

			m_len += (ushort)l;
		}

		// complete the packet
		public void EnCode(ushort _opp)
		{
			m_opp = _opp;

			byte[] cLen = BitConverter.GetBytes(m_len);
			byte[] cOpp = BitConverter.GetBytes(m_opp);

			Array.Copy(cLen, 0, m_buf,  0, 2);	// len 2: packet length
			Array.Copy(cOpp, 0, m_buf,  2, 2);	// opp 2: operation protocol
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
			m_opp = (ushort)System.BitConverter.ToInt16(m_buf,  2 );
		}

		// util
		public int EnCrypt(ref byte[] dst)
		{
			byte[]	s = m_buf;
			int		l = m_len + NTC.PCK_HEAD;

			Array.Clear(dst, 0, dst.Length);
			Array.Copy(s, dst, l);

			return l;
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

		public static uint GetSocketId(ref System.Net.Sockets.Socket scH)
		{
			return (uint)scH.Handle.ToInt32();
		}
	}



	////////////////////////////////////////////////////////////////////////////
	// Tcp base

	public delegate void PGN_Fnc(int ev, int err, object socket, object data);	// close/accept/connect/send/recv callback event.: event, err?, socket, buffer

	public abstract class TcpBase
	{
		// for controll
		protected uint			m_aId	= NTC.OK;								// id from server
		protected TcpBase		m_pPrn	= null;									// Parent instance
		protected object		m_oLock	= new object();							// synchronizer

		// for I/O
		protected Socket		m_scH   = null;
		protected EndPoint		m_sdH	= null;
		protected string		m_sIp	= "";									// default ip
		protected int			m_sPt	= 0;									// default port

		protected PGN_Fnc		IoEvent	= IoDefEvent;							// Io event for user defined function

		abstract public void	Destroy();
		virtual  public int		Query(string s, object v){	return PGN.NTC.EFAIL; }
		virtual  public Socket	GetSocket()              {	return m_scH;         }
		         public void	SetIoEvent(PGN_Fnc     v){	IoEvent = v;          }

		public uint				NetId
		{
			get { return m_aId;  }
			set { m_aId = value; }
		}


		public static void IoDefEvent(int ev, int err, object socket, object data)
		{
			switch(ev)
			{
				case NTC.EV_CLOSE	: PGLog.LOGI("IoEvent: Close");		break;
				case NTC.EV_ACCEPT	: PGLog.LOGI("IoEvent: Accept");	break;
				case NTC.EV_CONNECT	: PGLog.LOGI("IoEvent: Connect");	break;
				case NTC.EV_SEND	: PGLog.LOGI("IoEvent: Send");		break;
				case NTC.EV_RECV	: PGLog.LOGI("IoEvent: Receive");	break;
				default: PGLog.LOGI("IoEvent: Not defined");			break;
			}
		}
	}


	////////////////////////////////////////////////////////////////////////////
	// udp base
	public abstract class UdpBase
	{
		// for controll
		protected int			m_bHost	= 0;									// Client: 0, server: 1
		protected object		m_oLock	= new object();							// synchronizer

		// for network socket
		protected Socket		m_scH   = null;
		protected EndPoint		m_sdH	= null;									// local
		protected EndPoint		m_sdR	= null;									// remote
		protected string		m_sIp	= "";									// server ip
		protected int			m_sPt	= 0;									// server port

		abstract public void	Destroy();
		virtual  public int		Query(string s, object v){	return PGN.NTC.EFAIL; }
		virtual  public Socket	GetSocket()				{	return m_scH; }


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
				m_sdR = null;
				m_sIp = "";
				m_sPt = 0;
			}
		}
	}
}

