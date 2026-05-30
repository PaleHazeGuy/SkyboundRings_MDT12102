using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace TerrainGeneration
{
  public class ThreadedDataRequester : MonoBehaviour
  {
    private static ThreadedDataRequester instance;

    private Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();

    void Awake()
    {
      instance = FindObjectOfType<ThreadedDataRequester>();
      if (instance == null)
      {
        Debug.LogError("No ThreadedDataRequester found in scene!");
      }
    }

    public static void RequestData(Func<object> generateData, Action<object> callback)
    {
      ThreadStart threadStart = delegate
      {
        instance.DataThread(generateData, callback);
      };

      new Thread(threadStart).Start();
    }

    void DataThread(Func<object> generateData, Action<object> callback)
    {
      object data = generateData();
      lock (dataQueue)
      {
        dataQueue.Enqueue(new ThreadInfo(callback, data));
      }
    }

    void Update()
    {
      lock (dataQueue)
      {
        while (dataQueue.Count > 0)
        {
          ThreadInfo threadInfo = dataQueue.Dequeue();
          threadInfo.callback(threadInfo.parameter);
        }
      }
    }

    struct ThreadInfo
    {
      public readonly Action<object> callback;
      public readonly object parameter;

      public ThreadInfo(Action<object> callback, object parameter)
      {
        this.callback = callback;
        this.parameter = parameter;
      }
    }
  }
}
