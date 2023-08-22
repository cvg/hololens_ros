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
    GameObject example_anchor;
    GameObject example_hl_anchor;
    GameObject[] real_anchors;
    GameObject[] hl_anchors;

    int num_anchors = 8;

    public ConfigReader configReader;
    ROSConnection ros;


    IEnumerator Start() {
        yield return new WaitUntil(() => configReader.FinishedReader);   

        example_anchor = GameObject.Find("ExampleAnchor");
        example_hl_anchor = GameObject.Find("ExampleHLAnchor");

        num_anchors = configReader.num_anchors;
        real_anchors = new GameObject[num_anchors];
        hl_anchors = new GameObject[num_anchors];

        // Set up GameObjects
        // Each anchor should be slighly spaced apart, 3 in each row
        for (int i = 0; i < 9; i++) {
            // Clone example_anchor
            GameObject real_anchor = Instantiate(example_anchor);
            real_anchor.name = "real_anchor_" + i.ToString();
            GameObject hl_anchor = Instantiate(example_hl_anchor);
            hl_anchor.name = "hl_anchor_" + i.ToString();

            // Set anchor material
            Material real_anchor_material = real_anchor.GetComponent<MeshRenderer>().material;
            real_anchor_material.color = Color.HSVToRGB(Random.Range(0f, 1f), 1f, 1f);
            Material hl_anchor_material = hl_anchor.GetComponent<MeshRenderer>().material;
            hl_anchor_material.color = real_anchor_material.color;

            // Set anchor positions
            real_anchor.transform.position = new Vector3((i % 3) * 0.5f, (i / 3) * 0.5f, 0f);
            // hl_anchor slightly offset from real_anchor
            hl_anchor.transform.position = real_anchor.transform.position + new Vector3(0f, 0f, 0.5f);

            // hide
            real_anchor.SetActive(false);
            hl_anchor.SetActive(false);

            real_anchors[i] = real_anchor;
            hl_anchors[i] = hl_anchor;
        }

        example_anchor.SetActive(false);
        example_hl_anchor.SetActive(false);

        // Set up publisher to publish transforms of anchors
        ros = ROSConnection.GetOrCreateInstance();
        // Publisher should be able to handle multiple transforms at once
        ros.RegisterPublisher<PoseArrayMsg>(configReader.overlay_alignment_tr);
    }

    public void AlignmentMode() {
        if (real_anchors[0].activeSelf) {
            // If anchors are already visible, delete manipulation handlers and hide
            for (int i = 0; i < num_anchors; i++) {
                Destroy(real_anchors[i].GetComponent<ManipulationHandler>());
                Destroy(hl_anchors[i].GetComponent<ManipulationHandler>());

                real_anchors[i].SetActive(false);
                hl_anchors[i].SetActive(false);
            }
        } else {
            // Set up GameObjects to be visible and interactable
            for (int i = 0; i < num_anchors; i++) {
                // Add interactable
                real_anchors[i].AddComponent<ManipulationHandler>();
                hl_anchors[i].AddComponent<ManipulationHandler>();

                real_anchors[i].SetActive(true);
                hl_anchors[i].SetActive(true);
            }
        }
    }

    public void ConfirmAlignment() {
        // Get poses of all anchors and put into PoseArray message
        PoseArrayMsg poseArrayMsg = new PoseArrayMsg();
        poseArrayMsg.header.frame_id = "map";
        poseArrayMsg.poses = new PoseMsg[num_anchors * 2];

        // Get poses of all anchors
        for (int i = 0; i < num_anchors; i++) {
            // Unity to ROS
            Vector3 real_anchor_position = new Vector3(real_anchors[i].transform.position.x, real_anchors[i].transform.position.y, real_anchors[i].transform.position.z);
            Quaternion real_anchor_rotation = new Quaternion(real_anchors[i].transform.rotation.x, real_anchors[i].transform.rotation.y, real_anchors[i].transform.rotation.z, real_anchors[i].transform.rotation.w);
            real_anchor_position = real_anchor_position.Unity2Ros();
            real_anchor_rotation = real_anchor_rotation.Unity2Ros();

            // Get pose of real anchor
            PoseMsg real_anchor_pose = new PoseMsg();
            real_anchor_pose.position = new PointMsg(real_anchor_position.x, real_anchor_position.y, real_anchor_position.z);
            real_anchor_pose.orientation = new QuaternionMsg(real_anchor_rotation.x, real_anchor_rotation.y, real_anchor_rotation.z, real_anchor_rotation.w);

            // Unity to ROS
            Vector3 hl_anchor_position = new Vector3(hl_anchors[i].transform.position.x, hl_anchors[i].transform.position.y, hl_anchors[i].transform.position.z);
            Quaternion hl_anchor_rotation = new Quaternion(hl_anchors[i].transform.rotation.x, hl_anchors[i].transform.rotation.y, hl_anchors[i].transform.rotation.z, hl_anchors[i].transform.rotation.w);
            hl_anchor_position = hl_anchor_position.Unity2Ros();
            hl_anchor_rotation = hl_anchor_rotation.Unity2Ros();

            // Get pose of hl anchor
            PoseMsg hl_anchor_pose = new PoseMsg();
            hl_anchor_pose.position = new PointMsg(hl_anchor_position.x, hl_anchor_position.y, hl_anchor_position.z);
            hl_anchor_pose.orientation = new QuaternionMsg(hl_anchor_rotation.x, hl_anchor_rotation.y, hl_anchor_rotation.z, hl_anchor_rotation.w);

            // Add to PoseArray
            poseArrayMsg.poses[i] = real_anchor_pose;
            poseArrayMsg.poses[i + num_anchors] = hl_anchor_pose;
        }

        // Publish
        ros.Send(configReader.overlay_alignment_tr, poseArrayMsg);
    }
}