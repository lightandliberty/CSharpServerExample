using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeNet
{
    /// <summary>
    /// byte[] 버퍼를 참조로 보관하여 pop_xxx 메서드 호출 순서대로 데이터 변환을 수행한다.
    /// CPacket.Push(string data)에서 데이터를 UTF-8로 인코딩한 길이를 CPacket.buffer에 저장하고, 데이터를 UTF-8로 인코딩한 byte[]배열을 CPacket.buffer에 다시 저장한다.
    /// </summary>
    public class CPacket
    {
        public IPeer owner { get; private set; }
        public byte[] buffer { get; private set; }
        public int position { get; private set; }
        public Int16 protocol_id { get; private set; }

        // CPacketBufferManager에 Stack<CPacket>에 capacity멤버의 수만큼 처음에 가득 차 있는데, 거기서 Pop()해서 가져온다.
        // Protocol_ID로 this.protocol_id를 초기화
        public static CPacket Create(Int16 protocol_id)
        {
            // CPacket packet = new CPacket();
            CPacket packet = CPacketBufferManager.Pop();
            packet.Set_Protocol(protocol_id);   // CPacket.protocol_id를 설정한 후, 위치를 점프시킴.(헤더는 나중에 넣을 것이므로, 데이터부터 넣을 수 있도록)
            return packet;
        }

        /// <summary>
        /// CPacketBufferManager의 Stack<CPacket> pool에서 Pop()해서 가져 오므로, 여기서 파괴하면 다시 Stack<CPacket> pool에 Push()를 함.
        /// </summary>
        /// <param name="packet"></param>
        public static void Destroy(CPacket packet)
        {
            CPacketBufferManager.Push(packet); 
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="buffer">CPacket의 head-buffer로 이루어진 데이터를 생성자를 통해 지정할 수 있음.</param>
        /// <param name="owner">CPacket의 owner인 듯? owner의 메서드가 이곳에 저장되어, 이 객체를 통해 실행할 수 있음.</param>
        public CPacket(byte[] buffer, IPeer owner)
        {
            // 참조로만 보관하여 작업한다.
            // 복사가 필요하면 별도로 구현해야 한다.
            this.buffer = buffer;
            // 헤더는 읽을 필요 없으니, 그 이후부터 시작한다.
            this.position = Defines.HEADERSIZE;
            this.owner = owner;
        }

        // 헤더-데이터를 담은 buffer를 생성
        public CPacket()
        {
            this.buffer = new byte[1024];
        }

        /// <summary>
        /// Cpacket의 멤버 byte[] buffer에서 Int16로 바꿈. 배열 인덱스 position 사용, 다음 위치로 이동 시킴.
        /// </summary>
        /// <returns></returns>
        public Int16 Pop_Protocol_ID()
        {
            return Pop_Int16();
        }

        /// <summary>
        /// 매개변수의 CPacket에 protocol_id, buffer, position을 복사한다. 
        /// </summary>
        /// <param name="target"></param>
        public void Copy_To(CPacket target)
        {
            #region Set_Protocol(this.protocol_id)부분. this.protocol_id를 target.protocol_id에 복사 한다.
            // target.protocol_id = this.protocol_id;
            // target.position = Defines.HEADERSIZE;       // 아래에서 중복됨.

            // protocol_id를 target.buffer에 복사한 후, position을 그만큼 오프셋. set_protocol()에서 실행되는 push_int16부분인데, target.buffer와 target.position이 중복되므로 없어도 됨.
            // byte[] temp_buffer = BitConverter.GetBytes(protocol_id); // 아래에서 중복됨.
            // temp_buffer.CopyTo(target.buffer, target.position);      // 아래에서 중복됨.
            // target.position += temp_buffer.Length;                   // 아래에서 중복됨.
            #endregion Set_Protocol(this.protocol_id)부분. this.protocol_id를 target.protocol_id에 복사 한다.
            #region OverWrite(this.buffer, this.position) 부분. target.buffer와 target.position에 현재 객체의 buffer멤버와 position을 복사한다.
            // Array.Copy(this.buffer, target.buffer, this.buffer.Length);
            // target.position = this.position;
            // 위의 명령어가 차례대로 실행되는 것과 같다.
            #endregion OverWrite(this.buffer, this.position) 부분. target.buffer와 target.position에 현재 객체의 buffer멤버와 position을 복사한다.

            target.Set_Protocol(this.protocol_id);          // this.protocol_id를 설정하고, this.position에 헤더크기를 더함.
            // 첫번째 매개변수와 두번째 매개변수를 객체에 복사한다.
            target.OverWrite(this.buffer, this.position);   // this.buffer를 target.buffer에 this.buffer의 길이만큼 복사하고, this.position을 target에 전달.
        }

        /// <summary>
        /// 첫번째 매개변수와 두번째 매개변수를 객체에 복사한다.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="srcPosition"></param>
        public void OverWrite(byte[] source, int srcPosition)
        {
            Array.Copy(source, this.buffer, source.Length);
            this.position = srcPosition;
        }

        // byte[]를 byte로
        public byte Pop_Byte()
        {
            byte data = (byte)BitConverter.ToInt16(this.buffer, this.position);
            this.position += sizeof(byte);
            return data;
        }

        // byte[]를 short(Int16)으로
        public Int16 Pop_Int16()
        {
            Int16 data = BitConverter.ToInt16(this.buffer, this.position);
            this.position += sizeof(Int16);
            return data;
        }

        // byte[]를 long(Int32)로
        public Int32 Pop_Int32()
        {
            Int32 data = BitConverter.ToInt32(this.buffer, this.position);
            this.position += sizeof(Int32);
            return data;
        }

        public string Pop_String()
        {
            // 문자열 길이는 최대 2바이트까지. 0 ~ 32767
            Int16 len = BitConverter.ToInt16(this.buffer, this.position);
            this.position += sizeof(Int16);

            
            // 인코딩은 utf8로 통일한다.
            string data = System.Text.Encoding.UTF8.GetString(this.buffer, this.position, len); // byte[]배열을 string으로 변환
            this.position += len;

            return data;
        }

        public void Set_Protocol(Int16 protocol_id)
        {
            this.protocol_id = protocol_id;
            // this.buffer = new byte[1024];

            // 헤더는 나중에 넣을 것이므로 데이터부터 넣을 수 있도록 위치를 점프시켜 놓는다.
            this.position = Defines.HEADERSIZE;

            Push_Int16(protocol_id);
        }

        // 본문의 크기를 구해서(현재까지 기록한 바이트 배열 후 인덱스 - 헤더의 크기)
        // 헤더가 될 바이트 배열에 본문의 크기를 저장 후, CPacket.buffer 멤버에 복사
        // 패킷의 가장 앞 부분에, 바이트 배열 byte[] this.buffer의 현재 index - 헤더의 크기 2를 뺀 부분(지금까지 기록한 바이트 배열의 크기)(즉 CPacket 본문의 바이트 길이)를
        // this.buffer의 앞 부분에 복사
        public void Record_Size()
        {
            Int16 body_size = (Int16)(this.position - Defines.HEADERSIZE);
            byte[] header = BitConverter.GetBytes(body_size);   // 헤더가 될 바이트 배열에 본문의 크기를 저장
            header.CopyTo(this.buffer, 0);                      // 
        }

        /// <summary>
        /// Int16 데이터를 크기에 맞는 byte[]배열에 저장해서, CPacket의 buffer에 position위치부터 이어서 저장. position은 데이터의 크기만큼 이동.
        /// </summary>
        /// <param name="data"></param>
        // push()에 오버로딩된 같은 정의가 있음.
        public void Push_Int16(Int16 data)  // Int16(short)형 데이터를 크기에 맞는 byte[]배열에 저장해서, CPacket의 buffer에 position인덱스부터 이어서 저장. position은 복사한 byte[] 길이만큼 이동.
        {
            byte[] temp_buffer = BitConverter.GetBytes(data);   // Int16형 data를 byte[] 배열에 저장
            temp_buffer.CopyTo(this.buffer, this.position);
            this.position += temp_buffer.Length;                // 패킷의 기록할 인덱스 위치를 임시 버퍼의 길이로 저장.
        }

        public void Push(byte data)         // byte데이터를 크기에 맞는 byte[]배열에 저장해서, CPacket의 buffer에 position인덱스부터 이어서 저장 
        {
            byte[] temp_buffer = BitConverter.GetBytes(data);   // byte데이터를 byte[] 배열에 저장.
            temp_buffer.CopyTo(this.buffer, this.position);     // byte데이터를 byte[]배열에 저장해서, CPacket의 buffer에 position인덱스부터 저장
            this.position += sizeof(byte);
        }

        /// <summary>
        /// Int16데이터를 크기에 맞는 byte[]배열에 저장해서, CPacket의 buffer에 positino위치부터 이어서 저장. position은 데이터의 크기만큼 이동.
        /// </summary>
        /// <param name="data"></param>
        public void Push(Int16 data)
        {
            byte[] temp_buffer = BitConverter.GetBytes(data);   // data를 byte[] 배열에 저장
            temp_buffer.CopyTo(this.buffer, this.position);     // CPacket의 byte[] buffer배열에 position부터 이어서 저장
            this.position += temp_buffer.Length;
        }

        /// <summary>
        /// Int32 데이터를 크기에 맞는 byte[]배열에 저장해서, CPacket의 buffer에 position위치부터 이어서 저장. position은 데이터의 크기만큼 이동.
        /// </summary>
        /// <param name="data"></param>
        public void Push(Int32 data)        // Int32 데이터를 크기에 맞는 byte[]배열에 저장해서, CPacket의 buffer에 position위치부터 이어서 저장. position은 데이터의 크기만큼 이동.
        {
            byte[] temp_buffer = BitConverter.GetBytes(data);
            temp_buffer.CopyTo(this.buffer, this.position);
            this.position += temp_buffer.Length;
        }

        #region str데이터를 UTF8로 인코딩해서, 그 길이를 2바이트로 헤더에 저장하고, UTF8인코딩 데이터를 뒤에 저장한다. CPacket.position도 데이터 헤더와 데이터 크기만큼 오프셋
        /// <summary>
        /// 문자열 데이터를 UTF-8로 인코딩한 바이트 배열의 길이를 먼저 CPacket.buffer에 저장하고, 
        /// Cpacket.position을 배열의 길이를 담은 바이트 크기인 sizeof(Int16)만큼 오프셋
        /// 문자열 데이터를 UTF-8로 인코딩한 바이트 배열을 CPacket.buffer에 이어서 저장한다.
        /// CPacket.position을 인코딩한 배열의 크기만큼 오프셋
        /// </summary>
        /// <param name="data"></param>
        public void Push(string data)
        {
            byte[] temp_buffer = Encoding.UTF8.GetBytes(data);  // string데이터를 byte[] 배열에 저장

            Int16 len = (Int16)temp_buffer.Length;
            byte[] len_buffer = BitConverter.GetBytes(len); // short형 len값엔 문자열을 인코딩한 바이트배열의 길이가 들어 있음.
            len_buffer.CopyTo(this.buffer, this.position);  // 헤더에 short형으로 len값을 저장함.
            this.position += sizeof(Int16);                 // 문자열의 길이를 담은 short형의 바이트 크기만큼 오프셋.

            temp_buffer.CopyTo(this.buffer, this.position); // 문자열을 인코딩한 바이트 배열을 CPacket의 배열에 저장
            this.position += temp_buffer.Length;            // 문자열의 길이만큼 바이트 배열의 인덱스인 position을 오프셋
        }
        #endregion str데이터를 UTF8로 인코딩해서, 그 길이를 2바이트로 헤더에 저장하고, UTF8인코딩 데이터를 뒤에 저장한다. CPacket.position도 데이터 헤더와 데이터 크기만큼 오프셋
    }
}
