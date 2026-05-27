using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TerrainGeneration;

namespace Types.TerrainGeneration
{
  [CreateAssetMenu(fileName = "HeightMap", menuName = "Constants/TerrainGeneration/HeightMap")]
  public class HeightMap : Types.Core.UpdatableData
  {
    [Header("Noise Configuration")]
    public NoiseSettings noiseSettings;
    public bool useFalloff;

    [Header("Height Scaling")]
    public float heightMultiplier;
    public AnimationCurve heightCurve;

    public float minHeight => heightMultiplier * heightCurve.Evaluate(0);
    public float maxHeight => heightMultiplier * heightCurve.Evaluate(1);

#if UNITY_EDITOR
    protected override void OnValidate()
    {
      noiseSettings?.ValidateValues();
      base.OnValidate();
    }
#endif
  }
}
