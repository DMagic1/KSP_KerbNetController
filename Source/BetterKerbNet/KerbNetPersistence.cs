#region license
/*The MIT License (MIT)
KerbNetPersistence - A persistent storage module for saving settings to disk
Copyright (c) 2016 DMagic

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion

using System;
using System.Reflection;
using System.IO;
using UnityEngine;

namespace BetterKerbNet
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class KerbNetPersistence : MonoBehaviour
	{
		[Persistent]
		public bool showTooltips = true;
		[Persistent]
		public bool rememberFoV;
		[Persistent]
		public bool rememberMode;
		[Persistent]
		public bool rememberOverlay;
		[Persistent]
		public bool autoRefresh;
		[Persistent]
		public bool orientationButton;
		[Persistent]
		public float scale = 1;

		private const string fileName = "PluginData/Settings.cfg";
		private string fullPath;
		private KerbNetDevourerSettings settings;

		private static bool loaded;
		private static KerbNetPersistence instance;

		public static KerbNetPersistence Instance
		{
			get { return instance; }
		}

		private void Awake()
		{
			if (loaded)
			{
				Destroy(gameObject);
				return;
			}

			DontDestroyOnLoad(gameObject);

			loaded = true;

			instance = this;

			fullPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fileName).Replace("\\", "/");
			GameEvents.OnGameSettingsApplied.Add(SettingsApplied);

			if (Load())
				print("[KerbNet Controller] Settings file loaded");
			else
			{
				if (Save())
					print("[KerbNet Controller] New Settings files generated at:\n" + fullPath);
			}
		}

		private void OnDestroy()
		{
			GameEvents.OnGameSettingsApplied.Remove(SettingsApplied);
		}

		public void SettingsApplied()
		{
			if (HighLogic.CurrentGame != null)
				settings = HighLogic.CurrentGame.Parameters.CustomParams<KerbNetDevourerSettings>();

			if (settings == null)
				return;

			if (settings.useAsDefault)
			{
				showTooltips = settings.showTooltips;
				rememberFoV = settings.rememberFoV;
				rememberMode = settings.rememberMode;
				rememberOverlay = settings.rememberOverlay;
				autoRefresh = settings.autoRefresh;
				orientationButton = settings.orientationButton;
				scale = settings.scale;

				if (Save())
					print("[KerbNet Controller] Settings file saved");
			}
		}

		public bool Load()
		{
			bool b = false;

			try
			{
				if (File.Exists(fullPath))
				{
					ConfigNode node = ConfigNode.Load(fullPath);
					ConfigNode unwrapped = node.GetNode(GetType().Name);
					ConfigNode.LoadObjectFromConfig(this, unwrapped);
					b = true;
				}
				else
				{
					print(string.Format("[KerbNet Controller] Settings file could not be found [{0}]", fullPath));
					b = false;
				}
			}
			catch (Exception e)
			{
				print(string.Format("[KerbNet Controller] Error while loading settings file from [{0}]\n{1}", fullPath, e));
				b = false;
			}

			return b;
		}

		public bool Save()
		{
			bool b = false;

			try
			{
				ConfigNode node = AsConfigNode();
				ConfigNode wrapper = new ConfigNode(GetType().Name);
				wrapper.AddNode(node);
				wrapper.Save(fullPath);
				b = true;
			}
			catch (Exception e)
			{
				print(string.Format("[KerbNet Controller] Error while saving settings file from [{0}]\n{1}", fullPath, e));
				b = false;
			}

			return b;
		}

		private ConfigNode AsConfigNode()
		{
			try
			{
				ConfigNode node = new ConfigNode(GetType().Name);

				node = ConfigNode.CreateConfigFromObject(this, node);
				return node;
			}
			catch (Exception e)
			{
				print(string.Format("[KerbNet Controller] Failed to generate settings file node...\n{0}", e));
				return new ConfigNode(GetType().Name);
			}
		}
	}
}
