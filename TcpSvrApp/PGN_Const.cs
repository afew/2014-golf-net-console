// PGN Constant
//
// type	byte
// len	2byte : packet length
// opp	2byte : operation protocol
//
//+++++++1+++++++++2+++++++++3+++++++++4+++++++++5+++++++++6+++++++++7+++++++++8

namespace PGN
{
	public static class NTC
	{
		// packet header/info
		public const int PCK_DATA					= 1360				;		// packet data size
		public const int PCK_HEAD					= 2 + 2				;		// len + opp
		public const int PCK_MAX					= PCK_HEAD+PCK_DATA	;		// max mtu for application
		public const int PCK_LIST					= 10				;		// Buffer List count

		// Network state
		public const int OK							= 0					;		// Network Ok
		public const int EFAIL						= -1				;		// Network fail exception
		public const int EFAIL_SOCK					= -2				;		// Network fail. socket exception
		public const int EFAIL_USER					= -3				;		// Network fail. user failed

		public const int WAIT						= 1					;		// Network wait message after sending data
		public const int DEFAULT					= OK				;		// Network Default state
		public const int TIME_OUT					= 15 * 1000			;		// 15s timeout
		public const int INVALID					= -1				;		// Invalidate state

		public const int MAX_CONNECT				= 3600				;		// Max connection
		public const int PGN_CLIENT					= 0					;		// Net app type: client
		public const int PGN_SERVER					= 1					;		// Net app type: server

		// IO event for delegate function accept, send receive
		public const int EV_CLOSE					= 1					;		// closed event
		public const int EV_ACCEPT					= 2					;		// accept event
		public const int EV_CONNECT					= 3					;		// connect
		public const int EV_SEND					= 4					;		// send
		public const int EV_RECV					= 5					;		// receive

		// Operation Protocol
		public const ushort CS_REQ_LOGIN			= 103				;		// snd: character name: uchar20
		public const ushort SC_ANS_LOGIN			= 104				;		// rcv: UID(uint32) + character name(char20)
		public const ushort SC_BROADCAST_USERLIST	= 105				;		// rcv: User Number(uint8) + [UID(uint32) + charcater name(uchar20) + owner(uint8) + ready(uint8)] * N
		public const ushort SC_BROADCAST_LOGOUT		= 114				;		// rcv: UID

		public const ushort CS_REQ_READY			= 106				;		// snd:
		public const ushort SC_BROADCAST_READY		= 107				;		// rcv: UID, ready?(RET_READY:RET_NOTREADY)

		public const ushort CS_REQ_GO				= 108				;		// snd:
		public const ushort SC_REQ_GO				= 109				;		// rcv: uint8
		public const ushort SC_BROADCAST_START		= 110				;		// rcv: UID, ready?(RET_READY:RET_NOTREADY)

		public const ushort CS_REQ_STOP				= 111				;		// snd:
		public const ushort SC_BROADCAST_STOP		= 112				;		// rcv: UID
		public const ushort SC_BROADCAST_QUIT		= 113				;		// rcv:

		public const ushort CS_REQ_ECHO				= 101				;		// snd/rcv: 1:1 data max(1024byte)
		public const ushort CS_REQ_BROADCAST		= 102				;		// snd/rcv: 1:n data max(1024byte)

		public const byte RST_OWNER_TRUE			= 1					;		// snd/rcv: is owner
		public const byte RST_OWNER_FALSE			= 2					;		// snd/rcv: is not owner
		public const byte RST_READY_TRUE			= 1					;		// snd/rcv: is ready
		public const byte RST_READY_FALSE			= 2					;		// snd/rcv: is not ready
		public const byte RST_SUCCESS				= 1					;		// snd/rcv: result success
		public const byte RST_FAIL					= 2					;		// snd/rcv: result failed

		// Game play Protocol
		public const ushort OP_DEFAULT				= 0					;		// Default
		public const ushort OP_CHAT					= 2					;		// Chatting
	}
}


