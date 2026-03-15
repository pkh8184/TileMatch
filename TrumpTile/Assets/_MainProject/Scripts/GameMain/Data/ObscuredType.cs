namespace TrumpTile.GameMain.Data
{
    /// <summary>
    /// 메모리에 기록되는 값을 암호화하기 위한 추상 클래스입니다.
    /// 타입에 맞게 재정의하기 위해 상속을 사용합니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ObscuredType<T>
    {
        protected T mValue;
        protected T mKey;

        public abstract T Value { get; set; }
        //일정 시간마다 키를 갱신하기 위한 함수
        public abstract void UpdateKey();
        //암호화 함수
        protected abstract T Encrypt(T value, T key);
        //복호화 함수
        protected abstract T Decrypt(T value, T key);
    }

    /// <summary>
    /// int형 ObscuredTpye 클래스
    /// </summary>
    public class ObscuredInt : ObscuredType<int>
    {
        //생성 시 랜덤한 키로 값을 암호화합니다.
        public ObscuredInt(int value)
        {
            mKey = UnityEngine.Random.Range(1, int.MaxValue);
            mValue = Encrypt(value, mKey);
        }
        //값 읽기 시 복호화, 값 수정 시 암호화를 위한 프로퍼티
        public override int Value { get => Decrypt(mValue, mKey); set => mValue = Encrypt(value, mKey); }

        //키 갱신
        public override void UpdateKey()
        {
            int currentValue = Value;
            mKey = UnityEngine.Random.Range(1, int.MaxValue);
            mValue = Encrypt(currentValue, mKey);
        }

        // XOR 연산으로 값을 암호화합니다.
        protected override int Encrypt(int value, int key)
        {
            return value ^ key;
        }

        // XOR 연산으로 값을 복호화합니다.
        protected override int Decrypt(int value, int key)
        {
            return value ^ key;
        }

        #region 연산자 정의

        // ObscuredInt i = 5;
        // int j = i;
        // 위 처리가 되도록 암묵적 변환 정의
        public static implicit operator ObscuredInt(int value) => new ObscuredInt(value);
        public static implicit operator int(ObscuredInt o) => o.Value;

        // 산술, 비교 연산자 정의
        public static ObscuredInt operator +(ObscuredInt a, ObscuredInt b) => new ObscuredInt(a.Value + b.Value);
        public static ObscuredInt operator -(ObscuredInt a, ObscuredInt b) => new ObscuredInt(a.Value - b.Value);
        public static ObscuredInt operator *(ObscuredInt a, ObscuredInt b) => new ObscuredInt(a.Value * b.Value);
        public static ObscuredInt operator /(ObscuredInt a, ObscuredInt b) => new ObscuredInt(a.Value / b.Value);
        public static ObscuredInt operator %(ObscuredInt a, ObscuredInt b) => new ObscuredInt(a.Value % b.Value);
        public static bool operator >(ObscuredInt a, ObscuredInt b) => a.Value > b.Value;
        public static bool operator <(ObscuredInt a, ObscuredInt b) => a.Value < b.Value;
        public static bool operator >=(ObscuredInt a, ObscuredInt b) => a.Value >= b.Value;
        public static bool operator <=(ObscuredInt a, ObscuredInt b) => a.Value <= b.Value;
        public static bool operator ==(ObscuredInt a, ObscuredInt b) => a.Value == b.Value;
        public static bool operator !=(ObscuredInt a, ObscuredInt b) => a.Value != b.Value;

        // == 연산 재정의를 위한 함수들 재정의
        public override bool Equals(object obj)
        {
            if (obj is ObscuredInt other)
            {
                return Value == other.Value;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
        #endregion
    }
    /// <summary>
    /// bool형 ObscuredType 클래스
    /// </summary>
    public class ObscuredBool : ObscuredType<bool>
    {
        public ObscuredBool(bool value)
        {
            mKey = UnityEngine.Random.Range(1, int.MaxValue) % 2 == 0;
            mValue = Encrypt(value, mKey);
        }

        public override bool Value { get => Decrypt(mValue, mKey); set => mValue = Encrypt(value, mKey); }

        public override void UpdateKey()
        {
            bool currentValue = Value;
            mKey = UnityEngine.Random.Range(1, int.MaxValue) % 2 == 0;
            mValue = Encrypt(currentValue, mKey);
        }

        // bool XOR
        protected override bool Encrypt(bool value, bool key) => value ^ key;
        protected override bool Decrypt(bool value, bool key) => value ^ key;

        #region 연산자 정의
        public static implicit operator ObscuredBool(bool value) => new ObscuredBool(value);
        public static implicit operator bool(ObscuredBool o) => o.Value;

        public static bool operator ==(ObscuredBool a, ObscuredBool b) => a.Value == b.Value;
        public static bool operator !=(ObscuredBool a, ObscuredBool b) => a.Value != b.Value;

        public override bool Equals(object obj)
        {
            if (obj is ObscuredBool other)
            {
                return Value == other.Value;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
        #endregion
    }
}


