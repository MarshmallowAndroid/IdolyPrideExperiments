using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleInterop
{
    public static partial class Win32Console
    {
        private const int STD_OUTPUT_HANDLE = -11;

        public enum CharInfoAttributes : ushort
        {
            FOREGROUND_BLUE = 0x0001,
            FOREGROUND_GREEN = 0x0002,
            FOREGROUND_RED = 0x0004,
            FOREGROUND_INTENSITY = 0x0008,
            BACKGROUND_BLUE = 0x0010,
            BACKGROUND_GREEN = 0x0020,
            BACKGROUND_RED = 0x0040,
            BACKGROUND_INTENSITY = 0x0080,
            COMMON_LVB_LEADING_BYTE = 0x0100,
            COMMON_LVB_TRAILING_BYTE = 0x0200,
            COMMON_LVB_GRID_HORIZONTAL = 0x0400,
            COMMON_LVB_GRID_LVERTICAL = 0x0800,
            COMMON_LVB_GRID_RVERTICAL = 0x1000,
            COMMON_LVB_REVERSE_VIDEO = 0x4000,
            COMMON_LVB_UNDERSCORE = 0x8000
        }

        private static readonly IntPtr writeHandle = GetStdHandle(STD_OUTPUT_HANDLE);

        public static CONSOLE_SCREEN_BUFFER_INFO ConsoleScreenBufferInfo
        {
            get
            {
                GetConsoleScreenBufferInfo(writeHandle, out CONSOLE_SCREEN_BUFFER_INFO csbi);
                return csbi;
            }
        }

        public static bool CursorVisible
        {
            get
            {
                GetConsoleCursorInfo(writeHandle, out CONSOLE_CURSOR_INFO cci);
                return cci.Visible;
            }
            set
            {
                GetConsoleCursorInfo(writeHandle, out CONSOLE_CURSOR_INFO cci);
                cci.Visible = value;
                SetConsoleCursorInfo(writeHandle, ref cci);
            }
        }

        public static int BufferWidth
        {
            get
            {
                return ConsoleScreenBufferInfo.Size.X;
            }
            set
            {
                CONSOLE_SCREEN_BUFFER_INFO temp = ConsoleScreenBufferInfo;
                temp.Size.X = (short)value;
                SetConsoleScreenBufferSize(writeHandle, temp.Size);
            }
        }

        public static int BufferHeight
        {
            get
            {
                return ConsoleScreenBufferInfo.Size.Y;
            }
            set
            {
                CONSOLE_SCREEN_BUFFER_INFO temp = ConsoleScreenBufferInfo;
                temp.Size.Y = (short)value;
                SetConsoleScreenBufferSize(writeHandle, temp.Size);
            }
        }

        public static int WindowWidth
        {
            get
            {
                return ConsoleScreenBufferInfo.Window.Right + 1;
            }
            set
            {
                CONSOLE_SCREEN_BUFFER_INFO temp = ConsoleScreenBufferInfo;
                temp.Window.Right = (short)(value - 1);
                SetConsoleWindowInfo(writeHandle, true, ref temp.Window);
            }
        }

        public static int WindowHeight
        {
            get
            {
                return ConsoleScreenBufferInfo.Window.Bottom + 1;
            }
            set
            {
                CONSOLE_SCREEN_BUFFER_INFO temp = ConsoleScreenBufferInfo;
                temp.Window.Bottom = (short)(value - 1);
                SetConsoleWindowInfo(writeHandle, true, ref temp.Window);
            }
        }

        public static int AllWidth
        {
            get
            {
                return ConsoleScreenBufferInfo.Size.X;
            }
            set
            {
                CONSOLE_SCREEN_BUFFER_INFO temp = ConsoleScreenBufferInfo;
                temp.Size.X = (short)value;
                temp.Window.Right = (short)(value - 1);
                SetConsoleScreenBufferSize(writeHandle, temp.Size);
                SetConsoleWindowInfo(writeHandle, true, ref temp.Window);
            }
        }

        public static int AllHeight
        {
            get
            {
                return ConsoleScreenBufferInfo.Size.X;
            }
            set
            {
                CONSOLE_SCREEN_BUFFER_INFO temp = ConsoleScreenBufferInfo;
                temp.Size.Y = (short)value;
                temp.Window.Bottom = (short)(value - 1);
                SetConsoleScreenBufferSize(writeHandle, temp.Size);
                SetConsoleWindowInfo(writeHandle, true, ref temp.Window);
            }
        }

        public static void Write(string value)
        {
            int charsWritten = 0;
            WriteConsoleW(writeHandle, value, value.Length, ref charsWritten);
        }

        public static void WriteAt(string value, ref CHAR_INFO[] buffer, int x, int y, SMALL_RECT writeRegion, ushort attributes = 0x0007)
        {
            int sx = x;
            int sy = y;

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '\n') { sx = x; sy++; }
                else
                {
                    int bufferIndex = sx + (sy * (writeRegion.Right + 1));

                    //if (buffer[bufferIndex].UnicodeChar != '\xDB' && buffer[bufferIndex].UnicodeChar != '\xB2')
                    buffer[bufferIndex].Attributes = attributes;
                    buffer[bufferIndex].UnicodeChar = value[i];

                    sx++;
                }

            }
        }

        public static void Clear()
        {
            CHAR_INFO[] buffer = new CHAR_INFO[BufferWidth * BufferHeight];

            CONSOLE_SCREEN_BUFFER_INFO temp = ConsoleScreenBufferInfo;

            WriteConsoleOutput(
                writeHandle, buffer,
                temp.Size,
                new COORD { X = 0, Y = 0 },
                ref temp.Window);
        }

        private static bool cleared = false;
        private static COORD prevSize;
        private static COORD currentSize;

        public static void WriteBuffer(CHAR_INFO[] buffer, SMALL_RECT writeRegion)
        {
            CONSOLE_SCREEN_BUFFER_INFO temp = ConsoleScreenBufferInfo;

            short width = (short)(writeRegion.Right + 1);
            short height = (short)(writeRegion.Bottom + 1);

            if (temp.Size.X != width || temp.Size.Y != height)
            {
                currentSize = temp.Size;

                if ((prevSize.X == 0 && prevSize.Y == 0) || (prevSize.X != currentSize.X || prevSize.Y != currentSize.Y))
                {
                    prevSize = currentSize;
                    cleared = false;
                }
            }

            if (!cleared)
            {
                Clear();
                cleared = true;
            }

            WriteConsoleOutput(
                writeHandle, buffer,
                new COORD { X = width, Y = height },
                new COORD { X = 0, Y = 0 },
                ref writeRegion);
        }

        public static void TestWrite()
        {
            int width = 40;
            int height = 20;

            CHAR_INFO[] buffer = new CHAR_INFO[width * height];

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i].UnicodeChar = (char)0xDB;
                buffer[i].Attributes = (ushort)CharInfoAttributes.FOREGROUND_GREEN;
            }

            WindowWidth = 40 - 1;
            BufferWidth = 40;
            WindowHeight = 20 - 1;
            BufferHeight = 20;

            //WriteBuffer(buffer);
        }

        public static void SetCursorPosition(int x, int y)
        {
            COORD cursorPosition;
            cursorPosition.X = (short)x;
            cursorPosition.Y = (short)y;
            SetConsoleCursorPosition(writeHandle, cursorPosition);
        }
    }
}
