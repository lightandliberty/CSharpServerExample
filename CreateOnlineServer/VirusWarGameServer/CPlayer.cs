using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirusWarGameServer
{
    using FreeNet;
    public class CPlayer
    {
        CGameUser owner;
        public byte player_index { get; private set; }
        public List<short> viruses { get; private set; }
        public CPlayer(CGameUser user, byte player_index)
        {
            this.owner = user;
            this.player_index = player_index;
            this.viruses = new List<short>();
        }

        public void Reset()
        {
            this.viruses.Clear();
        }

        public void Add_Cell(short position)
        {
            this.viruses.Add(position);
        }

        public void Remove_Cell(short position)
        {
            this.viruses.Remove(position);
        }

        public void Send(CPacket msg)
        {
            // 큐가 비어 있을 때는 CUserToken.sending_queue 큐에 추가한 뒤, start_send()메서드에서 CUserToken.socket.SendAsync()메서드를 호출하여 비동기 전송을 시작하고,
            // 데이터가 들어 있는 경우에는 새로 추가만 한다.
            this.owner.Send(msg);   // CGameUser.Send(CPacket msg)가 실행되어, CUserToken.Send(msg)가 실행됨.
            
            CPacket.Destroy(msg);
        }

        public void Send_For_Broadcast(CPacket msg)
        {
            this.owner.Send(msg);
        }

        public int Get_Virus_Count()
        {
            return this.viruses.Count;
        }
    }
}
