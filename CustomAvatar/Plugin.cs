﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IllusionPlugin;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CustomAvatar
{
	public class Plugin : IPlugin
	{
		private const string CustomAvatarsPath = "CustomAvatars";
		private const string FirstPersonEnabledKey = "avatarFirstPerson";
		private const string PreviousAvatarKey = "previousAvatar";
		
		private bool _init;
		private bool _firstPersonEnabled;

	    private GameScenesManager _scenesManager;
		
		public Plugin()
		{
			Instance = this;
		}

		public event Action<bool> FirstPersonEnabledChanged;

		public static Plugin Instance { get; private set; }
		public AvatarLoader AvatarLoader { get; private set; }
		public PlayerAvatarManager PlayerAvatarManager { get; private set; }

		public bool FirstPersonEnabled
		{
			get { return _firstPersonEnabled; }
			private set
			{
				if (_firstPersonEnabled == value) return;

				_firstPersonEnabled = value;

				if (value)
				{
					PlayerPrefs.SetInt(FirstPersonEnabledKey, 0);
				}
				else
				{
					PlayerPrefs.DeleteKey(FirstPersonEnabledKey);
				}
                
				FirstPersonEnabledChanged?.Invoke(value);
			}
		}

		public string Name
		{
			get { return "Custom Avatars Plugin"; }
		}

		public string Version
		{
			get { return "3.1.3-beta"; }
		}

		public static void Log(object message)
		{
		    string fullMsg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.FFF}] [CustomAvatarsPlugin] {message}";

            Debug.Log(fullMsg);
			File.AppendAllText("CustomAvatarsPlugin-log.txt", fullMsg + Environment.NewLine);
		}

		public void OnApplicationStart()
		{
			if (_init) return;
			_init = true;
			
			File.WriteAllText("CustomAvatarsPlugin-log.txt", string.Empty);
			
			AvatarLoader = new AvatarLoader(CustomAvatarsPath, AvatarsLoaded);
			
			FirstPersonEnabled = PlayerPrefs.HasKey(FirstPersonEnabledKey);

		    SceneManager.sceneLoaded += SceneLoaded;
        }

		public void OnApplicationQuit()
		{
			if (PlayerAvatarManager == null) return;
			PlayerAvatarManager.AvatarChanged -= PlayerAvatarManagerOnAvatarChanged;
		    SceneManager.sceneLoaded -= SceneLoaded;

		    if (_scenesManager != null)
		        _scenesManager.transitionDidFinishEvent -= SceneTransitionDidFinish;
		}

	    public void SceneLoaded(Scene scene, LoadSceneMode mode)
	    {
	        if (_scenesManager == null)
	        {
	            _scenesManager = Resources.FindObjectsOfTypeAll<GameScenesManager>().FirstOrDefault();

	            if (_scenesManager != null)
	                _scenesManager.transitionDidFinishEvent += SceneTransitionDidFinish;
	        }
	    }

	    private void SceneTransitionDidFinish()
	    {
	        Camera mainCamera = Camera.main;

	        SetCameraCullingMask(mainCamera);
        }

	    private void AvatarsLoaded(IReadOnlyList<CustomAvatar> loadedAvatars)
		{
			if (loadedAvatars.Count == 0)
			{
				Log("No custom avatars found in path " + Path.GetFullPath(CustomAvatarsPath));
				return;
			}

			var previousAvatarPath = PlayerPrefs.GetString(PreviousAvatarKey, null);
			var previousAvatar = AvatarLoader.Avatars.FirstOrDefault(x => x.FullPath == previousAvatarPath);
			
			PlayerAvatarManager = new PlayerAvatarManager(AvatarLoader, previousAvatar);
			PlayerAvatarManager.AvatarChanged += PlayerAvatarManagerOnAvatarChanged;
		}

		private void PlayerAvatarManagerOnAvatarChanged(CustomAvatar newAvatar)
		{
			PlayerPrefs.SetString(PreviousAvatarKey, newAvatar.FullPath);
		}

		public void OnUpdate()
		{
		    if (Input.GetKeyDown(KeyCode.PageUp))
			{
				if (PlayerAvatarManager == null) return;
				PlayerAvatarManager.SwitchToNextAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.PageDown))
			{
				if (PlayerAvatarManager == null) return;
				PlayerAvatarManager.SwitchToPreviousAvatar();
			}
			else if (Input.GetKeyDown(KeyCode.Home))
			{
				FirstPersonEnabled = !FirstPersonEnabled;
			}
		}

		private void SetCameraCullingMask(Camera camera)
		{
            Log("Adding third person culling mask to " + camera.name);

		    camera.cullingMask &= ~(1 << AvatarLayers.OnlyInThirdPerson);
		    camera.cullingMask |= 1 << AvatarLayers.OnlyInFirstPerson;
		}

		public void OnFixedUpdate()
		{
		}

		public void OnLevelWasInitialized(int level)
		{
		}

		public void OnLevelWasLoaded(int level)
		{
		}
	}
}