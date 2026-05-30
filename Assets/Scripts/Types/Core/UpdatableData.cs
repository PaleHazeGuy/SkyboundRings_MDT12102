using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Types.Core
{
  public class UpdatableData : ScriptableObject
  {
    public event System.Action OnValuesUpdated;
    public bool autoUpdate;
    protected virtual void OnValidate()
    {
#if UNITY_EDITOR
      if (autoUpdate)
      {
        UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
        UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
      }
#endif
    }

    public void NotifyOfUpdatedValues()
    {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
#endif
      OnValuesUpdated?.Invoke();
    }
  }
}
