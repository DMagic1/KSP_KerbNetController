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

using System.Reflection;
using UnityEngine;

namespace BetterKerbNet
{

	public class KerbNetDevourerSettings : GameParameters.CustomParameterNode
	{
		private const string easyDescription = "The best possible FoV range and anomaly scanning chance is used for all KerbNet display modes";
		private const string medDescription = "The best FoV range and anomaly scanning chance are calculated separately for each KerbNet display mode";
		private const string hardDescription = "Individual KerbNet scanning modules are used to set the FoV range ane anomaly scanning chance for each display mode";

		[GameParameters.CustomIntParameterUI("FoV Difficulty", toolTip = "", minValue = 0, maxValue = 2, autoPersistance = true)]
		public int setting = 1;
		[GameParameters.CustomStringParameterUI("Selected Mode:", lines = 4, autoPersistance = false)]
		public string description;
		[GameParameters.CustomParameterUI("Show Tooltips", autoPersistance = true)]
		public bool showTooltips = true;
		[GameParameters.CustomParameterUI("Remember Last FoV Value", autoPersistance = true)]
		public bool rememberFoV;
		[GameParameters.CustomParameterUI("Remember Last Display Mode", toolTip = "Automatically start in the same display mode as when last used", autoPersistance = true)]
		public bool rememberMode;
		[GameParameters.CustomParameterUI("Remember Last Overlay Mode", toolTip = "Automatically adjust the grid overlay to that when last used", autoPersistance = true)]
		public bool rememberOverlay;
		[GameParameters.CustomParameterUI("Remember Auto-Refresh Setting", autoPersistance = true)]
		public bool autoRefresh;

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
			switch(setting)
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

			return true;
		}
	}
}
