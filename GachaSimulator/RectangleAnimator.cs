using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ConsoleInterop;

namespace GachaSimulator
{
    internal class RectangleAnimator
    {
        private const char white = '\xDB';
        private const char lighterGray = '\xB2';
        private const char lightGray = '\xB1';
        private const char darkGray = '\xB0';
        private const char black = ' ';
        //private const char alternateWhite = '\xFE';

        private readonly int initialX;
        private readonly int targetY;
        private readonly int targetWidth;
        private readonly int targetHeight;
        private readonly int rarity;

        private readonly int bufferWidth;
        private readonly int bufferHeight;
        private Win32Console.CHAR_INFO[] buffer;
        private readonly Win32Console.SMALL_RECT writeRegion;
        private readonly string stars;

        private int brightness = 0;
        private int posXValue = 0;
        private int posYValue = 0;
        private int widthValue = 8;
        private int heightValue = 1;
        private float flickerProgress = 0;
        private bool drawRectangle = false;
        private bool drawStars = false;
        private bool forceWhite = false;

        public RectangleAnimator(
            int x, int y,
            int width, int height,
            Win32Console.CHAR_INFO[] charBuffer, Win32Console.SMALL_RECT consoleWriteRegion,
            int gachaRarity)
        {
            initialX = x - width / 2;
            targetY = y - height / 2;
            targetWidth = width;
            targetHeight = height;
            buffer = charBuffer;
            rarity = gachaRarity;

            StringBuilder starsBuilder = new();
            for (int i = 0; i < rarity; i++)
            {
                starsBuilder.Append("* ");
            }
            stars = starsBuilder.ToString().Trim();
            writeRegion = consoleWriteRegion;
            bufferWidth = writeRegion.Right + 1;
            bufferHeight = writeRegion.Bottom + 1;

            //offsetX = windowCenterX - (targetWidth / 2);
            //offsetX = windowCenterY;

            //posXValue = initialX;
            //posYValue = targetY + 4;
        }

        public void UpdateFrame(float time, float delay)
        {
            float internalTime = time - delay;

            //Win32Console.WriteAt("Time: " + time.ToString("F2"), ref buffer, 0, 0, writeRegion);
            //Win32Console.WriteAt("posXValue: " + posXValue.ToString(), ref buffer, 0, 1, writeRegion);
            //Win32Console.WriteAt("posYValue: " + posYValue.ToString(), ref buffer, 0, 2, writeRegion);
            //Win32Console.WriteAt("widthValue: " + widthValue.ToString(), ref buffer, 0, 3, writeRegion);
            //Win32Console.WriteAt("heightValue: " + heightValue.ToString(), ref buffer, 0, 4, writeRegion);

            if (internalTime >= 0F && internalTime <= 500F)
            {
                posXValue = initialX;
                posYValue = 0;
                heightValue = bufferHeight;
                drawRectangle = true;
                forceWhite = true;
            }

            Animate(internalTime, ref brightness, 400F, 600F, 256, 0, EaseOutQuint);

            // Flicker on start
            if (internalTime >= 1000F)
            {
                flickerProgress = (internalTime - 1000F) / 250f;

                if (flickerProgress < 1F)
                {
                    posXValue = initialX;
                    posYValue = targetY + 4;
                    widthValue = targetWidth;
                    heightValue = 1;
                    brightness = 256;
                    forceWhite = false;
                    drawRectangle = (int)(flickerProgress * 1000F) % 2 == 0;
                }
                else
                {
                    drawRectangle = true;
                    brightness = 256;
                }
            }

            // Enter upward
            Animate(internalTime, ref posYValue, 1000F, 300F, targetY + 4, targetY, EaseOutQuint);

            // Lengthen down
            Animate(internalTime, ref heightValue, 1350F, 300F, 1, targetHeight, EaseOutQuint);

            // Squish down
            Animate(internalTime, ref posXValue, 3200F, 500F, initialX, initialX + (targetWidth / 2 - 1), EaseOutQuint);
            Animate(internalTime, ref posYValue, 3200F, 500F, targetY, (int)(targetY + targetHeight * 0.9F / 2), EaseOutQuint);
            Animate(internalTime, ref widthValue, 3200F, 500F, targetWidth, targetWidth - (int)(targetWidth * 0.75F), EaseOutQuint);
            Animate(internalTime, ref heightValue, 3200F, 500F, targetHeight, targetHeight - (int)(targetHeight * 0.9F), EaseOutQuint);

            // Slide right regardless of time offset
            Animate(time, ref posXValue, 3400F, 500F, initialX, initialX + bufferWidth, EaseOutQuint);

            if (drawRectangle)
                DrawRectangle(posXValue, posYValue, widthValue, heightValue, brightness, forceWhite);
        }

        public void UpdateResultFrame(float time)
        {
            // Swipe rectangle on entire window
            Animate(time, ref posXValue, 0F, 0F, 0, 0, EaseOutQuint);
            Animate(time, ref posYValue, 0F, 0F, 0, 0, EaseOutQuint);
            Animate(time, ref widthValue, 0F, 200F, 0, bufferWidth, EaseOutQuint);
            Animate(time, ref heightValue, 0F, 0F, bufferHeight, 0, EaseOutQuint);

            // Move rectangle until no longer visible
            Animate(time, ref posXValue, 200F, 400F, 0, bufferWidth + 1, EaseOutQuint);
            if (time >= 200F) drawStars = true;

            if (drawStars)
                Win32Console.WriteAt(
                    stars,
                    ref buffer,
                    bufferWidth / 2 - stars.Length / 2,
                    bufferHeight / 2 - 1,
                    writeRegion);

            if (drawRectangle)
                DrawRectangle(posXValue, posYValue, widthValue, heightValue, brightness);
        }

        private delegate float InterpolationMethod(float progress);

        private static void Animate(float time, ref int variable, float start, float length, int from, int to, InterpolationMethod interpolationMethod)
        {
            if (length == 0F)
            {
                variable = from;
                return;
            }

            if (time >= start)
            {
                float progress = (time - start) / length;
                variable = (int)(from + (to - from) * interpolationMethod(progress));
            }
        }

        private static float EaseOutQuint(float progress) => progress >= 1.0F ? 1.0F : 1.0F - (float)Math.Pow(1.0F - progress, 3);

        //private static float Lerp(float progress) => progress >= 1.0F ? 1.0F : progress;

        private void DrawRectangle(int x, int y, int width, int height, int brightness, bool forceWhite = false)
        {
            for (int ry = 0; ry < height; ry++)
            {
                for (int rx = 0; rx < width; rx++)
                {
                    if (x + rx < 0 || y + ry < 0) break;
                    if (x + rx >= bufferWidth || y + ry >= bufferHeight) break;

                    int index = x + rx + (y + ry) * bufferWidth;
                    if (index < 0 || index >= buffer.Length) break;

                    char c = black;
                    if (brightness >= 64)
                        c = darkGray;
                    if (brightness >= 128)
                        c = lightGray;
                    if (brightness >= 192)
                        c = lighterGray;
                    if (brightness >= 256)
                        c = white;

                    if (c == black) continue;
                    buffer[index].UnicodeChar = c;

                    if (forceWhite)
                    {
                        buffer[index].Attributes = (ushort)(
                            Win32Console.CharInfoAttributes.FOREGROUND_RED |
                            Win32Console.CharInfoAttributes.FOREGROUND_GREEN |
                            Win32Console.CharInfoAttributes.FOREGROUND_BLUE |
                            Win32Console.CharInfoAttributes.FOREGROUND_INTENSITY);

                        continue;
                    }

                    if (rarity == 3)
                    {
                        buffer[index].Attributes = (ushort)Win32Console.CharInfoAttributes.FOREGROUND_BLUE;
                    }
                    else if (rarity == 4)
                    {
                        buffer[index].Attributes = (ushort)(
                            Win32Console.CharInfoAttributes.FOREGROUND_RED |
                            Win32Console.CharInfoAttributes.FOREGROUND_GREEN |
                            Win32Console.CharInfoAttributes.FOREGROUND_BLUE);
                    }
                    else
                    {
                        //if (rx <= (width / 3) - 1)
                        //    buffer[index].Attributes = (ushort)Win32Console.CharInfoAttributes.FOREGROUND_RED;
                        //else if (rx <= (width / 3) * 2)
                        //    buffer[index].Attributes = (ushort)Win32Console.CharInfoAttributes.FOREGROUND_GREEN;
                        //else if (rx <= (width / 3) * 3 + 1)
                        //    buffer[index].Attributes = (ushort)Win32Console.CharInfoAttributes.FOREGROUND_BLUE;

                        buffer[index].Attributes = (ushort)Win32Console.CharInfoAttributes.FOREGROUND_BLUE;
                        buffer[index].Attributes |= (ushort)Win32Console.CharInfoAttributes.FOREGROUND_GREEN;
                        buffer[index].Attributes |= (ushort)Win32Console.CharInfoAttributes.FOREGROUND_INTENSITY;
                    }
                }
            }
        }

        //private void ClearBuffer()
        //{
        //    for (int i = 0; i < buffer.Length; i++)
        //    {
        //        buffer[i].UnicodeChar = ' ';
        //        buffer[i].Attributes = (ushort)(
        //            Win32Console.CharInfoAttributes.FOREGROUND_RED |
        //            Win32Console.CharInfoAttributes.FOREGROUND_GREEN |
        //            Win32Console.CharInfoAttributes.FOREGROUND_BLUE);
        //    }
        //}
    }
}
