#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
using Windows.Storage.Streams;
#endif

using System;
using UnityEngine;
using Newtonsoft.Json;

public class ConfigReader : MonoBehaviour
{
    public bool FinishedReader = false;

    public string ip = "192.168.50.238";
    public int port = 9091;

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
        FinishedReader = true;
    }
}

public class ConfigFile
{
    public string ip { get; set; }
    public int port { get; set; }
}
