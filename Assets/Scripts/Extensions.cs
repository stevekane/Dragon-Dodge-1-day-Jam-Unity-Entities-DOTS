using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public static class Extensions {
  public static bool Contains<T>(
  this DynamicBuffer<T> xs,
  in T x)
  where T : struct, IEquatable<T> {
    for (int i = 0; i < xs.Length; i++) {
      if (xs[i].Equals(x)) {
        return true;
      }
    }
    return false;
  }

  public static bool Remove<T>(
  this DynamicBuffer<T> xs,
  in T x) 
  where T : struct {
    for (int i = 0; i < xs.Length; i++) {
      if (xs[i].Equals(x)) {
        xs.RemoveAtSwapBack(i);
        return true;
      }
    }
    return false;
  }

  public static void Shuffle<T>(
  this DynamicBuffer<T> xs, 
  in uint seed) 
  where T : struct {  
    var rng = new Random(seed);
    var n = xs.Length;  

    while (n > 1) {
      n--;  
      int k = rng.NextInt(n + 1);  
      T value = xs[k];  
      xs[k] = xs[n];
      xs[n] = value;  
    }  
  }

  public static float3 ToWorldPosition(
  this int2 tp) {
    return new float3(tp.x, 0, tp.y);
  }

  public static int2 ToBoardPosition(
  this float3 v) {
    return new int2(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.z));
  }

  public static quaternion ToWorldRotation(
  this CardinalRotation cardinalRotation) {
    return Quaternion.AngleAxis(90 * (int)cardinalRotation, Vector3.up);
  }

  public static bool InBounds(
  this int2 v,
  in int2 min,
  in int2 max) {
    return v.x >= min.x && v.y >= min.y && v.x <= max.x && v.y <= max.y;
  }
}