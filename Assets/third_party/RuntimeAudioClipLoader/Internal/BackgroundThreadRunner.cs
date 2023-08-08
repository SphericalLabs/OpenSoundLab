using System;
using System.Collections.Generic;
using System.Threading;

namespace RuntimeAudioClipLoader.Internal
{
	public interface IActionRunner
	{
		void Enqueue(Action action);
	}

	public class BackgroundThreadRunner : IActionRunner
	{
		static BackgroundThreadRunner shared;
		public static BackgroundThreadRunner Shared
		{
			get
			{
				if (shared == null)
					shared = new BackgroundThreadRunner();
				return shared;
			}
		}

		Queue<Action> toRun = new Queue<Action>();
		Thread backgroundThread;

		public void Enqueue(Action action)
		{
			lock (toRun)
				toRun.Enqueue(action);

			msToSleepIfUnused = msToSleepIfUnusedDefault;

			if (backgroundThread == null || backgroundThread.IsAlive == false)
			{
				backgroundThread = new Thread(BackgroundThreadMain);
				backgroundThread.Priority = ThreadPriority.Highest;
				backgroundThread.IsBackground = true;
				backgroundThread.Start();
			}
		}

		const int msToSleepIfUnusedDefault = 30000;
		volatile int msToSleepIfUnused = msToSleepIfUnusedDefault;

		void BackgroundThreadMain()
		{
			Action task = null;
			while (msToSleepIfUnused > 0)
			{
				lock (toRun)
				{
					if (toRun.Count > 0)
						task = toRun.Dequeue();
				}
				if (task != null)
				{
					task.Invoke();
					task = null;
				}
				else
				{
					var toSleep = (msToSleepIfUnusedDefault - msToSleepIfUnused) / 10;
					toSleep = Math.Max(10, Math.Min(toSleep, 2000));
					Thread.Sleep(toSleep);
					msToSleepIfUnused -= toSleep;
				}
			}
		}
	}
}
