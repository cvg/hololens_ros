#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
using Windows.Storage.Streams;
#endif

using System;
using UnityEngine;
using Newtonsoft.Json;

// JULIA: Set up imports for ROS
using Unity.Robotics.ROSTCPConnector;

public class ConfigReader : MonoBehaviour
{
    public bool FinishedReader = false;

    public string ip = "129.132.105.150";
    public int port = 9090;

    private ROSConnection ros;

    void Start()
    {
        LoadFile();
    }

    async void LoadFile()
    {
#if ENABLE_WINMD_SUPPORT
        Debug.Log("Getting the file");
        StorageFile file = await StorageFile.GetFileFromPathAsync("U:\\Users\\chenj\\Pictures\\ip.txt"); // Need to make this path not dependent on username
        Debug.Log("Getting the Random Access Stream");
        IRandomAccessStream stream = await file.OpenAsync(0);
        string fileContent = await FileIO.ReadTextAsync(file);
        ConfigFile json  = JsonConvert.DeserializeObject<ConfigFile>(fileContent);
        ip = json.ip;
        port = json.port;
        Debug.Log(fileContent);
#endif

        ros = ROSConnection.GetOrCreateInstance();
        ros.Connect(ip, port); // Specify the unique ip and port once, all other GetOrCreateInstance() should point here

        FinishedReader = true;
    }
}

public class ConfigFile
{
    public string ip { get; set; }
    public int port { get; set; }
}
