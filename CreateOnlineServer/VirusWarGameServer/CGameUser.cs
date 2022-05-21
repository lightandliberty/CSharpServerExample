using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace VirusWarGameServer
{
    /// <summary>
    /// 하나의 session객체를 나타낸다.
    /// </summary>
    public class CGameUser : IPeer
    {
        CUserToken token;

        public CGameRoom battle_room { get; private set; }

        CPlayer player;

        public CGameUser(CUserToken token)
        {
            this.token = token;
            this.token.Set_Peer(this); // On_Message() On_Removed ... 등 이 객체의 메서드들을 매개변수에 설정한다.
        }

        void IPeer.On_Message(Const<Byte[]> buffer)
        {
            // ex)
            byte[] clone = new byte[1024];
            Array.Copy(buffer.Value, clone, buffer.Value.Length);
            CPacket msg = new CPacket(clone, this);
            Program.game_main.Enqueue_Packet(msg, this);
        }

        void IPeer.On_Removed()
        {
            Console.WriteLine("클라이언트의 접속이 끊겼습니다.");
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
            PROTOCOL protocol = (PROTOCOL)msg.Pop_Protocol_ID();
            Console.WriteLine("프로토콜 ID: " + protocol);
            switch(protocol)
            {
                case PROTOCOL.ENTER_GAME_ROOM_REQ:
                    Program.game_main.Matching_Req(this);
                    break;
                case PROTOCOL.LOADING_COMPLETED:
                    this.battle_room.Loading_Complete(player);
                    break;
                case PROTOCOL.MOVING_REQ:
                    {
                        short begin_pos = msg.Pop_Int16();
                        short target_pos = msg.Pop_Int16();
                        this.battle_room.Moving_Req(this.player, begin_pos, target_pos);
                    }
                    break;
                case PROTOCOL.TURN_FINISHED_REQ:
                    this.battle_room.Turn_Finished(this.player);
                    break;
            }
        }

        public void Enter_Room(CPlayer player, CGameRoom room)
        {
            this.player = player;
            this.battle_room = room;
        }

    }
}
