using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace GachaSimulator
{
    class PreciseTimer
    {
        private readonly float interval;

        private Thread? timerThread;
        private bool continueRunning;

        public PreciseTimer(float interval)
        {
            this.interval = interval;
        }

        public void Start()
        {
            timerThread = new Thread(RunTimer)
            {
                IsBackground = true
            };

            timerThread.Start();

            continueRunning = true;
        }

        public void Stop()
        {
            continueRunning = false;
        }

        private void RunTimer()
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();

            double next = 0d;
            while (continueRunning)
            {
                next += interval;

                double elapsed;
                while (true)
                {
                    elapsed = ElapsedMillis(stopwatch);

                    double difference = next - elapsed;

                    //Console.CursorLeft = 0;
                    //Console.CursorTop = 0;
                    //Console.Write("{0:f2}".PadRight(Console.BufferWidth, ' '), difference);

                    if (difference > 16f)
                    {
                        Thread.Sleep(1);
                    }
                    else if (difference >= 1f)
                    {
                        Thread.SpinWait(100);
                    }
                    else if (difference > 0f)
                    {
                        Thread.SpinWait(10);
                    }
                    else if (difference <= 0f)
                    {
                        break;
                    }
                    else Thread.SpinWait(100);

                    //if (difference <= 0f) break;
                    //else Thread.SpinWait((int)(100000 * difference));

                    if (!continueRunning) return;
                }

                Elapsed?.Invoke();

                if (!continueRunning) return;

                if (stopwatch.Elapsed.TotalSeconds >= 10d)
                {
                    next = 0d;
                    stopwatch.Restart();
                }
            }
        }

        private static double ElapsedMillis(Stopwatch stopwatch)
        {
            return stopwatch.ElapsedTicks * (1000f / Stopwatch.Frequency);
        }

        public delegate void PreciseTimerEventHandler();

        public event PreciseTimerEventHandler? Elapsed;
    }
}
