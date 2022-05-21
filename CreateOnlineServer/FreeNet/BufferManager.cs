using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace FreeNet
{
    /// <summary>
    /// <remark>
    /// "송, 수신 버퍼 풀링 기법"
    /// </remark>
    /// 이 클래스는 각각의 소켓 I/O 운영 분배할 수 있고, 각각의 소켓 Input/Output운영의 사용을 위한 소켓비동기이벤트객체들에 할당할 수 있는 하나의 큰 버퍼를 생성합니다. 
    /// 이것은 버퍼들이 쉽게 재사용되어지고 힙메모리 조각화를 방지합니다.
    /// 이 버퍼매니저 클래스의 작업들은 스레드로부터 안전하지 않습니다.
    /// </summary>
    /// <remarks>
    /// 바이트 배열을 생성하고, <c>m_buffer = new byte[m_numBytes]</c>
    /// 바이트 배열과 바이트 배열의 버퍼시작 지점(인덱스), 버퍼 크기를 매개변수로 메서드에 넘겨주면, 소켓비동기이벤트개체에서 그 부분을 버퍼로 사용하도록 설정한다.
    /// <c>args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize</c>
    /// 설정하고 인덱스는 사용(할당)했던 버퍼의 크기만큼 증가 <c>m_currentIndex += m_bufferSize</c>
    /// </remarks>
    internal class BufferManager    // 동일한 어셈블리(.dll / .exe) 안에서만 public으로 사용, 다른 어셈블리에서 재정의 불가.
    {
        int m_numBytes;         // 버퍼풀에 의해 제어되는 모든 바이트들의 수
        byte[] m_buffer;        // 버퍼 매니저에 의해 유지되는 근본적인 바이트 배열
        Stack<int> m_freeIndexPool;
        int m_currentIndex;
        int m_bufferSize;

        /// <param name="totalBytes">버퍼의 전체 크기 = 최대 동시 접속수치 * 버퍼 하나의 크기 * (전송용 1개 + 수신용 1개)</param>
        /// <param name="bufferSize">버퍼 하나의 크기 (this.buffer_size)</param>
        public BufferManager(int totalBytes, int bufferSize) 
        {
            m_numBytes = totalBytes;
            m_currentIndex = 0;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
        }

        /// <summary>
        /// 버퍼 풀에 의해 사용된 버퍼의 공간을 할당한다.
        /// </summary>
        public void InitBuffer()
        {
            // 하나의 큰 버퍼를 생성하고, 각각의 소켓비동기이벤트객체에 나눈다.
            m_buffer = new byte[m_numBytes];
        }

        /// <summary>
        /// 버퍼 풀에 있는 버퍼를 명시된 소켓비동기이벤트객체에 할당한다.
        /// <example> <code>
        /// this.buffer_manager = new BufferManager(this.max_connections * this.buffer_size * this.pre_alloc_count, this.buffer_size);
        /// </code> </example>
        /// </summary>
        /// <param name="args">소켓비동기객체</param>
        /// <returns>버퍼가 성공적으로 설정되면 true, 아니면 false </returns>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if(m_freeIndexPool.Count > 0)
            {
                args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), m_bufferSize);  // 버퍼를 해제한 적이 있을 경우, (풀링 기법에선 해당 안 됨)
            }
            else
            {
                if((m_numBytes - m_bufferSize) < m_currentIndex)
                {
                    return false;
                }
                args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);        // 버퍼를 해제한 적이 없는 경우,
                m_currentIndex += m_bufferSize;
            }
            return true;
        }

        /// <summary>
        /// 사용하지 않는 버퍼를 반환 (현재 만들고 있는 네트워크 모듈에서는 사용하지 않음.(프로그램을 시작할 때, 최대 동시 접속 수치만큼 버퍼를 할당한 후, 중간에 해제하지 않고, 계속 물고 있을 것이기 때문)
        /// 소켓비동기이벤트객체만 풀링하여 재사용할 수 있도록 처리해 놓으면, 그 객체에 할당된 버퍼도 같이 따라가게 되므로.
        /// 소켓비동기이벤트객체로부터 버퍼를 제거한다. 이것은 버퍼 풀로 다시 해제합니다.
        /// </summary>
        /// <param name="args"></param>
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            m_freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }



    }
}
