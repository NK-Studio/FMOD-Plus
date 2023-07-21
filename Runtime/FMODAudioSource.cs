using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace FMODPlus
{
    [AddComponentMenu("FMOD Studio/FMOD Audio Source")]
    public class FMODAudioSource : MonoBehaviour
    {
        [SerializeField] private EventReference clip;

        [Obsolete("Use the EventReference field instead")]
        public string Event = "";

        public EventReference Clip
        {
            get => clip;
            set
            {
                clip = value;
                UnPause();
                Release();
                instance = RuntimeManager.CreateInstance(clip);
            }
        }

        private bool _preIsMute;

        [SerializeField, Tooltip("Mutes the Sound.")]
        private bool mute;

        private bool _muteFunc;

        public bool Mute
        {
            get => mute;
            set
            {
                mute = value;
                _preIsMute = mute;

                if (instance.isValid())
                {
                    // 기본적으로는 뮤트 옵션을 그대로 진행한다,
                    if (!_muteFunc)
                        instance.setPaused(mute);

                    // 뮤트하는 것을 끄고, 코드 뮤트마저 꺼져있다면,
                    if (!mute)
                        if (_muteFunc)
                            instance.setPaused(true);
                }
            }
        }

        [Tooltip("Play the sound when the Component loads.")]
        public bool PlayOnAwake = true;

        [SerializeField, Range(0f, 1f), Tooltip("Sets the overall volume of the sound.")]
        private float volume = 1f;

        /// <summary>
        /// Adjusts the overall volume of the sound.
        /// </summary>
        public float Volume
        {
            get => volume;
            set
            {
                if (instance.isValid())
                    instance.setVolume(volume);
            }
        }

        [SerializeField, Range(-3f, 3f),
         Tooltip("Sets the frequency of the sound. Use this to slow down or speed up the sound.")]
        private float pitch = 1f;

        /// <summary>
        /// Adjusts the pitch of the volume.
        /// </summary>
        public float Pitch
        {
            get => pitch;
            set
            {
                if (instance.isValid())
                    instance.setPitch(pitch);
            }
        }

        public ParamRef[] Params = Array.Empty<ParamRef>();

        public bool AllowFadeout = true;
        public bool TriggerOnce;

        public bool Preload;

        public bool OverrideAttenuation;
        public float OverrideMinDistance = -1.0f;
        public float OverrideMaxDistance = -1.0f;

        private EventDescription eventDescription;
        private EventInstance instance;

        private bool hasTriggered;
        private bool isQuitting;
        private bool isOneshot;
        private List<ParamRef> cachedParams = new();

        public EventInstance EventInstance
        {
            set => instance = value;
            get => instance;
        }

        public bool IsActive { get; private set; }

        private float MaxDistance
        {
            get
            {
                if (OverrideAttenuation)
                {
                    return OverrideMaxDistance;
                }

                if (!eventDescription.isValid())
                {
                    Lookup();
                }

                float minDistance, maxDistance;
                eventDescription.getMinMaxDistance(out minDistance, out maxDistance);
                return maxDistance;
            }
        }

        private void UpdatePlayingStatus(bool force = false)
        {
            // If at least one listener is within the max distance, ensure an event instance is playing
            bool playInstance = StudioListener.DistanceSquaredToNearestListener(transform.position) <=
                                MaxDistance * MaxDistance;

            if (force || playInstance != IsPlaying())
            {
                if (playInstance)
                    PlayInstance();
                else
                    StopInstance();
            }
        }

        private void Start()
        {
            RuntimeUtils.EnforceLibraryOrder();

            if (Preload)
            {
                Lookup();
                eventDescription.loadSampleData();
            }

            if (PlayOnAwake)
                Play();
        }

        private void OnValidate()
        {
            // If the event has changed, release the instance and lookup the new event
            ControlPause();

            Volume = volume;
            Pitch = pitch;
        }

        private void ControlPause()
        {
            if (mute != _preIsMute)
            {
                // 값이 변경될 때 수행할 동작
                if (!_muteFunc)
                    instance.setPaused(mute);
            }

            // 이전 값을 현재 값으로 업데이트
            _preIsMute = mute;
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDestroy()
        {
            if (!isQuitting)
            {
                if (instance.isValid())
                {
                    RuntimeManager.DetachInstanceFromGameObject(instance);
                    if (eventDescription.isValid() && isOneshot)
                    {
                        instance.release();
                        instance.clearHandle();
                    }
                }

                if (Preload)
                {
                    eventDescription.unloadSampleData();
                }
            }
        }

        private void Lookup()
        {
            eventDescription = RuntimeManager.GetEventDescription(Clip);

            if (eventDescription.isValid())
            {
                for (int i = 0; i < Params.Length; i++)
                {
                    PARAMETER_DESCRIPTION param;
                    eventDescription.getParameterDescriptionByName(Params[i].Name, out param);
                    Params[i].ID = param.id;
                }
            }
        }

        /// <summary>
        /// Play a sound.
        /// </summary>
        public void Play()
        {
            if (TriggerOnce && hasTriggered)
            {
                return;
            }

            if (Clip.IsNull)
            {
                return;
            }

            cachedParams.Clear();

            if (!eventDescription.isValid())
            {
                Lookup();
            }

            bool isSnapshot;
            eventDescription.isSnapshot(out isSnapshot);

            if (!isSnapshot)
            {
                eventDescription.isOneshot(out isOneshot);
            }

            bool is3D;
            eventDescription.is3D(out is3D);

            IsActive = true;

            if (is3D && !isOneshot && Settings.Instance.StopEventsOutsideMaxDistance)
            {
                UpdatePlayingStatus(true);
            }
            else
            {
                PlayInstance();
            }
        }

        private void PlayInstance()
        {
            if (!instance.isValid())
            {
                instance.clearHandle();
            }

            // Let previous oneshot instances play out
            if (isOneshot && instance.isValid())
            {
                instance.release();
                instance.clearHandle();
            }

            bool is3D;
            eventDescription.is3D(out is3D);

            if (!instance.isValid())
            {
                Lookup();
                eventDescription.createInstance(out instance);

                // Only want to update if we need to set 3D attributes
                if (is3D)
                {
                    var transform = GetComponent<Transform>();
                    {
                        instance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
                        RuntimeManager.AttachInstanceToGameObject(instance, transform, false);
                    }
                }
            }

            foreach (var param in Params)
            {
                instance.setParameterByID(param.ID, param.Value);
            }

            foreach (var cachedParam in cachedParams)
            {
                instance.setParameterByID(cachedParam.ID, cachedParam.Value);
            }

            if (is3D && OverrideAttenuation)
            {
                instance.setProperty(EVENT_PROPERTY.MINIMUM_DISTANCE, OverrideMinDistance);
                instance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, OverrideMaxDistance);
            }

            instance.start();

            if (_muteFunc)
                instance.setPaused(true);

            hasTriggered = true;
        }

        /// <summary>
        /// Stop the sound.
        /// </summary>
        public void Stop()
        {
            IsActive = false;
            hasTriggered = false;
            cachedParams.Clear();
            StopInstance();
        }

        /// <summary>
        /// Stop the sound.
        /// </summary>
        public void Stop(bool fade)
        {
            AllowFadeout = fade;
            IsActive = false;
            hasTriggered = false;
            cachedParams.Clear();
            StopInstance();
        }

        private void Release()
        {
            IsActive = false;
            cachedParams.Clear();

            if (instance.isValid())
            {
                instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                instance.release();
                instance.clearHandle();
            }
        }

        private void StopInstance()
        {
            if (instance.isValid())
            {
                instance.stop(AllowFadeout ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
                instance.release();
                if (!AllowFadeout)
                {
                    instance.clearHandle();
                }
            }
        }

        /// <summary>
        /// Parameters are set with parameters entered as arguments.
        /// </summary>
        /// <param name="parameters"></param>
        public void ApplyParameter(ParamRef[] parameters)
        {
            foreach (ParamRef sourceParam in Params)
            foreach (ParamRef param in parameters)
                if (sourceParam.Name == param.Name)
                {
                    sourceParam.Value = param.Value;
                    break;
                }

            foreach (ParamRef parameter in parameters)
                SetParameter(parameter.Name, parameter.Value);
        }

        /// <summary>
        /// Set the parameters of the sound.
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="value"></param>
        /// <param name="ignoreseekspeed"></param>
        public void SetParameter(string paramName, float value, bool ignoreseekspeed = false)
        {
            if (Settings.Instance.StopEventsOutsideMaxDistance && IsActive)
            {
                string findName = paramName;
                ParamRef cachedParam = cachedParams.Find(x => x.Name == findName);

                if (cachedParam == null)
                {
                    PARAMETER_DESCRIPTION paramDesc;
                    eventDescription.getParameterDescriptionByName(paramName, out paramDesc);

                    cachedParam = new ParamRef
                    {
                        ID = paramDesc.id,
                        Name = paramDesc.name
                    };
                    cachedParams.Add(cachedParam);
                }

                cachedParam.Value = value;
            }

            if (instance.isValid())
            {
                instance.setParameterByName(paramName, value, ignoreseekspeed);
            }
        }

        /// <summary>
        /// Set the parameters of the sound.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <param name="ignoreseekspeed"></param>
        public void SetParameter(PARAMETER_ID id, float value, bool ignoreseekspeed = false)
        {
            if (Settings.Instance.StopEventsOutsideMaxDistance && IsActive)
            {
                PARAMETER_ID findId = id;
                ParamRef cachedParam = cachedParams.Find(x => x.ID.Equals(findId));

                if (cachedParam == null)
                {
                    PARAMETER_DESCRIPTION paramDesc;
                    eventDescription.getParameterDescriptionByID(id, out paramDesc);

                    cachedParam = new ParamRef();
                    cachedParam.ID = paramDesc.id;
                    cachedParam.Name = paramDesc.name;
                    cachedParams.Add(cachedParam);
                }

                cachedParam.Value = value;
            }

            if (instance.isValid())
            {
                instance.setParameterByID(id, value, ignoreseekspeed);
            }
        }

        /// <summary>
        /// Returns the length of the current clip.
        /// </summary>
        public float Length
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Clip.Path))
                    return 0f;

                var currentEventRef = RuntimeManager.GetEventDescription(Clip);

                if (currentEventRef.isValid())
                {
                    currentEventRef.getLength(out int length);
                    float convertSecond = length / 1000f;

                    return convertSecond;
                }

                return 0f;
            }
        }

        /// <summary>
        /// Returns the time of the currently playing event.
        /// </summary>
        public float Time
        {
            get
            {
                if (!IsPlaying())
                    return 0f;

                EventInstance.getTimelinePosition(out var time);
                float convertTime = time / 1000f;
                return convertTime;
            }
        }

        /// <summary>
        /// Checks if a sound is playing.
        /// </summary>
        /// <returns></returns>
        public bool IsPlaying()
        {
            if (instance.isValid())
            {
                PLAYBACK_STATE playbackState;
                instance.getPlaybackState(out playbackState);
                return (playbackState != PLAYBACK_STATE.STOPPED);
            }

            return false;
        }

        /// <summary>
        /// Pause the audio.
        /// </summary>
        public void Pause()
        {
            _muteFunc = true;
            instance.setPaused(true);
        }

        /// <summary>
        /// Unpause the audio.
        /// </summary>
        public void UnPause()
        {
            _muteFunc = false;

            if (!mute)
                instance.setPaused(false);
        }

        /// <summary>
        /// Call Key Off when using Sustain Key Point.
        /// </summary>
        public void KeyOff()
        {
            EventInstance.keyOff();
        }

        /// <summary>
        /// Call Key Off when using Sustain Key Point.
        /// </summary>
        public void TriggerCue()
        {
            KeyOff();
        }

        /// <summary>
        /// Create an instance in-place, play a sound effect, and destroy it immediately.
        /// </summary>
        /// <param name="path">Sound effect path to play.</param>
        /// <param name="volumeScale"></param>
        /// <param name="position">Play a sound at that location.</param>
        public void PlayOneShot(string path, float volumeScale = 1.0f, Vector3 position = default)
        {
            try
            {
                PlayOneShot(RuntimeManager.PathToGUID(path), volumeScale, position);
            }
            catch (EventNotFoundException)
            {
                RuntimeUtils.DebugLogWarning("[FMOD] Event not found: " + path);
            }
        }

        /// <summary>
        /// Create an instance in-place, play a sound effect, and destroy it immediately.
        /// </summary>
        /// <param name="eventReference">Sound effect path to play.</param>
        /// <param name="volumeScale"></param>
        /// <param name="position">Play a sound at that location.</param>
        public void PlayOneShot(EventReference eventReference, float volumeScale = 1.0f, Vector3 position = default)
        {
            try
            {
                PlayOneShot(eventReference.Guid, volumeScale, position);
            }
            catch (EventNotFoundException)
            {
                RuntimeUtils.DebugLogWarning("[FMOD] Event not found: " + eventReference);
            }
        }

        /// <summary>
        /// Parameter compatible, create instance internally, play sound effect, destroy immediately.
        /// </summary>
        /// <param name="eventReference"></param>
        /// <param name="parameterName"></param>
        /// <param name="parameterValue"></param>
        /// <param name="volumeScale"></param>
        /// <param name="position"></param>
        public void PlayOneShot(EventReference eventReference, string parameterName, float parameterValue,
            float volumeScale = 1.0f, Vector3 position = new())
        {
            try
            {
                PlayOneShot(eventReference.Guid, parameterName, parameterValue, volumeScale, position);
            }
            catch (EventNotFoundException)
            {
                RuntimeUtils.DebugLogWarning("[FMOD] Event not found: " + eventReference);
            }
        }

        /// <summary>
        /// Parameter compatible, create instance internally, play sound effect, destroy immediately.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="parameterName"></param>
        /// <param name="parameterValue"></param>
        /// <param name="volumeScale"></param>
        /// <param name="position"></param>
        public void PlayOneShot(string path, string parameterName, float parameterValue,
            float volumeScale = 1.0f, Vector3 position = new())
        {
            try
            {
                PlayOneShot(RuntimeManager.PathToGUID(path), parameterName, parameterValue, volumeScale, position);
            }
            catch (EventNotFoundException)
            {
                RuntimeUtils.DebugLogWarning("[FMOD] Event not found: " + path);
            }
        }

        private void PlayOneShot(FMOD.GUID guid, float volumeScale = 1.0f, Vector3 position = new())
        {
            EventInstance instance = RuntimeManager.CreateInstance(guid);
            instance.set3DAttributes(position.To3DAttributes());
            instance.setVolume(volumeScale);
            instance.start();
            instance.release();
        }

        private void PlayOneShot(FMOD.GUID guid, string parameterName, float parameterValue,
            float volumeScale = 1.0f, Vector3 position = new())
        {
            EventInstance instance = RuntimeManager.CreateInstance(guid);
            instance.set3DAttributes(position.To3DAttributes());
            instance.setParameterByName(parameterName, parameterValue);
            instance.setVolume(volumeScale);
            instance.start();
            instance.release();
        }
    }
}
