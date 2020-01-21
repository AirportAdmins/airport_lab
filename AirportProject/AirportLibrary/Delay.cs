using System;
using System.Collections.Generic;
using System.Threading;

namespace AirportLibrary.Delay
{
    public class PlayDelaySource
    {
        List<PlayDelayToken> tokens = new List<PlayDelayToken>();

        double timeFactor;
        public double TimeFactor
        {
            get => timeFactor;
            set
            {
                timeFactor = value;
                lock (tokens)
                {
                    foreach (var token in tokens)
                    {
                        token.WakeUp();
                    }
                }
            }
        }

        public PlayDelaySource(double timeFactor)
        {
            TimeFactor = timeFactor;
        }

        public PlayDelayToken CreateToken()
        {
            var autoResetEvent = new AutoResetEvent(false);
            var token = new PlayDelayToken(this);
            lock (tokens)
            {
                tokens.Add(token);
            }
            return token;
        }
        public int Adapt(int playTimeMs)
        {
            //Console.WriteLine("Real time sleep left: {0}", (int)(playTimeMs / timeFactor));
            return (int) (playTimeMs / timeFactor);
        }
        public void RemoveToken(PlayDelayToken token)
        {
            lock (tokens)
            {
                tokens.Remove(token);
            }
        }
    }

    public class PlayDelayToken
    {
        AutoResetEvent resetEvent = new AutoResetEvent(false);
        PlayDelaySource source;
        int sleepingLeft;

        public PlayDelayToken(PlayDelaySource source)
        {
            this.source = source;
        }

        public void Sleep(int playTimeMs)//, DateTime stdt)
        {
            sleepingLeft = playTimeMs;
            var timeFactor = source.TimeFactor;
            var start = DateTime.Now;
            //Console.WriteLine("Play time of waking: {0}", stdt.AddMilliseconds(sleepingLeft).ToLongTimeString());
            while (resetEvent.WaitOne(source.Adapt(sleepingLeft)))
            {
                var real = (int) (DateTime.Now - start).TotalMilliseconds;
                start = DateTime.Now;
                var passed = (int) (real * timeFactor);
                timeFactor = source.TimeFactor;
                //Console.WriteLine("Passed {0} of real and {1} of play time", real, passed);
                sleepingLeft -= passed;
                //stdt = stdt.AddMilliseconds(passed);
                //Console.WriteLine("Play time of waking: {0}", stdt.AddMilliseconds(sleepingLeft).ToLongTimeString());
                if (sleepingLeft <= 0)
                    break;
            }
            source.RemoveToken(this);
        }

        public void WakeUp()
        {
            resetEvent.Set();
        }
    }
}
