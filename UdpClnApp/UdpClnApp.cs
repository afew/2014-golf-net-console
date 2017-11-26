using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;


class UdpClnApp
{
	static void Main(string[] args)
	{
		System.Console.WriteLine("Start Udp Client ...");

		PGN.Udp		pNet = new PGN.Udp(PGN.NTC.DNF_CLIENT);
		PGN.Packet	pPck = new PGN.Packet();

		pNet.Create("127.0.0.1", 20000);

		int c = 0;

		int op = PGN.NTC.OP_CHAT;

		//string str = "ABCDEF HIJK: " + c;


		while(true)
		{
			++c;
			Thread.Sleep(1000);

			string str = "Send Mesage: Hello world: " + c;
			pNet.SendTo(str,op);

		}

		pNet.Destroy();
	}
}


//class UdpClnApp
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
//            m_sdH	= new IPEndPoint(IPAddress.Any, m_sPt+10);
//            m_sdR	= new IPEndPoint(IPAddress.Loopback, m_sPt);


//            m_scH.Bind(m_sdH);


//            int i = 0;
//            while( i++ < 10 )
//            {
//                Console.WriteLine("보낼 메세지를 입력하세요 ({0}/10)", i);
//                byte[] sendBuffer = Encoding.UTF8.GetBytes(Console.ReadLine());
//                m_scH.SendTo(sendBuffer, m_sdR);

//                Array.Clear(m_rcvB, 0, m_rcvB.Length);
//                int rcn = m_scH.ReceiveFrom(m_rcvB, ref m_sdR);

//                Console.Write("UDP 에코 메세지 : ");
//                Console.WriteLine(Encoding.UTF8.GetString(m_rcvB, 0, rcn));
//            }
//            m_scH.Close();
//        }
//        catch( SocketException se )
//        {
//            Console.WriteLine(se.Message);
//        }
//    }
//}
