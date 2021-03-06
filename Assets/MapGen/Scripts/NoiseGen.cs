﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// this is not on an object so it does not need to inherit monobehavior
// static because we only need one instance of this in our world
public static class NoiseGen
{
    public enum NormalizeMode { Local,Global};//determining how we want to normalize our noise map local is for a single mesh when you know it will be generating the whole world global is for by chunk
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];


        System.Random prng = new System.Random(seed);//psuedo random number generator
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;


        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;//gens random from 100000 -100000 if too high the gen returns the same number
            float offsetY = prng.Next(-100000, 100000) - offset.y;//offset is a user based offset that is added to the random
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }
         

        if (scale <= 0)
        {
            scale = .00001f;
        }


        float maxLocalNoiseHeight = float.MinValue;//tracks highesst height
        float minLocalNoiseHeight = float.MaxValue;//tracks lowest height

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for( int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency ;//the higher the frequency the further the sample points will be therefore the height values will change more rapidly
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency ;//halfwidth is to allow the scale to zoom to the center instead of the top right
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY)*2 -1;//multiply by 2 then subtract by 1 to allow for negative values in the height
                    noiseHeight += perlinValue * amplitude;
                    amplitude *= persistance;//persistance 0-1 so it decreases the octave
                    frequency *= lacunarity;//lacunarity increases the octave
                }
                if(noiseHeight > maxLocalNoiseHeight)//if current height greater than previous set to be max
                {
                    maxLocalNoiseHeight = noiseHeight;
                }
                else if(noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight;
            }
        }


        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);//returns a value from 0-1 if equal to minnoise height returns 0 if = maxheight return 1 anywhere in between .5

                }
                else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) /(maxPossibleHeight);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight,0,int.MaxValue);
                }
            }

        }

        return noiseMap;
    }
}
