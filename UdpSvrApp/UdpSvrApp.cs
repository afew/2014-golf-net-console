using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;


class UdpSvrApp
{
	static void Main(string[] args)
	{
		System.Console.WriteLine("Start Udp Server ...");

		PGN.Udp	pNet = new PGN.Udp(PGN.NTC.DNF_SERVER);

		pNet.Create("127.0.0.1", 20000);


		int c = 0;

		while(true)
		{
			++c;
			Thread.Sleep(1000);
		}
	}
}


//class UdpSvrApp
//{
//    protected static Socket		m_scH	= null;
//    protected static EndPoint	m_sdH	= null;
//    protected static EndPoint	m_sdR	= null;
//    protected static int		m_sPt	= 5432;

//    protected static byte[]		m_rcvB = new byte[512];

//    static void Main()
//    {
//        try
//        {
//            m_scH	= new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
//            m_sdH	= new IPEndPoint(IPAddress.Loopback, m_sPt);
//            m_sdR	= new IPEndPoint(IPAddress.None, m_sPt);


//            m_scH.Bind(m_sdH);


//            try
//            {
//                Console.WriteLine("UDP 에코 서버를 시작합니다");
//                while( true )
//                {
//                    Array.Clear(m_rcvB, 0, m_rcvB.Length);

//                    int rcn = m_scH.ReceiveFrom(m_rcvB, ref m_sdR);

//                    Console.Write(DateTime.Now.ToShortTimeString() + " 메세지 : ");
//                    Console.WriteLine(Encoding.UTF8.GetString(m_rcvB, 0, rcn));

//                    // 받은 데이터(m_rcvB)를 m_sdR 로 다시 보낸다
//                    m_scH.SendTo(m_rcvB, rcn, SocketFlags.None, m_sdR);
//                }
//            }
//            catch( SocketException se )
//            {
//                Console.WriteLine(se.Message);
//            }
//            finally
//            {
//                m_scH.Close();
//            }
//        }
//        catch( SocketException se )
//        {
//            Console.WriteLine(se.Message);
//        }

//    }
//}

