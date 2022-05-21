using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeNet
{
    public struct Const<T>
    {
        public T Value { get; private set; }

        public Const(T value) : this()  // 이 생성자에서 코드를 실행하기 전에, 매개변수가 없는 생성자를 호출한다. 매개변수가 여러개인데, 하나만 초기화 할 때 사용 필요.
        {
            this.Value = value;
        }

    }
}
