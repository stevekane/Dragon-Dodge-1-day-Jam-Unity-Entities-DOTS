using System;
using Unity.Entities;

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
}