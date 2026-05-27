using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainGeneration.TerrainGenerationPreview))]
public class TerrainGenerationPreviewEditor : Editor
{
  public override void OnInspectorGUI()
  {
    TerrainGeneration.TerrainGenerationPreview mapPreview = (TerrainGeneration.TerrainGenerationPreview)target;

    if (DrawDefaultInspector())
    {
      if (mapPreview.autoUpdate)
      {
        mapPreview.DrawMapInEditor();
      }
    }

    if (GUILayout.Button("Generate"))
    {
      mapPreview.DrawMapInEditor();
    }
  }
}
