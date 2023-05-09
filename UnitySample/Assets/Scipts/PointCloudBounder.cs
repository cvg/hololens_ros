using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Pcx;

using Unity.Robotics.Visualizations;

public class PointCloudBounder : MonoBehaviour
{

    GameObject Drawing3dManagerGO;
    ObjectManipulator objManipulator;
    MeshRenderer meshRenderer;
    BoundsControl boundsControl;

    // Start is called before the first frame update
    void Start()
    {
        Drawing3dManagerGO = GameObject.Find("Drawing3dManager");
        meshRenderer = Drawing3dManagerGO.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = (Material)Resources.Load("Default Point", typeof(Material));
        meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/singleDisk");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BoundPointCloud()
    {
        Drawing3dManagerGO.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        Drawing3dManagerGO.AddComponent<BoxCollider>();
        boundsControl = Drawing3dManagerGO.AddComponent<BoundsControl>();
        UnityEvent rescale = new UnityEvent();
        rescale.AddListener(RescalePointSize);
        boundsControl.ScaleStopped = rescale;

        meshRenderer = Drawing3dManagerGO.GetComponent<MeshRenderer>();
        // meshRenderer.sharedMaterial = (Material)Resources.Load("Default Point", typeof(Material));
        // meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/singleDisk");
        // meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/Point");
        meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.05f * Drawing3dManagerGO.transform.localScale[0]);
        // meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.05f);

        objManipulator = Drawing3dManagerGO.AddComponent<ObjectManipulator>();
        objManipulator.AllowFarManipulation = true;
        Drawing3dManagerGO.AddComponent<NearInteractionGrabbable>();
        Debug.Log("Added bounds");
    }

    public void OverlayPointCloud()
    {
        DestroyComponents();

        // Reset scale of Drawing3dManagerGO
        Drawing3dManagerGO.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        // // Reset position of Drawing3dManagerGO
        Drawing3dManagerGO.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
        Drawing3dManagerGO.transform.rotation = Quaternion.identity;

        meshRenderer = Drawing3dManagerGO.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            // meshRenderer.sharedMaterial = (Material)Resources.Load("Default Point", typeof(Material));
            // meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/singleDisk");
            // meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/Point");
            meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.02f * Drawing3dManagerGO.transform.localScale[0]);
            // meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.05f);
            Debug.Log("Added overlay");
        }

    }

    void DestroyComponents() 
    {
        // Remove all the new components added from manipulator
        Destroy(Drawing3dManagerGO.GetComponent<ObjectManipulator>());
        Destroy(Drawing3dManagerGO.GetComponent<NearInteractionGrabbable>());
        Destroy(Drawing3dManagerGO.GetComponent<BoxCollider>());
        Destroy(Drawing3dManagerGO.GetComponent<BoundsControl>());
        // Destroy(Drawing3dManagerGO.GetComponent<MeshRenderer>());
        Destroy(Drawing3dManagerGO.GetComponent<ConstraintManager>());
    }

    public void RescalePointSize()
    {
        meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/singleDisk");
        // meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/Point");
        meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.05f * Drawing3dManagerGO.transform.localScale[0]);
    }
}
