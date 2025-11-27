using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using GameFramework.Resource;

namespace StarForce
{
    public class LevelLoader
    {
        [System.Serializable]
        public class LevelData
        {
            public int levelId;
            public string instruction;
            public int[][] map; // Unity JsonUtility doesn't support multidimensional arrays directly, need a wrapper or use List
            public int enemyHp;
        }

        // Wrapper for row data to handle 2D array in JSON
        [System.Serializable]
        public class RowData
        {
            public int[] row;
        }

        [System.Serializable]
        public class LevelDataWrapper
        {
            public int levelId;
            public string instruction;
            public RowData[] mapRows;
            public int enemyHp;
        }

        public static void LoadLevel(int levelId)
        {
            string filePath = Application.dataPath + $"/GameMain/Configs/Levels/Level_{levelId}.json";
            
            if (!System.IO.File.Exists(filePath))
            {
                Log.Error($"Level file not found: {filePath}");
                return;
            }

            string json = System.IO.File.ReadAllText(filePath);
            ParseAndLoad(json);
        }

        private static void ParseAndLoad(string json)
        {
            LevelDataWrapper data = JsonUtility.FromJson<LevelDataWrapper>(json);
            
            if (data == null)
            {
                Log.Error("Failed to parse level data.");
                return;
            }

            // Convert RowData[] to int[,]
            int rows = data.mapRows.Length;
            int cols = data.mapRows[0].row.Length;
            int[,] map = new int[rows, cols];

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    map[y, x] = data.mapRows[y].row[x];
                }
            }

            // Initialize Map
            MapManager.Instance.InitLevel(map);

            // Spawn Entities based on map data
            // 2: Player, 3: Enemy
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    int type = map[y, x];
                    if (type == 2)
                    {
                        // Player
                        MapManager.Instance.AddEntity(100, 0, x, y);
                        GameEntry.Entity.ShowEntity(100, typeof(JellyLogic), "Assets/GameMain/Entities/Jelly.prefab", "JellyGroup", MapManager.Instance.m_Entities[100]);
                        // Clear spawn point from grid so it's walkable
                        // Actually MapManager.InitLevel already set it to 2, we might want to set it to 0 (empty) in the grid logic if 2 is considered an obstacle?
                        // In JellyMap.CalculateSlide, we check for 1 (Wall) and 4 (Cracker). 2 and 3 are just spawn points, so they should be treated as empty 0 for collision purposes, 
                        // BUT we need to make sure we don't overwrite the visual if we want to show "spawn point" graphics? 
                        // For now, let's assume the grid logic treats anything not 1 or 4 as walkable.
                    }
                    else if (type == 3)
                    {
                        // Enemy
                        // We can generate unique IDs for multiple enemies
                        int enemyId = 200 + x + y * 10; 
                        MapManager.Instance.AddEntity(enemyId, 1, x, y);
                        // Set HP from level data
                        MapManager.Instance.m_Entities[enemyId].Hp = data.enemyHp;
                        
                        GameEntry.Entity.ShowEntity(enemyId, typeof(JellyLogic), "Assets/GameMain/Entities/Jelly.prefab", "JellyGroup", MapManager.Instance.m_Entities[enemyId]);
                    }
                }
            }
            
            Log.Info($"Level {data.levelId} Loaded: {data.instruction}");
        }
    }
}
