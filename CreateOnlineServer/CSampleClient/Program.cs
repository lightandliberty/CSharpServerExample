using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using FreeNet;

namespace CSampleClient
{
    using GameServer;
    class Program
    {
        static List<IPeer> game_servers = new List<IPeer>();

        static void Main(string[] args)
        {
            CPacketBufferManager.Initialize(2000);
            // CNetworkService객체는 메시지의 비동기 송,수신 처리를 수행한다.
            // 메시지 송,수신은 서버, 클라이언트 모두 동일한 로직으로 처리될 수 있으므로,
            // CNetworkService객체를 생성하여 Connector객체에 넘겨준다.
            CNetworkService service = new CNetworkService();

            // endpoint정보를 갖고 있는 Connector생성. 만들어 둔 NetworkService객체를 넣어 준다.
            CConnector connector = new CConnector(service);
            // 접속 성공시 호출될 콜백 메서드 지정.
            connector.connected_callback += On_Connected_GameServer;
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7979);
            connector.Connect(endpoint);
            // System.Threading.Thread.Sleep(10);

            while (true)
            {
                Console.Write("> ");
                string line = Console.ReadLine();
                if (line == "q")
                {
                    break;
                }

                CPacket msg = CPacket.Create((short)PROTOCOL.CHAT_MSG_REQ);
                msg.Push(line);
                game_servers[0].Send(msg);
            }

            // IPeer 인터페이스 객체를 상속한 객체로 캐스트 후, 상속한 객체의 IPeer객체에 접근하여, 그 객체의 .Disconnect()를 실행하는 걸로, IPeer인터페이스 객체의 메서드에 접근할 수 있음.
            // 즉, CRemoteServerPeer의 IPeer객체 token의 메서드를 실행할 수 있음.
            ((CRemoteServerPeer)game_servers[0]).token.Disconnect();    // CUserToken의 Disconnect()가 실행됨. CRemoteServerPeer의 Disconnect() 아님.

            // System.Threading.Tread.Sleep(1000 * 20);
            Console.ReadKey();
        }

        /// <summary>
        /// 접속 성공시 호출될 콜백 메서드
        /// </summary>
        /// <param name="server_token"></param>
        static void On_Connected_GameServer(CUserToken server_token)
        {
            lock(game_servers)
            {
                IPeer server = new CRemoteServerPeer(server_token);
                game_servers.Add(server);
                Console.WriteLine("Connected!");
            }
        }










    }
}
