﻿using System;
using System.Collections;
using System.Collections.Generic;
using CulverinEditor;
using CulverinEditor.Debug;
using CulverinEditor.Map;

public class LevelMap : CulverinBehaviour
{
    public GameObject map_obj;
    private int[,] level_map;
    public int map_width = 0;
    public int map_height = 0;

    //[Obsolete("Change: Level_map.map[index, index2] -> Leve_map[index, index2]", false)]
    public int[,] map
    {
        get
        {
            return level_map;
        }
    }

    /// <summary>  
    ///     Like int[,] map but this return a number and check if outside array! 
    ///     Change: Level_map.map[index, index2] -> Leve_map[index, index2]
    /// </summary>  
    /// 
    public int this[int index, int index2]
    {
        get
        {
            if (index >= 0 && index2 >= 0 && index < map_width && index2 < map_height)
                return level_map[index, index2];
            return 1;
        }
    }

    void Start()
    {
        map_width = Map.GetWidthMap();
        map_height = Map.GetHeightMap();

        level_map = new int[map_width, map_height];
        for (int y = 0; y < map_height; y++)
        {
            for (int x = 0; x < map_width; x++)
            {
                level_map[x, y] = 0;
            }
        }

        string map_string = Map.GetMapString();

        int t = 0;
        for (int y = 0; y < map_height; y++)
        {
            for (int x = 0; x < map_width; x++)
            {
                level_map[x, y] = int.Parse(map_string[t].ToString());
                t += 1;
            }
        }
    }

    /// <summary>
    /// Value == 0 -> Walkable
    /// Value == 1 -> No Walkable
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="value"></param>
    /// 
    public void UpdateMap(int x, int y, int value)
    {
        level_map[x, y] = value;
    }

    public int GetValue(int x, int y)
    {
        return level_map[x, y];
    }

    public void GetPositionByeValue(out int x, out int y, int value)
    {
        for (int j = 0; j < map_height; j++)
        {
            for (int i = 0; i < map_width; i++)
            {
                if (level_map[i, j] == value)
                {
                    x = i;
                    y = j;

                    return;
                }
            }
        }

        x = 0;
        y = 0;
    }

    public bool GodModeWalkability(int x, int y)
    {
        return (x >= 0 && y >= 0 && x < map_width && y < map_height);
    }
}