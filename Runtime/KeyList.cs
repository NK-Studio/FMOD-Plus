using System.Collections.Generic;
using FMODUnity;
using JetBrains.Annotations;
using UnityEngine;

namespace FMODPlus
{
    public class KeyList : ScriptableObject
    {
        public AudioPathByString Clips;

        [SerializeField] [UsedImplicitly] private List<EventReferenceByKey> cachedSearchClips;

        private static KeyList _instance;

        public static KeyList Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = Resources.Load<KeyList>("KeyList");
                
                return _instance;
            }
            set => _instance = value;
        }

#if UNITY_EDITOR
        public EditorEventRef GetEventRef(string key)
        {
            List<EventReferenceByKey> clipList = Clips.GetList();

            foreach (EventReferenceByKey referenceByKey in clipList)
                if (referenceByKey.Key == key)
                {
                    EventReference eventRef = referenceByKey.Value;
                    return EventManager.EventFromPath(eventRef.Path);
                }

            return null;
        }
#endif
        
        /// <summary>
        /// Find EventReference and ParamRef via Key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="clip"></param>
        /// <param name="paramRefs"></param>
        /// <returns></returns>
        public bool TryFindClipAndParams(string key, out EventReference clip, out ParamRef[] paramRefs)
        {
            if (Clips.TryGetValue(key, out EventReference eventReference))
                if (Clips.TryGetParamRef(key, out ParamRef[] parameters))
                {
                    clip = eventReference;
                    paramRefs = parameters;
                    return true;
                }

            string msg = Application.systemLanguage == SystemLanguage.Korean
                ? $"Key: {key}을(를) 찾을 수 없습니다."
                : $"Key: {key} is not found.";

            Debug.LogError(msg);
            clip = default;
            paramRefs = default;
            return false;
        }

        /// <summary>
        /// Find ParamRef through Key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="paramRefs"></param>
        /// <returns></returns>
        public bool TryFindParamRefs(string key, out ParamRef[] paramRefs)
        {
            if (Clips.TryGetParamRef(key, out ParamRef[] parameter))
            {
                paramRefs = parameter;
                return true;
            }

            string msg = Application.systemLanguage == SystemLanguage.Korean
                ? $"Key: {key}을(를) 찾을 수 없습니다."
                : $"Key: {key} is not found.";

            Debug.LogError(msg);
            paramRefs = default;
            return false;
        }

        /// <summary>
        /// Find the EventReference by Key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        public bool TryFindClip(string key, out EventReference clip)
        {
            if (Clips.TryGetValue(key, out EventReference eventReference))
            {
                clip = eventReference;
                return true;
            }

            string msg = Application.systemLanguage == SystemLanguage.Korean
                ? $"Key: {key}을(를) 찾을 수 없습니다."
                : $"Key: {key} is not found.";

            Debug.LogError(msg);
            clip = default;
            return false;
        }
    }
}