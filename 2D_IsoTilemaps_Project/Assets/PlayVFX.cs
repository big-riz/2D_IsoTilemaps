using UnityEngine;
using UnityEngine.VFX;
public class PlayVFX : MonoBehaviour
{
    public VisualEffect vfx;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        vfx.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
