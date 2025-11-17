using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class VertexPointUtil
{
    public static Vector2 GeometricMedian(List<Vector2> points, int maxIter = 100, float tol = 1e-4f)
    {
        Vector2 guess = points.Aggregate(Vector2.zero,(a,b) => a + b) / points.Count;
        for (int i = 0; i < maxIter; i++)
        {
            Vector2 num = Vector2.zero;
            float denom = 0f;
            foreach (var p in points)
            {
                float dist = Vector2.Distance(guess, p);
                if (dist > tol)
                {
                    num += p / dist;
                    denom += 1f / dist;
                }
            }
            var newGuess = num / denom;
            if ((newGuess - guess).magnitude < tol) break;
            guess = newGuess;
        }
        return guess;
    }
    
    public static List<Vector2> ConvertListVectorToVector2(List<List<Vector2>> listVector) => listVector.SelectMany(x => x).ToList();
}
