using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainGeneration
{
  public class TerrainGenerationPreview : MonoBehaviour
  {
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public Types.TerrainGeneration.Mesh meshSettings;
    public Types.TerrainGeneration.HeightMap heightMapSettings;
    public Types.TerrainGeneration.Texture textureData;

    public Material terrainMaterial;

    [Range(0, Types.TerrainGeneration.Mesh.numSupportedLODs - 1)]
    public int editorPreviewLOD;
    public bool autoUpdate;

    public void DrawMapInEditor()
    {
      if (textureData != null)
      {
        textureData.ApplyToMaterial(terrainMaterial);
        if (heightMapSettings != null)
        {
          textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        }
      }

      if (meshSettings == null || heightMapSettings == null) return;

      HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);

      DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLOD));
    }

    public void DrawMesh(MeshData meshData)
    {
      meshFilter.sharedMesh = meshData.CreateMesh();
      meshFilter.gameObject.SetActive(true);
    }

    void OnValuesUpdated()
    {
      if (!Application.isPlaying)
      {
        DrawMapInEditor();
      }
    }

    void OnTextureValuesUpdated()
    {
      if (!Application.isPlaying && textureData != null)
      {
        textureData.ApplyToMaterial(terrainMaterial);
      }
    }

    void OnValidate()
    {
      if (meshSettings != null)
      {
        meshSettings.OnValuesUpdated -= OnValuesUpdated;
        meshSettings.OnValuesUpdated += OnValuesUpdated;
      }
      if (heightMapSettings != null)
      {
        heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
        heightMapSettings.OnValuesUpdated += OnValuesUpdated;
      }
      if (textureData != null)
      {
        textureData.OnValuesUpdated -= OnTextureValuesUpdated;
        textureData.OnValuesUpdated += OnTextureValuesUpdated;
      }
    }
  }
}
