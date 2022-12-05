using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Pcx;

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

        meshRenderer = Drawing3dManagerGO.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = (Material)Resources.Load("Default Point", typeof(Material));
        meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/singleDisk");
        // meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/Point");
        meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.05f * Drawing3dManagerGO.transform.localScale[0]);
        // meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.05f);

        objManipulator = Drawing3dManagerGO.AddComponent<ObjectManipulator>();
        objManipulator.AllowFarManipulation = true;
        Drawing3dManagerGO.AddComponent<NearInteractionGrabbable>();
        Debug.Log("Added bounds");
    }

    public void RescalePointSize()
    {
        meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/singleDisk");
        // meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/Point");
        meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.05f * Drawing3dManagerGO.transform.localScale[0]);
    }
}
