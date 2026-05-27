using UnityEngine;
using System.Linq;

namespace Types.TerrainGeneration
{
  [System.Serializable]
  public class TerrainLayer
  {
    public string name;
    public Color color;
    [Range(0, 1)] public float startHeight;
    [Range(0, 1)] public float blendStrength;
    [Range(0, 1)] public float colorStrength;
    public Texture2D texture;
    public float textureScale = 1f;
  }

  [CreateAssetMenu(fileName = "Texture", menuName = "Constants/TerrainGeneration/Texture")]
  public class Texture : Types.Core.UpdatableData
  {
    const int maxLayerCount = 8;

    public TerrainLayer[] layers;

    protected override void OnValidate()
    {
      if (layers == null) return;
      if (layers.Length > maxLayerCount)
      {
        System.Array.Resize(ref layers, maxLayerCount);
      }
      base.OnValidate();
    }

    public void ApplyToMaterial(Material material)
    {
      if (material == null || layers == null || layers.Length == 0) return;

      material.SetInt("layerCount", layers.Length);
      material.SetColorArray("baseColours", layers.Select(x => x.color).ToArray());
      material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
      material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
      material.SetFloatArray("baseColourStrength", layers.Select(x => x.colorStrength).ToArray());
      material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());

      Texture2DArray textureArray = GenerateTextureArray(layers.Select(x => x.texture).ToArray());
      if (textureArray != null)
      {
        material.SetTexture("baseTextures", textureArray);
      }
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
      if (material == null) return;
      material.SetFloat("minHeight", minHeight);
      material.SetFloat("maxHeight", maxHeight);
    }

    private Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
      int textureSize = 512;
      var validTexture = textures.FirstOrDefault(t => t != null);
      if (validTexture != null)
      {
        textureSize = validTexture.width;
      }

      Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, maxLayerCount, TextureFormat.RGBA32, true);

      for (int i = 0; i < maxLayerCount; i++)
      {
        if (i < textures.Length && textures[i] != null)
        {
          textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        else
        {
          Texture2D tex = new Texture2D(textureSize, textureSize);
          Color[] cols = Enumerable.Repeat(Color.white, textureSize * textureSize).ToArray();
          tex.SetPixels(cols);
          tex.Apply();
          textureArray.SetPixels(tex.GetPixels(), i);
        }
      }
      textureArray.Apply();
      return textureArray;
    }
  }
}
