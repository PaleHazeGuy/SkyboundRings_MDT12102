using System.Collections;
using System.Collections.Generic;
using Types.TerrainGeneration;
using Unity.VisualScripting;
using UnityEngine;

namespace TerrainGeneration
{
  public class TerrainGenerator : MonoBehaviour
  {
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    [Header("LOD Settings")]
    public int colliderLODIndex;
    public LODInfo[] detailLevels;

    [Header("Configuration Asset Settings")]
    public Types.TerrainGeneration.Mesh meshSettings;
    public Types.TerrainGeneration.HeightMap heightMapSettings;
    public Types.TerrainGeneration.Texture textureSettings;

    [Header("Scene References")]
    public Transform viewer;
    public Material mapMaterial;

    private Vector2 viewerPosition;
    private Vector2 viewerPositionOld;
    private float meshWorldSize;
    private int chunksVisibleInViewDst;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    void Start()
    {
      textureSettings.ApplyToMaterial(mapMaterial);
      textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

      float maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
      meshWorldSize = meshSettings.meshWorldSize;
      chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);

      UpdateVisibleChunks();
    }

    void Update()
    {
      viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

      if (viewerPosition != viewerPositionOld)
      {
        for (int i = 0; i < visibleTerrainChunks.Count; i++)
        {
          visibleTerrainChunks[i].UpdateCollisionMesh();
        }
      }

      if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
      {
        viewerPositionOld = viewerPosition;
        UpdateVisibleChunks();
      }
    }

    void UpdateVisibleChunks()
    {
      HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();

      for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--)
      {
        alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
        visibleTerrainChunks[i].UpdateTerrainChunk();
      }

      int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
      int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

      for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
      {
        for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
        {
          Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

          if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
          {
            if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
            {
              terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
            }
            else
            {
              TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
              terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
              newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
              newChunk.Load();
            }
          }
        }
      }
    }

    void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
    {
      if (isVisible)
      {
        if (!visibleTerrainChunks.Contains(chunk))
        {
          visibleTerrainChunks.Add(chunk);
        }
      }
      else
      {
        visibleTerrainChunks.Remove(chunk);
      }
    }
  }

  [System.Serializable]
  public struct LODInfo
  {
    [Range(0, Types.TerrainGeneration.Mesh.numSupportedLODs - 1)]
    public int lod;
    public float visibleDstThreshold;

    public float sqrVisibleDstThreshold
    {
      get
      {
        return visibleDstThreshold * visibleDstThreshold;
      }
    }
  }


}
