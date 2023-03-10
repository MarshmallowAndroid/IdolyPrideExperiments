using ConsoleInterop;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GachaSimulator
{
    internal class ScrollingTextAnimator
    {
        private readonly string message;
        private readonly int targetY;
        private readonly Win32Console.CHAR_INFO[] buffer;
        private readonly Win32Console.SMALL_RECT writeRegion;
        private readonly int bufferWidth;
        private readonly int instanceCount;

        public ScrollingTextAnimator(string text, int y, Win32Console.CHAR_INFO[] charBuffer, Win32Console.SMALL_RECT consoleWriteRegion)
        {
            message = text;
            targetY = y;
            buffer = charBuffer;
            writeRegion = consoleWriteRegion;

            bufferWidth = writeRegion.Right + 1;

            while (instanceCount * text.Length < bufferWidth) instanceCount++;

            instanceCount = (instanceCount / 2) + 2;
        }

        public void UpdateFrame(float time, bool invert)
        {
            if (time >= 3800F) return;

            int width = invert ? -bufferWidth : bufferWidth;
            int x = (((int)(time / 10000F * width)) % (message.Length * 2)) - message.Length;

            for (int i = 0; i < bufferWidth; i++)
            {
                buffer[i + (targetY * bufferWidth)].UnicodeChar = ' ';
            }

            for (int i = 0; i < instanceCount; i++)
            {
                WriteText(x + (message.Length * 2 * i), targetY);
            }
        }

        private void WriteText(int x, int y)
        {
            for (int tx = 0; tx < message.Length; tx++)
            {
                if (x + tx >= bufferWidth || x + tx < 0) continue;

                int index = x + tx + (y * bufferWidth);
                buffer[index].UnicodeChar = message[tx];
                buffer[index].Attributes = 0x0007;
            }
        }
    }
}
