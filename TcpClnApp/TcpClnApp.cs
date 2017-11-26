using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;


class TcpClnApp
{
	static void Main(string[] args)
	{
		System.Console.WriteLine("Start TCP Client ...");

		PGN.TcpCln	pNet = new PGN.TcpCln();
		PGN.Packet	pPck = new PGN.Packet();

		pNet.Create(null, "192.168.0.20", 20000);
		pNet.Connect();

		int c = 0;

		while(true)
		{
			++c;
			Thread.Sleep(1000);

			//float x = 100;
			//float y = 200;
			//float z = 300;

			int op = PGN.NTC.OP_CHAT;


			//string str = "ABCDEF HIJK: " + c;
			string str = "Send Mesage: Hello world: " + c;
			pNet.Send(str, op);


			//pPck.Reset();
			//pPck.PacketAdd(x);
			//pPck.PacketAdd(y);
			//pPck.PacketAdd(z);
			//pPck.PacketAdd(str);
			//pPck.EnCode(0, op);

			//PGN_TcpCln.PGN_Packet pck = new PGN_TcpCln.PGN_Packet();
			//pck.PacketAdd(c);
			//pck.PacketAdd(str);

			//pNet.Send(pck);
			//pNet.Send();
		}

		pNet.Destroy();
	}
}
