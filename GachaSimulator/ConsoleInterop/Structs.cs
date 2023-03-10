using System.Runtime.InteropServices;

namespace ConsoleInterop
{
    public static partial class Win32Console
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct COORD
        {
            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SMALL_RECT
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CONSOLE_SCREEN_BUFFER_INFO
        {
            public COORD Size;
            public COORD CursorPosition;
            public ushort Attributes;
            public SMALL_RECT Window;
            public COORD MaximumWindowSize;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct CHAR_INFO
        {
            [FieldOffset(0)]
            public char UnicodeChar;
            [FieldOffset(0)]
            public byte AsciiChar;
            [FieldOffset(2)]
            public ushort Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CONSOLE_CURSOR_INFO
        {
            public int Size;
            public bool Visible;
        }
    }
}
