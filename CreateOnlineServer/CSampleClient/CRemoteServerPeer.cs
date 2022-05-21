using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace CSampleClient
{
    using GameServer;

    class CRemoteServerPeer : IPeer
    {
        public CUserToken token { get; private set; }   // private set이므로, 생성할 때 또는 정해진 메서드를 통해서만 설정 가능.

        // 매개변수 CUserToken을 this.token에 복사하고, 이 메서드를 token객체에 등록한다.
        public CRemoteServerPeer(CUserToken token)
        {
            this.token = token;
            this.token.Set_Peer(this);  // On_Message(), On_Removed, Send(), Disconnect(), Process_User_Operation(CPacket)을 token에 등록한다.

        }


        void IPeer.On_Message(Const<byte[]> buffer)
        {
            CPacket msg = new CPacket(buffer.Value, this);
            PROTOCOL protocol_id = (PROTOCOL)msg.Pop_Protocol_ID();
            switch (protocol_id)
            {
                case PROTOCOL.CHAT_MSG_ACK:
                    {
                        string text = msg.Pop_String();
                        Console.WriteLine(string.Format("text {0}", text));
                    }
                    break;
            }
        }

        // 원격 연결이 끊겼을 때, 이 메서드가 호출됨. 
        void IPeer.On_Removed()
        {
            Console.WriteLine("Server removed.");
        }

        void IPeer.Send(CPacket msg)
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
