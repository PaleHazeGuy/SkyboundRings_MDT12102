using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Types.Core.UpdatableData), true)]
public class UpdatableDataEditor : Editor
{
  public override void OnInspectorGUI()
  {
    base.OnInspectorGUI();

    Types.Core.UpdatableData data = (Types.Core.UpdatableData)target;

    if (GUILayout.Button("Update"))
    {
      data.NotifyOfUpdatedValues();
      EditorUtility.SetDirty(target);
    }
  }
}
