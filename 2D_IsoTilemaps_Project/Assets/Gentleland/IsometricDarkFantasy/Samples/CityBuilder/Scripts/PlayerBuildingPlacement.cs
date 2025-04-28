using UnityEngine;
using UnityEngine.EventSystems;

namespace Gentleland.IsometricEnemyBase
{
    public class PlayerBuildingPlacement : MonoBehaviour
    {
        #region Inspector Variables
        [SerializeField]
        Color deleteColor = Color.red;
        [SerializeField]
        Color wrongPlacementColor = Color.red;
        [SerializeField]
        Color goodPlacementColor = Color.green;
        #endregion

        #region Selected Building
        /// <summary>
        /// reference the prefab of the selected building
        /// </summary>
        GameObject selectedBuildingPrefab;
        /// <summary>
        /// reference the instance of the selected building 
        /// </summary>
        Building selectedBuildingInstance;
        #endregion

        #region Building Placement Mode
        enum BuildingPlacementMode
        {
            Place,
            Delete
        }
        /// <summary>
        /// used to swap between placing and deleting buildings
        /// </summary>
        BuildingPlacementMode mode = BuildingPlacementMode.Place;
        #endregion

        /// <summary>
        /// reference to last building with the mouse over it 
        /// </summary>
        Building mouseOverbuilding = null;
        /// <summary>
        /// original color of the last building with the mouse over it 
        /// </summary>
        Color mouseOverbuildingColor;


        void Update()
        {
            //calculating mouse position in each space
            Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridMousePos = BuildingGrid.instance.WorldToGridPosition(worldMousePos);
            Vector3 worldMousePosOnTile = BuildingGrid.instance.GridToWorldTilePosition(gridMousePos);

            // put back the original building color if there was a building with mouse over it last frame
            if (mouseOverbuilding != null)
            {
                mouseOverbuilding.spriteRenderer.color = mouseOverbuildingColor;
            }

            // if place mode
            if (mode == BuildingPlacementMode.Place)
            {
                //nothing to do if there is no building slected
                if (selectedBuildingPrefab == null || selectedBuildingInstance == null)
                {
                    return;
                }
                //place building at mouse position while staying align with grid
                selectedBuildingInstance.gameObject.transform.position = worldMousePosOnTile;
                
                //checks if the building can be built at this position
                bool isGoodPlacement = BuildingGrid.instance.IsBuildingAtGridPositionPossible(gridMousePos, selectedBuildingInstance);
                if (isGoodPlacement)
                {   
                    //if it is the case update the color accordingly
                    selectedBuildingInstance.spriteRenderer.color = goodPlacementColor;
                    //build building on click
                    if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                    {
                        var building = Instantiate(selectedBuildingPrefab, worldMousePosOnTile, Quaternion.identity).GetComponent<Building>();
                        BuildingGrid.instance.AddBuildingAtGridPosition(gridMousePos, building);
                    }
                }
                else
                {
                    selectedBuildingInstance.spriteRenderer.color = wrongPlacementColor;
                }
                selectedBuildingInstance.transform.position = worldMousePosOnTile;
            }
            // if delete mode
            else
            {
                //updates the building with mouse over it (null if no building)
                mouseOverbuilding = BuildingGrid.instance.GetBuildingAtPos(gridMousePos);

                // if there is a building save original color and update the color 
                if (mouseOverbuilding != null)
                {
                    mouseOverbuildingColor = mouseOverbuilding.spriteRenderer.color;
                    mouseOverbuilding.spriteRenderer.color = deleteColor;

                    //delete building if clicked on it
                    if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) {
                        BuildingGrid.instance.DeleteBuilding(mouseOverbuilding);
                    }
                }
            }
        }

        /// <summary>
        /// Selects the Place mode and the given Building which allows to place it with mouse
        /// </summary>
        /// <param name="newBuildingPrefab">the building to select </param>
        public void SelectBuilding(GameObject newBuildingPrefab)
        {
            if (newBuildingPrefab == null)
            {
                Debug.LogError("Building prefab is null");
                return;
            }
            mode = BuildingPlacementMode.Place;
            if (selectedBuildingInstance != null)
            {
                Destroy(selectedBuildingInstance.gameObject);
            }
            selectedBuildingPrefab = newBuildingPrefab;
            selectedBuildingInstance = Instantiate(newBuildingPrefab).GetComponent<Building>();
            if (selectedBuildingInstance == null)
            {
                Debug.LogError("Building prefab doesn't have Building Component");
                return;
            }
        }

        /// <summary>
        /// Selects the Delete mode which allow to delete buildings by clicking on them
        /// </summary>
        public void SelectDeleteMode()
        {
            mode = BuildingPlacementMode.Delete;
            if (selectedBuildingInstance != null)
            {
                Destroy(selectedBuildingInstance.gameObject);
                selectedBuildingInstance = null;
                selectedBuildingPrefab = null;
            }
        }

    }
}