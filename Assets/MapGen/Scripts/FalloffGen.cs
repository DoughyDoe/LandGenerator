using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FalloffGen 
{
    public static float[,] GenerateFalloffMap(int size)
    {
        float[,] map = new float[size, size];
        for (int i = 0;  i < size; i++)
        {
            for (int j = 0; j < size; j++)// i and j are coordinates on our map
            {
                float x = i / (float)size*2-1;// *2 -1 to make it be -1 to 1 without it it is 0 -1 
                float y = j / (float)size * 2 - 1;

                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));// this is to find out which one is closer to 1
                map[i, j] = Evaluate(value);
            }

        }
        return map;
    }

    static float Evaluate(float value)
    {
        float a = 3;
        float b = 2.2f;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
    
}
