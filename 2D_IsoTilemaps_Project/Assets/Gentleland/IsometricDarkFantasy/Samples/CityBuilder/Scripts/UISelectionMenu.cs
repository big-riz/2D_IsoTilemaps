using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISelectionMenu : MonoBehaviour
{
    bool shown = false;
    public GameObject visuals;
    float ySpacing = 110;
    public void OnClick(Transform buttonTransform)
    {
        shown = !shown;
        transform.position = buttonTransform.position;
        transform.position = new Vector3(transform.position.x, transform.position.y + ySpacing);
        visuals.SetActive(shown);

    }
}
