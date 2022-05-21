using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace FreeNet
{
    // 대리자 콜백 함수의 매개변수로 CUserToken 클래스의 인스턴스를 받는데...
    // SessionHandler session_created_callback 이던데,
    public class CUserToken
    {
        public Socket socket { get; set; }

        public SocketAsyncEventArgs receive_event_args { get; private set; }
        public SocketAsyncEventArgs send_event_args { get; private set; }

        // 바이트를 패킷 형식으로 해석해 주는 해석기 (바이트에서 헤더를 읽어 본문의 크기를 알아 내고, 본문의 크기만큼 바이트를 다시 읽어 리턴하는 메서드 있음)
        CMessageResolver message_resolver;

        // session객체, 어플리케이션 단계에서 구현하여 사용.
        IPeer peer;

        // 전송할 패킷을 보관해 놓는 큐. 1-Send로 처리하기 위한 큐다.
        Queue<CPacket> sending_queue;
        // sending_queue lock처리에 사용되는 객체
        private object cs_Sending_Queue;

        public CUserToken()
        {
            this.cs_Sending_Queue = new object();            // sending_queue lock처리에 사용되는 객체

            this.message_resolver = new CMessageResolver();
            this.peer = null;
            this.sending_queue = new Queue<CPacket>();
        }

        // CUserToken의 IPeer객체를 전달된 객체로 설정.
        public void Set_Peer(IPeer peer)
        {
            this.peer = peer;
        }

        // CUserToken의 receive_event_args와 send_event_args를 매개변수로 설정한다.
        public void Set_Event_Args(SocketAsyncEventArgs receive_event_args, SocketAsyncEventArgs send_event_args)
        {
            this.receive_event_args = receive_event_args;
            this.send_event_args = send_event_args;
        }

        /// <summary>
        /// 이 메서드에서 직접 바이트 데이터를 해석해도 되지만, Message resolver클래스를 따로 둔 이유는
        /// 추후에 확장성을 고려하여 다른 resolver를 구현할 때 CUserToken클래스의 코드 수정을 최소화하기 위함이다.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="transfered"></param>
        public void On_Receive(byte[] buffer, int offset, int transfered)
        {
            // 계속 패킷을 만들어, On_Message메서드에 받은 데이터를 복사한 배열을 실어 CallBack해줌.
            this.message_resolver.On_Receive(buffer, offset, transfered, On_Message);
        }

        // peer가 있으면, peer에 설정된 On_Message를 호출하여 매개변수를 전달하고,(CUserToken.On_Message와 다름) 없으면, 아무 것도 안 함.
        void On_Message(Const<byte[]> buffer)
        {
            if (this.peer != null)
                this.peer.On_Message(buffer);
        }

        // sending_queue를 비우고, peer에 설정된 On_Removed를 호출한다.
        public void On_Removed()
        {
            this.sending_queue.Clear();
            // IPeer인터페이스는 개체를 만들 수 없으므로, 상위 개체가 정의한 메서드를 호출한다.
            if (this.peer != null)
                this.peer.On_Removed();
        }

        /// <summary>
        /// 패킷을 전송한다.
        /// 큐가 비어 있을 경우에는 큐에 추가한 뒤 바로 SendAsync메서드를 호출하고,
        /// 데이터가 들어 있는 경우에는 새로 추가만 한다.
        /// 
        /// 큐인된 패킷의 전송 시점:
        ///    현재 진행중인 SendAsync가 완료되었을 때 큐를 검사하여 나머지 패킷을 전송한다.
        /// </summary>
        /// <param name="msg"></param>
        public void Send(CPacket msg)
        {
            CPacket clone = new CPacket();  // 클론 패킷을 생성하여 내용을 복사한 후,
            msg.Copy_To(clone);     

            lock(this.cs_Sending_Queue)
            {
                // 큐가 비어 있다면 큐에 추가하고 바로 비동기 전송 메서드를 호출한다.
                if(this.sending_queue.Count <= 0)
                {
                    this.sending_queue.Enqueue(clone);
                    Start_Send();
                    return;
                }

                // 큐에 무언가가 들어 있다면, 아직 이전 전송이 완료되지 않은 상태이므로, 큐에 추가만 하고 리턴한다.
                // 현재 수행중인 SendAsync가 완료된 이후에 큐를 검사하여 데이터가 있으면 SendAsync를 호출하여 전송해줄 것이다.
                Console.WriteLine("Queue is not empty. Copy and Enqueue a msg. Protocol ID : " + msg.protocol_id);
                this.sending_queue.Enqueue(clone);
            }
        }

        /// <summary>
        /// 비동기 전송을 시작한다.
        /// sending_queue에서 Peek()으로 데이터를 가져와, send_event_args의 Buffer에 복사하고, SendAsync()에서 보낸다.
        /// </summary>
        void Start_Send()
        {
            lock(this.cs_Sending_Queue)
            {
                // 전송이 아직 완료된 상태가 아니므로, 데이터만 가져오고, 큐에서 제거하진 않는다.
                CPacket msg = this.sending_queue.Peek();
                
                // 패킷에 이미 헤더와 본문의 내용이 있는 거 같은데, 있는지 없는지 확인하기.
                // 패킷의 헤더에 패킷의 본문의 사이즈를 기록한다.
                msg.Record_Size();  // this.position - 헤더의크기 2를 CPacket.buffer, 0 에 기록. (byte[]의 .CopyTo를 이용)

                // 이번에 보낼 패킷 사이즈만큼 버퍼 크기를 설정하고,
                this.send_event_args.SetBuffer(this.send_event_args.Buffer, this.send_event_args.Offset, msg.position);
                // 보낼 패킷 내용을 send_event_args의 버퍼에 복사한다.
                Array.Copy(msg.buffer, 0, this.send_event_args.Buffer, this.send_event_args.Offset, msg.position);

                // 비동기 전송 시작
                bool pending = this.socket.SendAsync(this.send_event_args); // true일 경우, 작업 완료시 send_event_args에 Completed이벤트 밠생
                if (!pending) // pending이 false일 경우,(I/O작업이 동기적으로 완료하는 경우), send_event_args에 Completed이벤트 발생 안 함.
                    Process_Send(this.send_event_args);
            }
        }

        static int sent_count = 0;
        static object cs_count = new object();  // Critical Section 임계 영역 (한 번에 하나의 스레드만 접근할 수 있도록 되어 있는 영역 Section)

        /// <summary>
        /// 비동기 전송 완료시 호출되는 콜백 메서드
        /// 위의 Process_Send(this.send_event_args)에서 호출할 땐, lock이 필요 없지만,
        /// this.socket.SendAsync(this.send_event_args)에서 완료 콜백 호출은 다른 스레드에서 호출되므로, lock이 필요.
        /// </summary>
        /// <param name="e"></param>
        public void Process_Send(SocketAsyncEventArgs e)
        {
            if(e.BytesTransferred <= 0 || e.SocketError != SocketError.Success)
            {
                // Console.WriteLine(string.Format("전송에 실패했습니다. {0}, 전송했습니다. {1}", e.SocketError, e.BytesTransferred));
                return;
            }

            lock(this.cs_Sending_Queue)
            {
                // count가 0이하일 경우는 없겠지만,
                if(this.sending_queue.Count <= 0)
                {
                    throw new Exception("Sending queue count is less than zero!");
                }

                // todo: 재전송 로직 다시 검토~~ 테스트 안 해 봤음.
                // 패킷 하나를 다 못보낸 경우는?
                int size = this.sending_queue.Peek().position;
                if(e.BytesTransferred != size)
                {
                    string error = string.Format("더 보내야 합니다. {0} 만큼 전송되었습니다. 소켓 크기 {1}", e.BytesTransferred, size);
                    Console.WriteLine(error);
                    return;
                }

                //System.Threading.Interlocked.Increment(ref send_count);
                lock(cs_count)
                {
                    ++sent_count;
                    //if(sent_count % 20000 == 0)
                    //{
                        Console.WriteLine(string.Format("process send : {0}, transferred {1}, sent count {2}", e.SocketError, e.BytesTransferred, sent_count));
                    //}
                }

                Console.WriteLine(string.Format("process send : {0}, transferred {1}, sent count {2}", e.SocketError, e.BytesTransferred, sent_count));

                // 전송 완료된 패킷을 큐에서 제거한다.
                //CPacket packet = this.sending_queue.Dequeue();
                //CPacket.Destroy(packet);
                this.sending_queue.Dequeue();

                // 아직 전송하지 않은 대기중인 패킷이 있다면 다시 한 번 전송을 요청한다.
                if (this.sending_queue.Count > 0)
                    Start_Send();
            }
        }

        //void Send_Directly(CPacket msg)
        //{
        //    msg.Record_Size();
        //    this.send_event_args.SetBuffer(this.send_event_args.Offset, msg.position);
        //    Array.Copy(msg.buffer, 0, this.send_event_args.Buffer, this.send_event_args.Offset, msg.position);
        //    bool pending = this.socket.SendAsync(this.send_event_args);
        //    if (!pending)
        //        Process_Send(this.send_event_args);
        //}

        public void Disconnect()
        {
            // 클라이언트와 연결된 소켓을 닫는다.
            try
            {
                this.socket.Shutdown(SocketShutdown.Send);
            }
            // throws if client process has already closed
            catch (Exception) { }
            this.socket.Close();
        }

        public void Start_KeepAlive()
        {
            // 콜백 메서드, 매개변수, dueTime은 timer가 실행되기 전 대기 시간. 3초에 한 번씩.
            System.Threading.Timer keepAlive = new System.Threading.Timer((object e) =>
            {
                CPacket msg = CPacket.Create(0);
                msg.Push(0);    // Int32값 0을 CPacket의 buffer에 저장
                Send(msg);      // 
            }, null, 0, 3000);  
        }
    }
}
