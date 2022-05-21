using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeNet
{
    /// <summary>
    /// CPacketBufferManager에 static Stack<CPacket> pool이 있고, pool_capacity만큼 가득 할당한 후, Pop()에서 pool.Pop()을 실행하지만, pool 원소의 개수가 0이되면 다시 가득 채운다.(반복문으로 new CPacket()로서 채움)
    /// </summary>
    public class CPacketBufferManager   // .Pop()으로 CPacket을 생성할 때 쓰임.
    {
        static object cs_buffer = new object();
        static Stack<CPacket> pool;
        static int pool_capacity;

        public static void Initialize(int capacity)
        {
            pool = new Stack<CPacket>();
            pool_capacity = capacity;
            Allocate();
        }

        /// <summary>
        /// pool_capacity멤버의 수만큼 모든 풀에 new CPacket()을 추가
        /// </summary>
        static void Allocate()
        {
            for(int i = 0; i < pool_capacity; ++i)
            {
                pool.Push(new CPacket());
            }
        }

        /// <summary>
        /// 풀의 원소 CPacket의 개수가 0이면, pool_capacity의 개수만큼 배열의 원소를 생성
        /// </summary>
        /// <returns></returns>
        public static CPacket Pop()
        {
            lock(cs_buffer)
            {
                if(pool.Count <= 0)
                {
                    Console.WriteLine("reallocate.");
                    Allocate();
                }

                return pool.Pop();
            }
        }

        /// <summary>
        /// CPacket 을 pool에 원소로 추가. (pool_capacity 는 고려하지 않음)
        /// </summary>
        /// <param name="packet"></param>
        public static void Push(CPacket packet)
        {
            lock(cs_buffer)
            {
                pool.Push(packet);
            }
        }

    }
}
