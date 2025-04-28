using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gentleland.IsometricEnemyBase
{
    [RequireComponent(typeof(Grid))]
    public class BuildingGrid : MonoBehaviour
    {
        public static BuildingGrid instance;

        Dictionary<Vector2Int,Building> tilesOccupiedBybuildings= new Dictionary<Vector2Int, Building>();
        
        Grid grid;
        
        void Awake()
        {
            if (instance != null)
            {
                Debug.LogError("Can't Have multiple instances of BuildingGrid");
            }
            instance = this;
            grid = GetComponent<Grid>();
        }

        /// <summary>
        /// Add a Building to the tilesOccupiedBybuildings dictionary 
        /// for each grid position occupied by the building (using building.gridSize)
        /// </summary>
        /// <param name="gridPos">the position on the grid where the building should be added</param>
        /// <param name="building">the building to add</param>
        public void AddBuildingAtGridPosition(Vector2Int gridPos, Building building)
        {
            Vector2Int buildingSize = building.gridSize;
            for (int y = 0; y < buildingSize.y; ++y)
            {
                for (int x = 0; x < buildingSize.x; ++x)
                {
                    tilesOccupiedBybuildings.Add(new Vector2Int(gridPos.x + x, gridPos.y + y), building);
                }
            }
        }

        /// <summary>
        /// Returns true if the building is not overlapping with any building in tilesOccupiedBybuildings
        /// else returns false
        /// </summary>
        /// <param name="gridPos">the position on the grid where to check if the building can be built</param>
        /// <param name="building">the building</param>
        public bool IsBuildingAtGridPositionPossible(Vector2Int gridPos, Building building)
        {
            Vector2Int buildingSize = building.gridSize;
            for (int y = 0; y < buildingSize.y; ++y)
            {
                for (int x = 0; x < buildingSize.x; ++x)
                {
                    if (tilesOccupiedBybuildings.ContainsKey(new Vector2Int(gridPos.x + x, gridPos.y + y)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Remove the building from tilesOccupiedBybuildings dictionary and delete the building gameObject
        /// </summary>
        /// <param name="building">the building to remove</param>
        public void DeleteBuilding(Building building)
        {
            foreach (var toDelete in tilesOccupiedBybuildings.Where(p => p.Value == building).ToList())
            {
                tilesOccupiedBybuildings.Remove(toDelete.Key);
            }
            Destroy(building.gameObject);
        }
        /// <summary>
        /// returns the building at the given grid position 
        /// returns null if there is no building at this position
        /// </summary>
        /// <param name="gridPos">the position on the grid</param>
        public Building GetBuildingAtPos(Vector2Int gridPos)
        {
            Building b=null;
            tilesOccupiedBybuildings.TryGetValue(gridPos, out b);
            return b;
        }

        /// <summary>
        /// Converts a world space position to grid space position
        /// </summary>
        /// <param name="pos">the position in world space to convert in grid space </param>
        public Vector2Int WorldToGridPosition(Vector3 pos)
        {
            Vector3Int v3= grid.WorldToCell(pos);
            return new Vector2Int(v3.x, v3.y);
        }

        /// <summary>
        /// Converts a grid space position to world space position
        /// </summary>
        /// <param name="gridPos">the position in grid space to convert in world space</param>
        public Vector3 GridToWorldTilePosition(Vector2Int gridPos)
        {
            Vector3 v = grid.CellToWorld(new Vector3Int(gridPos.x,gridPos.y,0));
            return v;
        }


    }
}