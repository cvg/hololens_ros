using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.UnityRoboticsDemo;
using System.Collections;

/// <summary>
///
/// </summary>
public class RosPublisherExample : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "pos_rot";

    // The game object
    public GameObject cube;
    // Publish the cube's position and rotation every N seconds
    public float publishMessageFrequency = 2f;

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;

    public ConfigReader configReader;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => configReader.FinishedReader);         

        // start the ROS connection
        if (configReader.ip == "")
        {
            Debug.Log("IP WAS NOT READ PROPERLY");
        }
        ros = ROSConnection.GetOrCreateInstance();
        ros.Connect(configReader.ip, configReader.port);
        Debug.Log("Network " + ros.RosIPAddress);
        Debug.Log("Port " + ros.RosPort);

        ros.RegisterPublisher<PosRotMsg>(topicName);
        Debug.Log("Registered PosRot Publisher");
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed > publishMessageFrequency)
        {
            timeElapsed = 0;

            cube.transform.rotation = Random.rotation;

            PosRotMsg cubePos = new PosRotMsg(
                cube.transform.position.x,
                cube.transform.position.y,
                cube.transform.position.z,
                cube.transform.rotation.x,
                cube.transform.rotation.y,
                cube.transform.rotation.z,
                cube.transform.rotation.w
            );

            // Finally send the message to server_endpoint.py running in ROS
            Debug.Log("Publishing to topic " + topicName);
            ros.Publish(topicName, cubePos);

        }
    }
}