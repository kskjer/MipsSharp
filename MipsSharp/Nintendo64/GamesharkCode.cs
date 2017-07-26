using System;
using System.Collections.Generic;
using System.Text;

namespace MipsSharp.Nintendo64
{
    public struct GamesharkCode
    {
        public enum Type
        {
            Write8 = 0x80,
            Write16 = 0x81,
            WriteGs8 = 0x88,
            WriteGs16 = 0x89,
            Equal8 = 0xD0,
            Equal16 = 0xD1,
            EqualGs8 = 0xD8,
            EqualGs16 = 0xD9,
            NotEqual8 = 0xD2,
            NotEqual16 = 0xD3,
            NotEqualGs8 = 0xDA,
            NotEqualGs16 = 0xDB,
            WriteOnBoot8 = 0xF0,
            WriteOnBoot16 = 0xF1,
            ExpansionPackDisable = 0xEE,
            AdditionalEnableCode = 0x20,
            ChangeExceptionHandler = 0xCC,
            Enabler = 0xDE,
            SetStoreLocation = 0xFF,
            Repeater = 0x50
        }

        private readonly UInt64 _storage;

        public UInt32 Code => (UInt32)(_storage >> 32);
        public UInt32 Address => Code & 0x00FFFFFF;
        public UInt16 Value => (UInt16)(_storage & 0xFFFF);
        public Type CodeType => (Type)(_storage >> 56);
        public bool IsWrite => IsWrite8 || IsWrite16;
        public bool IsWrite8 =>
            CodeType == Type.WriteGs8 ||
            CodeType == Type.Write8 ||
            CodeType == Type.WriteOnBoot8;
        public bool IsWrite16 =>
            CodeType == Type.WriteGs16 ||
            CodeType == Type.Write16 ||
            CodeType == Type.WriteOnBoot16;
        public bool CanBeRepeated =>
            CodeType == Type.WriteGs8 ||
            CodeType == Type.WriteGs16 ||
            CodeType == Type.Write8 ||
            CodeType == Type.Write16 ||
            CodeType == Type.WriteOnBoot8 ||
            CodeType == Type.WriteOnBoot16;
        public bool IsConditional =>
            CodeType == Type.Equal8 ||
            CodeType == Type.Equal16 ||
            CodeType == Type.EqualGs8 ||
            CodeType == Type.EqualGs16 ||
            CodeType == Type.NotEqual8 ||
            CodeType == Type.NotEqual16 ||
            CodeType == Type.NotEqualGs8 ||
            CodeType == Type.NotEqualGs16;

        public override string ToString() => string.Format("{0:X8} {1:X4}", Code, Value);

        public GamesharkCode((UInt32 code, UInt16 value) tuple)
            : this(tuple.code, tuple.value)
        {

        }

        public GamesharkCode(UInt32 code, UInt16 value)
        {
            _storage = ((UInt64)code << 32) | value;
        }

        public GamesharkCode(Type type, UInt32 address, UInt16 value)
            : this((UInt32)type << 24 | (address & 0xFFFFFF), value)
        {

        }

        public struct Repeater
        {
            private readonly GamesharkCode _inner;

            public byte Count => (byte)(_inner.Address >> 8);
            public byte AddressStep => (byte)(_inner.Address);
            public Int16 ValueStep => (Int16)_inner.Value;
            public GamesharkCode Code => _inner;


            public Repeater(GamesharkCode code) => _inner = code;
            public Repeater(byte count, byte addrStep, short valStep) => 
                _inner = new GamesharkCode(0x50000000U | (uint)(count << 8) | (addrStep), (ushort)valStep);
        }
    }
}
