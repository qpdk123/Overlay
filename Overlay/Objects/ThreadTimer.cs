using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Overlay.Objects
{
    public partial class ThreadTimer
    {
        private readonly object _locker = new object();

        private volatile bool isRun = default(bool);
        private int interval = default(int);


        public bool IsRun { get => isRun; private set => isRun = value; }
        public int Interval { get => interval; set => interval = value; }

        public delegate void TimerHandler();
        public event TimerHandler Tick;
        public event TimerHandler Started;
        public event TimerHandler Stopped;

        public delegate void ExceptionHandler(Exception e);
        public event ExceptionHandler ExceptionOccured;

        private Thread thread = default(Thread);

        public ThreadTimer(int interval)
        {
            this.interval = interval;
        }

        public void Run()
        {
            thread = new Thread(ThreadProc);
            this.isRun = true;
            thread.Start();
        }

        private void ThreadProc()
        {
            try
            {
                if (this.Started != default(TimerHandler))
                {
                    this.Started.Invoke();
                }

                while (this.isRun)
                {
                    if (this.Tick != default(TimerHandler))
                    {
                        this.Tick.Invoke();
                    }
                    Thread.Sleep(this.interval);
                }
            }
            catch (Exception e)
            {
                if (e is ThreadInterruptedException == false)
                {
                    if (this.ExceptionOccured != default(ExceptionHandler))
                    {
                        this.ExceptionOccured.Invoke(e);
                    }
                }
            }
            finally
            {
                if (this.Stopped != default(TimerHandler))
                {
                    this.Stopped.Invoke();
                }
            }
        }

        public void Stop()
        {
            if (this.thread != default(Thread))
            {
                try
                {
                    this.isRun = false;
                    if (this.thread.Join(this.interval) == false)
                    {
                        this.thread.Interrupt();
                    }
                }
                catch (Exception e)
                {
                    if (this.ExceptionOccured != default(ExceptionHandler))
                    {
                        this.ExceptionOccured.Invoke(e);
                    }
                }
            }
        }
    }
}
