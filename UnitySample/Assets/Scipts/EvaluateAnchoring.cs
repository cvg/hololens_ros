using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.Utilities;
using Pcx;

using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Sensor;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;
using RosMessageTypes.Nav;

using Unity.Robotics.Visualizations;

public class EvaluateAnchoring : MonoBehaviour
{
    public GameObject example_anchor;
    GameObject[] real_anchor;
    GameObject[] hl_anchor;

    int num_anchors = 8;

    public ConfigReader configReader;
    ROSConnection ros;

    IEnumerator Start() {
        yield return new WaitUntil(() => configReader.FinishedReader);   

        num_anchors = configReader.num_anchors;
        real_anchor = new GameObject[num_anchors];
        hl_anchor = new GameObject[num_anchors];

        // Set up GameObjects
        for (int i = 0; i < num_anchors; i++) {
            // Clone example_anchor
            GameObject new_real_anchor = Instantiate(example_anchor);
            new_real_anchor.name = "real_anchor_" + i.ToString();
            GameObject new_hl_anchor = Instantiate(example_anchor);
            new_hl_anchor.name = "hl_anchor_" + i.ToString();

            real_anchor[i] = new_real_anchor;
            hl_anchor[i] = new_hl_anchor;

            // hide
            new_real_anchor.SetActive(false);
            new_hl_anchor.SetActive(false);
        }

        // Set up publisher to publish transforms of anchors
        ros = ROSConnection.GetOrCreateInstance();
        // Publisher should be able to handle multiple transforms at once
        ros.RegisterPublisher<TransformStampedMsg>("unity_anchor_transform", configReader.overlay_alignment_tr, 1);

    }

    public void AlignmentMode() {
        // Set up GameObjects to be visible and interactable

    }

    public void ConfirmAlignment() {
        // Set up a button for user to confirm alignment

        // Set up a publisher to publish the alignment transform of all anchors

    }
}