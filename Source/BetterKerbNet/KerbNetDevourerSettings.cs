#region license
/*The MIT License (MIT)
KerbNetDevourerSettings - In game settings for KerbNet Controller
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

using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using KSP.UI;
using TMPro;

namespace BetterKerbNet
{
	public class KerbNetDevourerSettings : GameParameters.CustomParameterNode
	{
		private const string easyDescription = "The best possible FoV range and anomaly scanning chance is used for all KerbNet display modes";
		private const string medDescription = "The best FoV range and anomaly scanning chance are calculated separately for each KerbNet display mode";
		private const string hardDescription = "Individual KerbNet scanning modules are used to set the FoV range and anomaly scanning chance for each display mode";

		[GameParameters.CustomStringParameterUI("", lines = 3, autoPersistance = false)]
		public string Reload = "KerbNet must be closed and restarted for changes to take effect.";
		[GameParameters.CustomIntParameterUI("FoV Difficulty", minValue = 0, maxValue = 2, autoPersistance = true)]
		public int setting = 1;
		[GameParameters.CustomStringParameterUI("Selected Mode", lines = 4, autoPersistance = false)]
		public string description;
		[GameParameters.CustomParameterUI("Show Tooltips", autoPersistance = true)]
		public bool showTooltips = true;
		[GameParameters.CustomParameterUI("Remember Last FoV Value", toolTip = "Automatically start with the last selected FoV value", autoPersistance = true)]
		public bool rememberFoV = false;
		[GameParameters.CustomParameterUI("Remember Last Display Mode", toolTip = "Automatically start in the same display mode as when last used", autoPersistance = true)]
		public bool rememberMode = false;
		[GameParameters.CustomParameterUI("Remember Last Overlay Mode", toolTip = "Automatically adjust the grid overlay to that when last used", autoPersistance = true)]
		public bool rememberOverlay = false;
		[GameParameters.CustomParameterUI("Remember Auto-Refresh Setting", toolTip = "Automatically adjust the auto-refresh setting", autoPersistance = true)]
		public bool autoRefresh = false;
		[GameParameters.CustomParameterUI("Add Map Orientation Button", toolTip = "Adds a button to toggle between orbit-up and north-up orientations", autoPersistance = true)]
		public bool orientationButton = false;
		[GameParameters.CustomFloatParameterUI("Scale", toolTip = "Adjust the UI scale for the KerbNet window", asPercentage = true, minValue = 0.5f, maxValue = 4, displayFormat = "N1", autoPersistance = true)]
		public float scale = 1;
		[GameParameters.CustomParameterUI("Use As Default", toolTip = "Save these settings to a file on disk to be used as defaults for newly created games", autoPersistance = false)]
		public bool useAsDefault = false;

		public KerbNetDevourerSettings()
		{
			if (HighLogic.LoadedScene == GameScenes.MAINMENU)
			{
				if (KerbNetPersistence.Instance == null)
					return;

				showTooltips = KerbNetPersistence.Instance.showTooltips;
				rememberFoV = KerbNetPersistence.Instance.rememberFoV;
				rememberMode = KerbNetPersistence.Instance.rememberMode;
				rememberOverlay = KerbNetPersistence.Instance.rememberOverlay;
				autoRefresh = KerbNetPersistence.Instance.autoRefresh;
				orientationButton = KerbNetPersistence.Instance.orientationButton;
				scale = KerbNetPersistence.Instance.scale;
			}

			useAsDefault = false;
		}

		public override GameParameters.GameMode GameMode
		{
			get { return GameParameters.GameMode.ANY; }
		}

		public override bool HasPresets
		{
			get { return true; }
		}

		public override string Section
		{
			get { return "DMagic Mods"; }
		}

		public override string DisplaySection
		{
			get { return "DMagic Mods"; }
		}

		public override int SectionOrder
		{
			get { return 1; }
		}

		public override string Title
		{
			get { return "KerbNet Controller"; }
		}

		public override void SetDifficultyPreset(GameParameters.Preset preset)
		{
			switch (preset)
			{
				case GameParameters.Preset.Easy:
				case GameParameters.Preset.Normal:
					description = easyDescription;
					setting = 0;
					break;
				case GameParameters.Preset.Moderate:
				case GameParameters.Preset.Custom:
					description = medDescription;
					setting = 1;
					break;
				case GameParameters.Preset.Hard:
					description = hardDescription;
					setting = 2;
					break;
			}
		}

		public override bool Enabled(MemberInfo member, GameParameters parameters)
		{
			if (member.Name == "description")
			{
				switch (setting)
				{
					case 0:
					default:
						description = easyDescription;
						break;
					case 1:
						description = medDescription;
						break;
					case 2:
						description = hardDescription;
						break;
				}
			}
			else if (member.Name == "Reload")
				return HighLogic.LoadedSceneIsFlight;

			return true;
		}
	}
}
