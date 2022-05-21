using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

// CSampleClient에서 사용
namespace FreeNet
{
    /// <summary>
    /// Endpoint정보를 받아서 서버에 접속한다.
    /// 접속하려는 서버 하나당 인스턴스 한개씩 생성하여 사용하면 된다.
    /// </summary>
    public class CConnector
    {
        public delegate void ConnectedHandler(CUserToken token);
        public ConnectedHandler connected_callback { get; set; }

        // 원격지 서버와의 연결을 위한 소켓
        Socket client;

        CNetworkService network_service;

        public CConnector(CNetworkService network_service)
        {
            this.network_service = network_service;
            this.connected_callback = null;
        }

        public void Connect(IPEndPoint remote_endpoint)
        {
            this.client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // 비동기 접속을 위한 event args.
            SocketAsyncEventArgs event_arg = new SocketAsyncEventArgs();
            event_arg.Completed += On_Connect_Completed;
            event_arg.RemoteEndPoint = remote_endpoint;
            bool pending = this.client.ConnectAsync(event_arg);
            if (!pending)
                On_Connect_Completed(null, event_arg);
        }

        void On_Connect_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // Console.WriteLine("Connect completed!");
                CUserToken token = new CUserToken();

                // 데이터 수신 준비
                this.network_service.On_Connect_Completed(this.client, token);

                if (this.connected_callback != null)
                    this.connected_callback(token);
            }
            // Failed
            else
                Console.WriteLine(string.Format("Failed to connect. {0}", e.SocketError));

        }
            
    }
}
