using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;   // SocketAsyncEventArgs 사용

namespace FreeNet
{
    // MDSN(Microsoft Developer Network)에 올라와 있는 샘플 코드를 그대로 사용
    // Represents a collection of reusable SocketAsyncEventArgs objects
    class SocketAsyncEventArgsPool  // 전송용, 수신용 풀 클래스 (그냥, SocketAsyncEventArgs의 Stack배열이라고 보면 됨.
    {
        // 자신의 클래스를 변수를 갖는 배열
        Stack<SocketAsyncEventArgs> m_pool;

        // object pool을 명시된 크기로 초기화
        // "capacity" 매개변수는 풀이 잡아 놓을 수 있는 SocketAsyncEventArgs 오브젝트들의 최대 갯수
        #region 속성 정의
        // 생성자
        public SocketAsyncEventArgsPool(int capacity)
        {
            // 멤버가 클래스의 인스턴스 배열을 가지는 배열임.
            m_pool = new Stack<SocketAsyncEventArgs>(capacity); // Stack의 최대 개수로 초기화 (지정된 초기화 용량)
        }

        // 소켓비동기이벤트객체 인스턴스를 풀에 추가
        // "item" 매개변수는 풀에 추가할 소켓비동기이벤트객체의 인스턴스다.
        // item.Push(item)메서드 생성
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null) { throw new ArgumentNullException("소켓비동기이벤트객체풀에 추가한 아이템은 null이 될 수 없습니다."); }
            lock(m_pool)
            {
                // 메서드 자체의 Push는 SocketAsyncEventArgsPool.Push(item)이고, m_pool.Push는 Stack<>의 Push(item)이다.
                // item이 null이 아니면, 이 클래스의 m_pool 배열 멤버에 인스턴스인 SocketAsyncEventArgs인 item을 추가.
                // 즉, 멤버가 클래스의 인스턴스 배열을 가지는 배열임.
                m_pool.Push(item);  // item이 null이 아닐 때, Stack<T>개체인 m_pool의 뒤에 삽입
            }

        }

        // 소켓비동기이벤트객체 인스턴스를 풀에서 제거
        // 풀에서 제거된 인스턴스를 리턴
        public SocketAsyncEventArgs Pop()
        {
            lock(m_pool)
            {
                return m_pool.Pop();    // Stack의 Pop()
            }
        }

        // 풀 안의 소켓비동기이벤트객체의 인스턴스 수
        public int Count
        {
            get { return m_pool.Count; }    // Stack의 Count
        }



        #endregion 속성 정의 <끝>
    }
}
