using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeAudioClipLoader.Internal
{
	public interface IActionAndCoroutineRunner : IActionRunner
	{
		void Enqueue(IEnumerator coroutine);
	}

	[ExecuteInEditMode]
	public class UnityMainThreadRunner : MonoBehaviour, IActionAndCoroutineRunner
	{
		static UnityMainThreadRunner shared;
		public static UnityMainThreadRunner Shared
		{
			get
			{
				if (shared == null)
					shared = CreateShared();
				return shared;
			}
		}

		[SerializeField]
		bool isShared;

		Queue<Action> actionsToInvoke = new Queue<Action>();
		Queue<IEnumerator> coroutinesToStart = new Queue<IEnumerator>();

		public void Enqueue(Action action)
		{
			if (action == null)
				return;

			lock (actionsToInvoke)
				actionsToInvoke.Enqueue(action);
		}

		public void Enqueue(IEnumerator coroutine)
		{
			if (coroutine == null)
				return;

			lock (coroutinesToStart)
				coroutinesToStart.Enqueue(coroutine);
		}

		void OnEnable()
		{
			if (isShared)
			{
				if (shared == null)
					shared = this;
				else
					GameObject.Destroy(gameObject);
			}
		}

		void Update()
		{
			ProcessQueues();
		}

		void ProcessQueues()
		{
			Action action = null;
			while (true)
			{
				lock (actionsToInvoke)
				{
					if (actionsToInvoke.Count == 0) break;
					action = actionsToInvoke.Dequeue();
				}
				action.Invoke();
			}

			IEnumerator coroutine = null;
			while (true)
			{
				lock (coroutinesToStart)
				{
					if (coroutinesToStart.Count == 0) break;
					coroutine = coroutinesToStart.Dequeue();
				}
				StartCoroutine(coroutine);
			}
		}

#if UNITY_5_3_OR_NEWER
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
		static void OnRuntimeMethodLoad()
		{
			if (shared == null)
				shared = CreateShared();
		}

		static UnityMainThreadRunner CreateShared()
		{
			try
			{
				var name = "Holder of " + typeof(UnityMainThreadRunner) + ".Shared";
				var go = new GameObject(name);
				GameObject.DontDestroyOnLoad(go);
				go.hideFlags = HideFlags.DontSave;// | HideFlags.HideInHierarchy;
				var behaviour = go.AddComponent<UnityMainThreadRunner>();
				behaviour.isShared = true;
				MonoBehaviour.DontDestroyOnLoad(behaviour);

				return behaviour;
			}
			catch (Exception e)
			{
				Debug.LogError(typeof(UnityMainThreadRunner) + " probably tried to be created in non Unity thread: " + e);
				return null;
			}
		}

	}
}