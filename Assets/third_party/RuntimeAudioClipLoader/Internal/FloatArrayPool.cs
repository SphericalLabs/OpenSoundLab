using System.Collections.Generic;

namespace RuntimeAudioClipLoader.Internal
{

	public interface IFloatArrayPool
	{
		float[] Rent(int exactLength);
		void Return(float[] array);
	}

	public class FloatArrayPool : IFloatArrayPool
	{
		public static readonly FloatArrayPool Shared = new FloatArrayPool();

		class ArrayInstance
		{
			public float[] array;
			public int lastTouchedAtVersion;
		}

		int version;

		Queue<int> lastTouchedLengths = new Queue<int>();
		Dictionary<int, Queue<ArrayInstance>> lengthToArrays = new Dictionary<int, Queue<ArrayInstance>>();

		Queue<ArrayInstance> GetInstances(int exactLength)
		{
			Queue<ArrayInstance> ret = null;
			lock (lengthToArrays)
			{
				if (!lengthToArrays.TryGetValue(exactLength, out ret))
					return lengthToArrays[exactLength] = new Queue<ArrayInstance>();
			}
			return ret;
		}

		public float[] Rent(int exactLength)
		{
			version++;
			var instances = GetInstances(exactLength);
			lock (instances)
			{
				if (instances.Count > 0)
				{
					var instance = instances.Dequeue();
					return instance.array;
				}
			}
			TryClearAtMostOne();
			return new float[exactLength];
		}

		public void Return(float[] array)
		{
			version++; // TODO: handle int overflow
			var instance = new ArrayInstance()
			{
				array = array,
				lastTouchedAtVersion = version,
			};
			TryClearAtMostOne();

			var instances = GetInstances(array.Length);
			lock (instances)
				instances.Enqueue(instance);
			lock (lastTouchedLengths)
				lastTouchedLengths.Enqueue(array.Length);
		}


		void TryClearAtMostOne()
		{
			int len;
			lock (lastTouchedLengths)
			{
				if (lastTouchedLengths.Count == 0)
					return;
				len = lastTouchedLengths.Dequeue();
			}

			var instances = GetInstances(len);
			ArrayInstance instance;
			lock (instances)
			{
				if (instances.Count == 0)
					return;
				instance = instances.Dequeue();
			}

			if (instance.lastTouchedAtVersion > version - 20)
			{
				lock (instances)
					instances.Enqueue(instance);
				lock (lastTouchedLengths)
					lastTouchedLengths.Enqueue(instance.array.Length);
			}
		}

	}
}
