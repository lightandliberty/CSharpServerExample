using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;

namespace FreeNet
{
    // 서버 네트워크의 출발점
    public class CNetworkService
    {
        int connected_count;
        // 클라이언트의 접속을 받아들이기 위한 객체
        CListener client_listener;  // 클라이언트의 접속을 기다림

        // 메시지 수신, 정송 시 필요한 객체
        SocketAsyncEventArgsPool receive_event_args_pool;       // 스택 SocketAsyncEventArg 개체 배열이라고 보면 됨.
        SocketAsyncEventArgsPool send_event_args_pool;          // 스택 SocketAsyncEventArg 개체 배열이라고 보면 됨.
        BufferManager buffer_manager;

        public delegate void SessionHandler(CUserToken token);  // CUserToken매개변수를 받는 형태의 대리자(함수 포인터) 형태 정의
        public SessionHandler session_created_callback { get; set; }    // CUserToken매개변수를 받는 대리자 객체(함수 포인터 배열) 생성

        // 설정
        int max_connections;
        int buffer_size;
        readonly int pre_alloc_count = 2;       // 읽기, 쓰기 2가지

        // 생성자
        public CNetworkService()
        {
            this.connected_count = 0;
            this.session_created_callback = null;
        }

        // 사전에 할당해돈 재사용할 수 있는 버퍼들과 컨텍스트 개체들에 의해 서버를 초기화한다.
        // 이러한 개체들은 사전에 할당하거나 다시 사용할 필요가 없습니다.
        // 하지만 API를 사용하여 서버의 성능향상을 위해 재사용할 수 있는 개체들을 쉽게 만드는 방법을 설명하기 위해 이 방법을 사용했습니다.
        public void Initialize()
        {
            this.max_connections = 10000;
            this.buffer_size = 1024;

            this.buffer_manager = new BufferManager(this.max_connections * this.buffer_size * this.pre_alloc_count, this.buffer_size);
            this.receive_event_args_pool = new SocketAsyncEventArgsPool(this.max_connections);  // 스택<SocketAsyncEventArg>배열의 최대 개수를 max_connections로 지정
            this.send_event_args_pool = new SocketAsyncEventArgsPool(this.max_connections);     // 스택<SocketAsyncEventArg>배열의 최대 개수를 max_connections로 지정

            // 모든 I/O 작업들이 한 조각을 사용할 하나의 큰 바이트 버퍼를 생성합니다.
            // 이것은 메모리의 조각화를 방지합니다.
            this.buffer_manager.InitBuffer();   // Buffer_Manager를 생성할 때 전달한, totalBytes로 초기화

            // 소켓비동기이벤트객체들의 풀을 미리 할당한다.
            SocketAsyncEventArgs arg;

            for(int i = 0; i < this.max_connections; i++)
            {
                // 동일한 소켓에 대고 send, receive를 하므로,
                // user token은 세션별로 하나씩만 만들어 놓고,
                // receive, send EventArgs에서 동일한 token을 참조하도록 구성한다.
                CUserToken token = new CUserToken();

                // receive pool
                {
                    // 재사용할 수 있는 소켓비동기이벤트 객체의 세트를 미리 사전할당한다.
                    arg = new SocketAsyncEventArgs();
                    arg.Completed += new EventHandler<SocketAsyncEventArgs>(Receive_Completed);
                    arg.UserToken = token;

                    // 버퍼풀로부터 한 바이트 버퍼를 소켓비동기이벤트 객체에 할당한다.
                    this.buffer_manager.SetBuffer(arg);

                    // 소켓비동기객체를 풀로 추가한다.
                    this.receive_event_args_pool.Push(arg);
                }

                // 풀에 보낸다.
                {
                    // 재사용가능한 소켓비동기이벤트객체의 한 세트를 사전 할당한다.
                    arg = new SocketAsyncEventArgs();
                    arg.Completed += new EventHandler<SocketAsyncEventArgs>(Send_Completed);
                    arg.UserToken = token;

                    // 버퍼 풀에서 하나의 바이트 버퍼를 소켓비동기이벤트 객체로 할당한다.
                    this.buffer_manager.SetBuffer(arg);

                    // 소켓비동기이벤트객체를 풀로 추가한다.
                    this.send_event_args_pool.Push(arg);
                }
            }
        }

        public void Listen(string host, int port, int backLog)
        {
            this.client_listener = new CListener();
            this.client_listener.CallBack_On_NewClient += On_New_Client;
            this.client_listener.Start(host, port, backLog);
        }

        /// <summary>
        /// todo: 
        /// 원격 서버의 접속에 성공 했을 때 호출
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="token"></param>
        public void On_Connect_Completed(Socket socket, CUserToken token)
        {
            // SocketAsyncEventArgsPool에서 빼오지 않고, 그 때 그 때 할당해서 사용한다.
            // 풀은 서버에서 클라이언트와의 통신용으로만 쓰려고 만든 것이기 때문이다.
            // 클라이언트 입장에서 서버와 통신을 할 때는 접속한 서버당 두 개의 EventArgs만 있으면 되기 때문에 그냥 new해서 쓴다.
            // 서버간 연결에서도 마찬가지다.
            // 풀링처리를 하려면 c->s로 가는 별도의 툴을 만들어서 써야 한다.
            SocketAsyncEventArgs receive_event_arg = new SocketAsyncEventArgs();
            receive_event_arg.Completed += new EventHandler<SocketAsyncEventArgs>(Receive_Completed);
            receive_event_arg.UserToken = token;
            receive_event_arg.SetBuffer(new byte[1024], 0, 1024);

            SocketAsyncEventArgs send_event_arg = new SocketAsyncEventArgs();
            send_event_arg.Completed += new EventHandler<SocketAsyncEventArgs>(Send_Completed);
            send_event_arg.UserToken = token;
            send_event_arg.SetBuffer(new byte[1024], 0, 1024);

            Begin_Receive(socket, receive_event_arg, send_event_arg);
        }

        /// <summary>
        /// 새로운 클라이언트가 접속 성공 했을 때 호출됩니다.
        /// AcceptAsync의 콜백 메서드에서 호출되며 여러 스레드에서 동시에 호출될 수 있기 때문에,
        /// 공유자원에 접근할 때는 주의해야 합니다.
        /// </summary>
        /// <param name="client_socket"></param>
        /// <param name="token"></param>
        void On_New_Client(Socket client_socket, object token)
        {
            // todo:
            // peer list처리

            Interlocked.Increment(ref this.connected_count);

            Console.WriteLine(string.Format("[{0}] A client connected. handle {1}, count {2}",
                Thread.CurrentThread.ManagedThreadId, client_socket.Handle, this.connected_count));

            // 풀에서 하나 꺼내와 사용한다.
            SocketAsyncEventArgs receive_args = this.receive_event_args_pool.Pop();
            SocketAsyncEventArgs send_args = this.send_event_args_pool.Pop();

            CUserToken user_token = null;
            if(this.session_created_callback != null)
            {
                user_token = receive_args.UserToken as CUserToken;
                this.session_created_callback(user_token);
            }

            Begin_Receive(client_socket, receive_args, send_args);
            // User_token.start_keepalive();
        }

        void Begin_Receive(Socket socket, SocketAsyncEventArgs receive_args, SocketAsyncEventArgs send_args)
        {
            // receive_args, send_args 아무 곳에서나 꺼내와도 된다. 둘 다 동일한 CUserToken을 들고 있다.
            CUserToken token = receive_args.UserToken as CUserToken;
            token.Set_Event_Args(receive_args, send_args);
            // 생성된 클라이언트 소켓을 보관해 놓고, 통신할 때 사용한다.
            token.socket = socket;

            bool pending = socket.ReceiveAsync(receive_args);
            if (!pending)
                Process_Receive(receive_args);
        }

        /// <summary>
        /// 이 메서드는 소켓에서 주거나 받는 작업이 완료되었을 때면 언제든지 호출된다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">소켓비동기이벤트객체는 완료된 받기 작업에 연관되어 있다.</param>
        void Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            if(e.LastOperation == SocketAsyncOperation.Receive)
            {
                Process_Receive(e);
                return;
            }

            throw new ArgumentException("소켓에서의 마지막으로 완료된 작업이 Receive 받기 작업이 아니었습니다.");
        }

        /// <summary>
        /// 이 메서드는 소켓에서 주거나 받거나 할 때면 언제든지 호출된다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">소켓비동기이벤트객체는 완료된 보내기 작업에 연관되어 있다.</param>
        void Send_Completed(object sender, SocketAsyncEventArgs e)
        {
            CUserToken token = e.UserToken as CUserToken;
            token.Process_Send(e);
        }

        /// <summary>
        /// 이 메서드는 비동기 받기 작업이 완료될 때, Invoded된다.
        /// 만약 원격 호스트가 연결에서 닫힌다면, 소켓도 닫힌다.
        /// </summary>
        /// <param name="e"></param>
        private void Process_Receive(SocketAsyncEventArgs e)
        {
            // 원격 호스트가 연결에서 닫혔는지 확인한다.
            CUserToken token = e.UserToken as CUserToken;
            if(e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                token.On_Receive(e.Buffer, e.Offset, e.BytesTransferred);   // IPeer.On_Message에서 정의된 메서드까지 실행되도록 연결됨. 즉 받은 패킷에 대해 처리 완료됨.

                bool pending = token.socket.ReceiveAsync(e);
                if(!pending)
                {
                    // 스택오버플로우?
                    Process_Receive(e);     // 다시 받도록 하고, 전송된 데이터가 없으면 Close_ClientSocket(token)이 실행되며 종료됨.
                }
            }
            else
            {
                Console.WriteLine(string.Format("error {0}, transferred {1}", e.SocketError, e.BytesTransferred));
                Close_ClientSocket(token);
            }
        }

        public void Close_ClientSocket(CUserToken token)
        {
            token.On_Removed();

            // 소켓비동기이벤트객체를 해제해서, 또다른 클라이언트에 의해 재사용될 수 있도록 한다.
            // 버퍼는 반환할 필요가 없다. 소켓비동기이벤트객체가 버퍼를 들고 있기 때문에
            // 이것을 재사용할 때, 들고 있는 버퍼를 그대로 사용하면 되기 때문이다.
            if (this.receive_event_args_pool != null)
                this.receive_event_args_pool.Push(token.receive_event_args);
            if (this.send_event_args_pool != null)
                this.send_event_args_pool.Push(token.send_event_args);
        }





















    }
}
