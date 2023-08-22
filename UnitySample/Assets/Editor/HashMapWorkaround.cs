using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

 

/// <summary>
/// This is necessary to silence a benign error until all lab images are updated with a unity version in this thread
/// https://forum.unity.com/threads/workaround-for-building-with-il2cpp-with-visual-studio-2022-17-4.1355570/
/// </summary>
public class HashMapWorkaround
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target == BuildTarget.WSAPlayer)
        {
            Debug.Log(pathToBuiltProject);
            string headerToModify = Path.Combine(pathToBuiltProject, @"Il2CppOutputProject\IL2CPP\external\google\sparsehash\internal\sparseconfig.h");
            string headerText = "#define _SILENCE_STDEXT_HASH_DEPRECATION_WARNINGS\n" + File.ReadAllText(headerToModify);
            File.WriteAllText(headerToModify, headerText);
        }
    }
}