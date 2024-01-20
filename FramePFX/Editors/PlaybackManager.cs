using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using FramePFX.Editors.Timelines;
using FramePFX.Utils;
using OpenTK.Graphics.OpenGL;

namespace FramePFX.Editors {
    /// <summary>
    /// An event sent by a <see cref="PlaybackManager"/>
    /// <param name="sender">The playback manager</param>
    /// <param name="state">The new state. Play may be repeatedly sent</param>
    /// <param name="frame">The starting frame when playing, the current frame when pausing, and the last play or jump frame when stopping</param>
    /// </summary>
    public delegate void PlaybackStateEventHandler(PlaybackManager sender, PlaybackState state, long frame);

    /// <summary>
    /// A class that manages the playback functionality of the editor, which manages the timer and play/pause/stop states
    /// </summary>
    public class PlaybackManager {
        private static readonly long THREAD_SPLICE_IN_TICKS = (long) (16.4d * Time.TICK_PER_MILLIS);
        private static readonly long YIELD_MILLIS_IN_TICKS = Time.TICK_PER_MILLIS / 10;

        // thread stuff
        private volatile bool thread_IsPlaying;
        private volatile bool thread_IsTimerRunning;
        private long intervalTicks;
        private long nextTickTime;
        private Thread thread;

        // regular state stuff
        private long lastPlayFrame;

        public PlaybackState PlaybackState { get; private set; } = PlaybackState.Stop;

        public Timeline Timeline { get; }

        /// <summary>
        /// An event fired when the play, pause or stop methods are called, if the current playback state does not match the matching function
        /// </summary>
        public event PlaybackStateEventHandler PlaybackStateChanged;

        public PlaybackManager(Timeline timeline) {
            this.Timeline = timeline;
        }

        public void StartTimer() {
            if (this.thread != null && this.thread.IsAlive) {
                throw new InvalidOperationException("Timer thread already running");
            }

            this.thread = new Thread(this.TimerMain) {
                IsBackground = true,
                Name = "Timer Thread"
            };

            this.thread_IsTimerRunning = true;
            this.thread.Start();
        }

        public void StopTimer() {
            this.thread_IsTimerRunning = false;
            this.thread?.Join();
        }

        public void SetFrameRate(double frameRate) {
            this.intervalTicks = (long) Math.Round(1000.0 / frameRate * Time.TICK_PER_MILLIS);
        }

        public void Play(long frame) {
            if (this.PlaybackState == PlaybackState.Play && this.Timeline.PlayHeadPosition == frame) {
                return;
            }

            this.lastPlayFrame = frame;
            this.PlaybackState = PlaybackState.Play;
            this.PlaybackStateChanged?.Invoke(this, PlaybackState.Play, frame);
            this.thread_IsPlaying = true;
        }

        public void Pause() {
            if (this.PlaybackState != PlaybackState.Play) {
                return;
            }

            this.lastPlayFrame = this.Timeline.PlayHeadPosition;
            this.PlaybackState = PlaybackState.Pause;
            this.PlaybackStateChanged?.Invoke(this, PlaybackState.Pause, this.lastPlayFrame);
            this.thread_IsPlaying = false;
        }

        public void Stop() {
            if (this.PlaybackState != PlaybackState.Play) {
                return;
            }

            this.PlaybackState = PlaybackState.Stop;
            this.PlaybackStateChanged?.Invoke(this, PlaybackState.Stop, this.lastPlayFrame);
            this.Timeline.PlayHeadPosition = this.lastPlayFrame;
            this.thread_IsPlaying = false;
        }

        private void OnTimerFrame() {
            Application.Current.Dispatcher.Invoke(() => {
                if (this.thread_IsPlaying)
                    this.Timeline.PlayHeadPosition++;
            });
        }

        private void TimerMain() {
            do {
                while (!this.thread_IsPlaying) {
                    Thread.Sleep(50);
                }

                long target = this.nextTickTime;
                while ((target - Time.GetSystemTicks()) > THREAD_SPLICE_IN_TICKS)
                    Thread.Sleep(1);
                while ((target - Time.GetSystemTicks()) > YIELD_MILLIS_IN_TICKS)
                    Thread.Yield();

                // CPU intensive wait
                long time = Time.GetSystemTicks();
                while (time < target) {
                    Thread.SpinWait(8);
                    time = Time.GetSystemTicks();
                }

                this.nextTickTime = Time.GetSystemTicks() + this.intervalTicks;
                if (this.thread_IsPlaying) {
                    this.OnTimerFrame();
                }
            } while (this.thread_IsTimerRunning);

            // long frameEndTicks = Time.GetSystemTicks();
            // while (this.thread_IsTimerRunning) {
            //     while (!this.thread_IsPlaying) {
            //         Thread.Sleep(50);
            //     }
            //     do {
            //         long ticksA = Time.GetSystemTicks();
            //         long interval = ticksA - frameEndTicks;
            //         if (interval >= this.intervalTicks)
            //             break;
            //         Thread.Sleep(1);
            //     } while (true);
            //     if (this.thread_IsPlaying) {
            //         try {
            //             this.OnTimerFrame();
            //         }
            //         catch (Exception e) {
            //             // Don't crash the timer thread in release, just ignore it (or break in release)
            //             #if DEBUG
            //             System.Diagnostics.Debugger.Break();
            //             #endif
            //         }
            //     }
            //     frameEndTicks = Time.GetSystemTicks();
            // }
        }
    }
}