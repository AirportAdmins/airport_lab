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
                foreach (var token in tokens)
                {
                    token.WakeUp();
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
            tokens.Add(token);
            return token;
        }
        public int Adapt(int playTimeMs)
        {
            return (int) (playTimeMs / timeFactor);
        }
        public void RemoveToken(PlayDelayToken token)
        {
            tokens.Remove(token);
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

        public void Sleep(int playTimeMs)
        {
            sleepingLeft = playTimeMs;
            var timeFactor = source.TimeFactor;
            var start = DateTime.Now;
            while (resetEvent.WaitOne(source.Adapt(sleepingLeft)))
            {
                var passed = (int) ((DateTime.Now - start).TotalMilliseconds * timeFactor);
                sleepingLeft -= passed;
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
