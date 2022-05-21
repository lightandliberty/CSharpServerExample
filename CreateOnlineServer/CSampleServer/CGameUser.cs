using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace CSampleServer
{
    using GameServer;

    /// <summary>
    /// 하나의 session객체를 나타낸다.
    /// </summary>
    class CGameUser : IPeer
    {
        CUserToken token;

        // 매개변수의 token을 설정하고, IPeer객체에 메서드를 등록
        public CGameUser(CUserToken token)
        {
            this.token = token;
            this.token.Set_Peer(this);
        }

        void IPeer.On_Message(Const<byte[]> buffer)
        {
            // ex)
            CPacket msg = new CPacket(buffer.Value, this);
            PROTOCOL protocol = (PROTOCOL)msg.Pop_Protocol_ID();
            Console.WriteLine("-----------------------------------------------------");
            Console.WriteLine("Protocol Id " + protocol);
            switch(protocol)
            {
                case PROTOCOL.CHAT_MSG_REQ:
                    {
                        string text = msg.Pop_String();
                        Console.WriteLine(string.Format("text {0}", text));

                        CPacket response = CPacket.Create((short)PROTOCOL.CHAT_MSG_ACK);
                        response.Push(text);
                        Send(response);
                    }
                    break;
            }
        }

        void IPeer.On_Removed()
        {
            Console.WriteLine("The client disconnected.");
            Program.Remove_User(this);
        }

        public void Send(CPacket msg)
        {
            this.token.Send(msg);
        }

        void IPeer.Disconnect()
        {
            this.token.socket.Disconnect(false);
        }

        void IPeer.Process_User_Operation(CPacket msg)
        {
        }

    }


}
