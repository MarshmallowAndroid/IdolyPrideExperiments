using ConsoleInterop;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Reflection;
using System.Text;

namespace GachaSimulator
{
    internal class Program
    {
        private static IdolyPrideAssetManager? assetManager;

        private static LoopWaveStream? waitBgm;
        private static LoopWaveStream? gachaMovieBgm;
        private static LoopWaveStream? gachaResultBgm;
        private static WaveStream? gachaSummaryInSound;
        private static WaveStream? gachaSummaryOutSound;
        private static WaveStream? gachaSummaryStripeSound;

        static void Main(string[] args)
        {
            if (args.Length < 1)
                assetManager = new();
            else
                assetManager = new(args[0]);

            waitBgm = assetManager.LoadSoundFragment("sud_bgm_cmn_wait");
            gachaMovieBgm = assetManager.LoadSoundFragment("sud_bgm_cmn_gacha-movie-01");
            gachaResultBgm = assetManager.LoadSoundFragment("sud_bgm_cmn_gacha-result-01");

            gachaSummaryInSound = assetManager.LoadSound("sud_se_sys_gacha-summary-in");
            gachaSummaryOutSound = assetManager.LoadSound("sud_se_sys_gacha-summary-out");
            gachaSummaryStripeSound = assetManager.LoadSound("sud_se_sys_gacha-summary-stripe-3");

            if (waitBgm == null || gachaMovieBgm == null || gachaResultBgm == null) return;
            if (gachaSummaryInSound == null || gachaSummaryOutSound == null || gachaSummaryStripeSound == null) return;

            gachaMovieBgm.Loop = false;

            Random random = new();

            bool quit = false;
            while (!quit)
            {
                waitBgm.Position = 0;
                gachaMovieBgm.Position = 0;
                gachaResultBgm.Position = 0;

                int[] rarities = new int[10];
                bool hasSr = false;
                bool hasSsr = false;
                for (int i = 0; i < rarities.Length; i++)
                {
                    double rate = random.NextDouble() * 100D;

                    rarities[i] = CalculateRarity(rate);

                    if (rarities[i] == 4 && !hasSr) hasSr = true;
                    else if (rarities[i] == 5 && !hasSsr) hasSsr = true;

                    // Guaranteed 4* and above
                    if (rarities.Length == 10)
                    {
                        // If min roll
                        if (i == rarities.Length - 1 && !hasSr && !hasSsr)
                        {
                            int guaranteedRarity = 0;
                            while (true)
                            {
                                if (guaranteedRarity == 4 || guaranteedRarity == 5) break;
                                guaranteedRarity = CalculateRarity(random.NextDouble() * 100D);
                            }
                            rarities[i] = guaranteedRarity;
                        }
                    }
                }

                FadeInOutSampleProvider fade = new(waitBgm.ToSampleProvider());
                AudioEngine.Instance.Play(fade);

                Console.Clear();
                DisplayCenter("TAP TO START");
                Console.ReadKey(true);
                Console.Clear();
                fade.BeginFadeOut(100);
                Thread.Sleep(1000);
                AudioEngine.Instance.Reset();

                AudioEngine.Instance.Play(gachaMovieBgm.ToSampleProvider());
                AudioEngine.Instance.Play(new FadeInOutSampleProvider(new OffsetSampleProvider(gachaResultBgm.ToSampleProvider())
                {
                    DelayBy = TimeSpan.FromSeconds(8)
                }));

                CancellationTokenSource cancelSource = new();
                Task.Run(() => GachaAnimation(rarities, gachaMovieBgm, gachaResultBgm, cancelSource.Token));

                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                        if (keyInfo.Key == ConsoleKey.R)
                        {
                            cancelSource.Cancel();
                            AudioEngine.Instance.Reset();
                            break;
                        }
                        else if (keyInfo.Key == ConsoleKey.Q)
                        {
                            quit = true;
                            break;
                        }
                    }

                    Thread.Sleep(100);
                }
            }
        }

        private static void GachaFinish()
        {
            if (assetManager == null) return;

            LoopWaveStream? topAudio = assetManager.LoadSoundFragment("sud_bgm_cmn_gacha-top-01");
            FadeInOutSampleProvider fade = new(topAudio.ToSampleProvider());

            if (AudioEngine.Instance.MixerInputs.Last() is FadeInOutSampleProvider lastFade)
                lastFade.BeginFadeOut(500);

            AudioEngine.Instance.Play(fade);
            fade.BeginFadeIn(500);
        }

        private static void PlayRectangles(int[] rarities, CancellationToken cancellationToken)
        {
            int bufferWidth = Console.WindowWidth;
            int bufferHeight = Console.WindowHeight;

            int x = bufferWidth / 2;
            int y = bufferHeight / 2;

            Win32Console.CHAR_INFO[] buffer = new Win32Console.CHAR_INFO[bufferWidth * bufferHeight];
            Win32Console.SMALL_RECT writeRegion = new()
            {
                Top = 0,
                Left = 0,
                Bottom = (short)(bufferHeight - 1),
                Right = (short)(bufferWidth - 1)
            };

            PreciseTimer preciseTimer = new(1000F / 60F);

            ScrollingTextAnimator scroll1 = new("I D O L Y   P R I D E", 3, buffer, writeRegion);
            ScrollingTextAnimator scroll2 = new("I D O L Y   P R I D E", bufferHeight - 3 - 1, buffer, writeRegion);

            List<RectangleAnimator> animators = new();

            int rCount = rarities.Length;
            int rWidth = 8;
            int rHeight = 20;
            x -= (rCount * rWidth / 2);

            for (int i = 0; i < rCount; i++)
            {
                animators.Add(new RectangleAnimator(x + (rWidth * i) + i, y, rWidth, rHeight, buffer, writeRegion, rarities[i]));
            }

            float time = 0F;
            float timeStart = -1F;
            int showIndex = 0;
            bool inSoundPlayed = false;
            bool stripeSoundPlayed = false;
            bool outSoundPlayed = false;
            preciseTimer.Elapsed += () =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    preciseTimer.Stop();
                    return;
                }

                scroll1.UpdateFrame(time, true);
                scroll2.UpdateFrame(time, false);

                for (int i = 0; i < rCount; i++)
                {
                    animators[i].UpdateFrame(time, 30F * i);
                }

                if (time >= 400F && !inSoundPlayed)
                {
                    AudioEngine.Instance.Play(new SoundEffect(gachaSummaryInSound!));
                    inSoundPlayed = true;
                }

                if (time >= 1000F + 300F && !stripeSoundPlayed)
                {
                    for (int i = 0; i < rCount; i++)
                    {
                        AudioEngine.Instance.Play(new SoundEffect(gachaSummaryStripeSound!));
                    }
                    stripeSoundPlayed = true;
                }

                if (time >= 1000F + 2200F && !outSoundPlayed)
                {
                    AudioEngine.Instance.Play(new SoundEffect(gachaSummaryOutSound!));
                    outSoundPlayed = true;
                }

                if (time >= 1000F + 2600F)
                {
                    if (timeStart < 0) timeStart = time;

                    if (showIndex >= animators.Count)
                    {
                        preciseTimer.Stop();
                        Console.Clear();

                        StringBuilder messageBuilder = new();
                        messageBuilder.AppendLine("Gacha result\n");
                        for (int i = 0; i < rarities.Length; i++)
                        {
                            StringBuilder starsBuilder = new();
                            for (int j = 0; j < rarities[i]; j++)
                            {
                                starsBuilder.Append("* ");
                            }
                            messageBuilder.AppendLine(starsBuilder.ToString().Trim());
                        }

                        GachaFinish();

                        messageBuilder.AppendLine("\nR to reroll. Q to quit.");
                        DisplayCenter(messageBuilder.ToString());

                        return;
                    }

                    animators[showIndex].UpdateResultFrame(time - timeStart);

                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey();
                        timeStart = -1F;
                        showIndex++;
                    }
                }

                Win32Console.WriteBuffer(buffer, writeRegion);
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i].UnicodeChar = ' ';
                    buffer[i].Attributes = (ushort)(
                        Win32Console.CharInfoAttributes.FOREGROUND_RED |
                        Win32Console.CharInfoAttributes.FOREGROUND_GREEN |
                        Win32Console.CharInfoAttributes.FOREGROUND_BLUE);
                }

                time += (1000F / 60F);
            };

            preciseTimer.Start();
        }

        private static async void GachaAnimation(int[] rarities, WaveStream movie, WaveStream result, CancellationToken cancellationToken)
        {
            string[] messages = new[]
            {
                "W H A T\n\n\nI S\n\n\n\" I D O L \" ?",
                "W H A T",
                "I S I S I S I S I S I S I S I S I S I S I S I S I S I S I S I S I S I S I S I S",
                "\" I D O L \" ?",
                "WHAT IS",
                "\"IDOL\"?",
                "WHAT IS \"IDOL\"?",
                "",
                ""
            };
            int messageIndex = -1;
            int previousMessageIndex = -1;

            //PlayRectangles(stars, cancellationToken);
            //return;

            //Win32Console.CHAR_INFO[] buffer = new Win32Console.CHAR_INFO[Win32Console.BufferWidth * Win32Console.BufferHeight];

            try
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    double currentMovieTime = movie.CurrentTime.TotalMilliseconds;
                    double currentResultTime = result.CurrentTime.TotalMilliseconds;

                    if (currentMovieTime >= 500D) messageIndex = 0;
                    if (currentMovieTime >= 1300D) messageIndex = 1;
                    if (currentMovieTime >= 2200D) messageIndex = 2;
                    if (currentMovieTime >= 2900D) messageIndex = 3;
                    if (currentMovieTime >= 3800D) messageIndex = 4;
                    if (currentMovieTime >= 4800D) messageIndex = 5;
                    if (currentMovieTime >= 5600D) messageIndex = 6;
                    if (currentMovieTime >= 7300D) messageIndex = 7;
                    if (currentResultTime > 0D)
                    {
                        PlayRectangles(rarities, cancellationToken);
                        messageIndex = 8;
                    }

                    if (previousMessageIndex != messageIndex)
                    {
                        if (messageIndex == messages.Length - 1) return;

                        DisplayCenter(messages[messageIndex]);
                        previousMessageIndex = messageIndex;
                    }

                    await Task.Delay(1, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        private static void DisplayCenter(string message)
        {
            Win32Console.CHAR_INFO[] buffer = new Win32Console.CHAR_INFO[Win32Console.WindowWidth * Win32Console.WindowHeight];
            Win32Console.SMALL_RECT writeRegion = new()
            {
                Right = (short)(Win32Console.WindowWidth - 1),
                Bottom = (short)(Win32Console.WindowHeight - 1)
            };

            string[] lines = message.Split('\n');

            int height = lines.Length;
            int currentLine = 0;
            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                Win32Console.CursorVisible = false;
                int x = (Console.WindowWidth / 2) - (trimmedLine.Length / 2);
                int y = (Console.WindowHeight / 2) - 1 - (height / 2) + currentLine;
                Win32Console.SetCursorPosition(x, y);
                Win32Console.WriteAt(trimmedLine, ref buffer, x, y, writeRegion);
                currentLine++;
            }

            Win32Console.WriteBuffer(buffer, writeRegion);
        }

        private static int CalculateRarity(double value)
        {
            if (value <= 3.5D)
                return 5;
            else if (value <= 15D)
                return 4;
            else
                return 3;
        }
    }
}