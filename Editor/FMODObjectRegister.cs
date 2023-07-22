#if UNITY_EDITOR
using FMODUnity;
using UnityEditor;
using UnityEngine;

namespace FMODPlus
{
    public class FMODObjectRegister : Editor
    {
        private const string KeyListDirectory = "Assets/Plugins/FMODPlus/Resources";
        private const string KeyListFilePath = "Assets/Plugins/FMODPlus/Resources/KeyList.asset";
        
        [MenuItem("GameObject/Audio/FMOD/Audio Source", priority = 5)]
        private static void FMODAudioSourceCreate()
        {
            var obj = new GameObject("Audio Source");
            obj.AddComponent<FMODAudioSource>();
            EditorUtility.SetDirty(obj);
            Selection.activeObject = obj;
        }

        [MenuItem("GameObject/Audio/FMOD/Command Sender", priority = 6)]
        private static void FMODAudioCommandSenderCreate()
        {
            var obj = new GameObject("Command Sender");
            obj.AddComponent<EventCommandSender>();
            EditorUtility.SetDirty(obj);
            Selection.activeObject = obj;
        }

        [MenuItem("GameObject/Audio/FMOD/Parameter Sender", priority = 7)]
        private static void FMODParameterSenderCreate()
        {
            var obj = new GameObject("Parameter Sender");
            obj.AddComponent<FMODParameterSender>();
            EditorUtility.SetDirty(obj);
            Selection.activeObject = obj;
        }
        
        [MenuItem("GameObject/Audio/FMOD/Key List", priority = 8)]
        private static void FMODLocalKeyListCreate()
        {
            var obj = new GameObject("Local Key List");
            obj.AddComponent<LocalKeyList>();
            EditorUtility.SetDirty(obj);
            Selection.activeObject = obj;
        }

        [MenuItem("FMOD/FMOD Plus/Key List")]
        private static void MoveKeyList()
        {
            if (KeyList.Instance == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Plugins"))
                    AssetDatabase.CreateFolder("Assets", "Plugins");
                
                if (!AssetDatabase.IsValidFolder("Assets/Plugins/FMODPlus"))
                    AssetDatabase.CreateFolder("Assets/Plugins", "FMODPlus");
          
                if (!AssetDatabase.IsValidFolder(KeyListDirectory))
                    AssetDatabase.CreateFolder("Assets/Plugins/FMODPlus", "Resources");

                KeyList.Instance = AssetDatabase.LoadAssetAtPath<KeyList>(KeyListFilePath);

                if (KeyList.Instance == null)
                {
                    KeyList.Instance = CreateInstance<KeyList>();
                    AssetDatabase.CreateAsset(KeyList.Instance, KeyListFilePath);
                }
            }
            
            Selection.activeObject = KeyList.Instance;
        }
    }
}
#endif