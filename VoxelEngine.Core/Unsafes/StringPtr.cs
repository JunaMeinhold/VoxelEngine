namespace HexaEngine.Core.Unsafes
{
    using System;
    using System.Buffers.Binary;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public unsafe struct UnsafeOldString : IFreeable, IEquatable<UnsafeOldString>
    {
        public UnsafeOldString(string str)
        {
            nint len = str.Length * sizeof(char);
            Ptr = (char*)Marshal.AllocHGlobal(len);
            fixed (char* strPtr = str)
            {
                Memcpy(strPtr, Ptr, len, len);
            }
            Length = str.Length;
        }

        public UnsafeOldString(char* ptr, int length)
        {
            Ptr = ptr;
            Length = length;
        }

        public UnsafeOldString(int length)
        {
            Ptr = (char*)Marshal.AllocHGlobal(length * sizeof(char));
            Length = length;
        }

        public char* Ptr;
        public int Length;

        public bool Compare(UnsafeOldString* other)
        {
            if (Length != other->Length)
            {
                return false;
            }

            for (uint i = 0; i < Length; i++)
            {
                if (Ptr[i] != other->Ptr[i])
                {
                    return false;
                }
            }
            return true;
        }

        public void Release()
        {
            Free(Ptr);
            Ptr = null;
        }

        public int Sizeof()
        {
            return (Length + 1) * sizeof(char) + 4;
        }

        public static implicit operator string(UnsafeOldString ptr)
        {
            return new(ptr.Ptr);
        }

        public static implicit operator UnsafeOldString(string str)
        {
            return new(str);
        }

        public static implicit operator ReadOnlySpan<char>(UnsafeOldString ptr)
        {
            return new(ptr.Ptr, ptr.Length);
        }

        public static implicit operator Span<char>(UnsafeOldString ptr)
        {
            return new(ptr.Ptr, ptr.Length);
        }

        public static implicit operator char*(UnsafeOldString ptr)
        {
            return ptr.Ptr;
        }

        public static bool operator ==(UnsafeOldString left, UnsafeOldString right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnsafeOldString left, UnsafeOldString right)
        {
            return !(left == right);
        }

        public static bool operator ==(UnsafeOldString left, string right)
        {
            if (left.Length != right.Length)
                return false;

            ReadOnlySpan<char> chars1 = new(left.Ptr, left.Length);
            ReadOnlySpan<char> chars2 = right;

            return chars1.SequenceEqual(chars2);
        }

        public static bool operator !=(UnsafeOldString left, string right)
        {
            return !(left == right);
        }

        public static bool operator ==(string left, UnsafeOldString right)
        {
            if (left.Length != right.Length)
                return false;

            ReadOnlySpan<char> chars1 = left;
            ReadOnlySpan<char> chars2 = new(right.Ptr, right.Length);

            return chars1.SequenceEqual(chars2);
        }

        public static bool operator !=(string left, UnsafeOldString right)
        {
            return !(left == right);
        }

        public override readonly string ToString()
        {
            return this;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is UnsafeOldString @string && Equals(@string);
        }

        public readonly bool Equals(UnsafeOldString other)
        {
            if (Length != other.Length)
                return false;

            ReadOnlySpan<char> chars1 = new(Ptr, Length);
            ReadOnlySpan<char> chars2 = new(other.Ptr, Length);

            return chars1.SequenceEqual(chars2);
        }

        public override readonly int GetHashCode()
        {
            return string.GetHashCode(this);
        }

        public readonly int GetHashCode(StringComparison comparison)
        {
            return string.GetHashCode(this, comparison);
        }
    }
}