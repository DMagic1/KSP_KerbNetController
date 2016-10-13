#region license
/*The MIT License (MIT)
ModuleKerbNetDevourer - An IAccessKerbNet interface module for collecting KerbNet data from the entire vessel
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using KSP.UI;
using KSP.UI.Dialogs;
using KSP.UI.TooltipTypes;
using KerbNet;

namespace BetterKerbNet
{
    public class ModuleKerbNetDevourer : IAccessKerbNet
    {
		private Vessel _activeVessel;		
		private Dictionary<string, KerbNetStorage> _kerbnets = new Dictionary<string, KerbNetStorage>();
		private float _bestMaxFoV;
		private float _bestMinFoV;
		private float _bestAnomaly;
		private float _currentMinFoV;
		private float _currentMaxFoV;
		private float _currentAnomaly;
		private Part _currentPart;
		private IAccessKerbNet _currentInterface;
		private KerbNetDialog _dialog;
		private KerbNetDevourerSettings _settings;
		private UIStateButton[] _stateButtons;
		private UIStateButton _orientButton;
		private Button[] _dialogButtons;
		private static string _autoRefreshState;
		private static string _currentMode;
		private static string _visibilityMode;
		private static float _currentFoV;
		private static Sprite _northSprite;
		private static Sprite _orbitSprite;

		public ModuleKerbNetDevourer(Vessel v)
		{
			_activeVessel = v;

			_settings = HighLogic.CurrentGame.Parameters.CustomParams<KerbNetDevourerSettings>();

			if (_northSprite == null)
			{
				Texture2D _north = GameDatabase.Instance.GetTexture("KerbNetController/Resources/North_Up", false);

				if (_north != null)
					_northSprite = Sprite.Create(_north, new Rect(0, 0, _north.width, _north.height), new Vector2(0.5f, 0.5f));
			}

			if (_orbitSprite == null)
			{
				Texture2D _orbit = GameDatabase.Instance.GetTexture("KerbNetController/Resources/Orbit_Up", false);

				if (_orbit != null)
					_orbitSprite = Sprite.Create(_orbit, new Rect(0, 0, _orbit.width, _orbit.height), new Vector2(0.5f, 0.5f));
			}
			
			if (SimpleScan())
			{
				if (KerbNetToolbar.Instance != null)
					KerbNetToolbar.Instance.SetToolbarEnabled(true);
			}
			else
			{
				if (KerbNetToolbar.Instance != null)
					KerbNetToolbar.Instance.SetToolbarEnabled(false);
			}
		}

		public void Open()
		{
			if (_dialog != null)
				Close();

			if (SimpleScan())
			{
				if (KerbNetToolbar.Instance != null)
					KerbNetToolbar.Instance.SetToolbarEnabled(true);

				ScanVessel();
			}
			else
			{
				if (KerbNetToolbar.Instance != null)
					KerbNetToolbar.Instance.SetToolbarEnabled(false);

				return;
			}

			if (!_settings.rememberMode || string.IsNullOrEmpty(_currentMode) || !_kerbnets.ContainsKey(_currentMode))
				_currentMode = GetFirstMode();

			UpdateValues(_currentMode);

			_dialog = KerbNetDialog.Display(this);

			_dialog.transform.localScale *= _settings.scale;

			AddModeListener();

			SetCloseListener();

			if (_settings.rememberOverlay)
				SetOverlayListener();

			if (_settings.autoRefresh)
				SetAutoRefreshListener();

			if (_settings.rememberFoV)
				SetFoVListener();

			if (_settings.orientationButton)
				SetOrientationButton();

			if (!_settings.showTooltips)
				KillTooltips();

			UpdateDisplay();
		}

		public void Close()
		{
			KerbNetDialog.Close();

			_dialog = null;
		}

		public void RefreshVessel(Vessel v)
		{
			_activeVessel = v;

			if (SimpleScan())
			{
				if (KerbNetToolbar.Instance != null)
					KerbNetToolbar.Instance.SetToolbarEnabled(true);

				ScanVessel();
			}
			else
			{
				if (KerbNetToolbar.Instance != null)
					KerbNetToolbar.Instance.SetToolbarEnabled(false);

				return;
			}

			if (_dialog == null)
				return;

			UpdateValues(_currentMode);

			UpdateDisplay();
		}

		private string GetFirstMode()
		{
			if (_kerbnets == null)
				return null;

			if (_kerbnets.Count <= 0)
				return null;

			return _kerbnets.Keys.First();
		}

		private bool SimpleScan()
		{
			var accessors = _activeVessel.FindPartModulesImplementing<IAccessKerbNet>();

			return accessors.Count > 0;
		}

		private void ScanVessel()
		{
			_kerbnets.Clear();

			var accessors = _activeVessel.FindPartModulesImplementing<IAccessKerbNet>();

			MonoBehaviour.print("[KerbNet Controller] Scanning Vessel [" + _activeVessel.vesselName + "]... Found [" + accessors.Count + "] KerbNet Parts");

			for (int i = accessors.Count - 1; i >= 0; i--)
			{
				IAccessKerbNet access = accessors[i];

				if (access == null)
					return;

				List<string> modes = access.GetKerbNetDisplayModes();

				for (int j = modes.Count - 1; j >= 0; j--)
				{
					string mode = modes[j];

					if (_kerbnets.ContainsKey(mode))
					{
						KerbNetStorage storage = _kerbnets[mode];

						float min = access.GetKerbNetMinimumFoV();

						if (min < storage.BestMinFoV)
							storage.BestMinFoV = min;

						float max = access.GetKerbNetMaximumFoV();

						if (max > storage.BestMaxFoV)
							storage.BestMaxFoV = max;

						float anom = access.GetKerbNetAnomalyChance();

						if (anom > storage.BestAnomalyChance)
							storage.BestAnomalyChance = anom;

						if (max - min > storage.WidestRange)
						{
							storage.WidestRange = max - min;
							storage.MinFoV = min;
							storage.MaxFoV = max;
							storage.CurrentPart = access.GetKerbNetPart();
							storage.Accessor = access;
						}

						if (min < _bestMinFoV)
							_bestMinFoV = min;

						if (max > _bestMaxFoV)
							_bestMaxFoV = max;

						if (anom > _bestAnomaly)
							_bestAnomaly = anom;

						if (mode == "Resources")
							UpdateResourceModes(storage);
					}
					else
					{
						KerbNetStorage storage = new KerbNetStorage(mode);

						storage.MinFoV = access.GetKerbNetMinimumFoV();
						storage.BestMinFoV = storage.MinFoV;
						storage.MaxFoV = access.GetKerbNetMaximumFoV();
						storage.BestMaxFoV = storage.MaxFoV; ;
						storage.BestAnomalyChance = access.GetKerbNetAnomalyChance();
						storage.WidestRange = storage.MaxFoV - storage.MinFoV;
						storage.CurrentPart = access.GetKerbNetPart();
						storage.Accessor = access;

						if (storage.MinFoV < _bestMinFoV)
							_bestMinFoV = storage.MinFoV;

						if (storage.MaxFoV > _bestMaxFoV)
							_bestMaxFoV = storage.MaxFoV;

						if (storage.BestAnomalyChance > _bestAnomaly)
							_bestAnomaly = storage.BestAnomalyChance;

						if (mode == "Resources")
						{
							_kerbnets.Add(mode, storage);
							AddResourceModes(storage);
						}
						else
							_kerbnets.Add(mode, storage);
					}
				}
			}
		}

		private void AddResourceModes(KerbNetStorage store)
		{
			if (ResourceMap.Instance == null)
				return;

			List<string> resources = ResourceMap.Instance.FetchAllResourceNames(HarvestTypes.Planetary);

			for (int i = resources.Count - 1; i >= 0; i--)
			{
				string s = resources[i];

				if (string.IsNullOrEmpty(s))
					continue;

				if (!_kerbnets.ContainsKey(s))
					_kerbnets.Add(s, store);
			}
		}

		private void UpdateResourceModes(KerbNetStorage store)
		{
			if (ResourceMap.Instance == null)
				return;

			List<string> resources = ResourceMap.Instance.FetchAllResourceNames(HarvestTypes.Planetary);

			for (int i = resources.Count - 1; i >= 0; i--)
			{
				string s = resources[i];

				if (string.IsNullOrEmpty(s))
					continue;

				if (!_kerbnets.ContainsKey(s))
					continue;

				KerbNetStorage storage = _kerbnets[s];

				storage.WidestRange = store.WidestRange;
				storage.MinFoV = store.MinFoV;
				storage.MaxFoV = store.MaxFoV;
				storage.BestAnomalyChance = store.BestAnomalyChance;
				storage.BestMaxFoV = store.BestMaxFoV;
				storage.BestMinFoV = store.BestMinFoV;
				storage.CurrentPart = store.CurrentPart;
				storage.Accessor = store.Accessor; ;
			}
		}

		private void KillTooltips()
		{
			if (_dialog == null)
				return;

			var tooltips = _dialog.GetComponentsInChildren<TooltipController_Text>(true);

			for (int i = tooltips.Length - 1; i >= 0; i--)
			{
				TooltipController_Text tooltip = tooltips[i];

				if (tooltip == null)
					continue;

				tooltip.SetText("");
			}
		}

		private void SetCloseListener()
		{
			if (_dialog == null)
				return;

			_dialogButtons = _dialog.GetComponentsInChildren<Button>(true);

			_dialogButtons[3].onClick.AddListener(new UnityAction(OnClose));
		}

		private void AddModeListener()
		{
			if (_dialog == null)
				return;

			_stateButtons = _dialog.GetComponentsInChildren<UIStateButton>(true);

			UIStateButton modeButton = _stateButtons[0];

			modeButton.onValueChanged.AddListener(new UnityAction<UIStateButton>(OnModeChange));

			if (_settings.rememberMode && !string.IsNullOrEmpty(_currentMode))
				modeButton.SetState(_currentMode, true);
		}

		private void SetOverlayListener()
		{
			if (_dialog == null)
				return;

			if (_stateButtons == null)
				return;

			UIStateButton overlayButton = _stateButtons[1];

			overlayButton.onValueChanged.AddListener(new UnityAction<UIStateButton>(OnOverylayChange));

			if (!string.IsNullOrEmpty(_visibilityMode))
				overlayButton.SetState(_visibilityMode, true);
		}

		private void SetAutoRefreshListener()
		{
			if (_dialog == null)
				return;

			if (_stateButtons == null)
				return;

			UIStateButton refreshButton = _stateButtons[2];

			refreshButton.onValueChanged.AddListener(new UnityAction<UIStateButton>(OnRefreshChange));

			if (!string.IsNullOrEmpty(_autoRefreshState))
				refreshButton.SetState(_autoRefreshState, true);
		}

		private void SetFoVListener()
		{
			if (_dialog == null)
				return;

			if (_dialogButtons == null)
				_dialogButtons = _dialog.GetComponentsInChildren<Button>(true);

			_dialogButtons[5].onClick.AddListener(new UnityAction(OnFoVChange));

			Slider fovSlider = _dialog.GetComponentInChildren<Slider>(true);

			fovSlider.onValueChanged.AddListener(new UnityAction<float>(OnFoVChange));

			if (_currentFoV > 0)
			{
				if (_currentFoV < _currentMinFoV)
					_currentFoV = _currentMinFoV;

				if (_currentFoV > _currentMaxFoV)
					_currentFoV = _currentMaxFoV;

				_dialog.fovCurrent = _currentFoV;

				if (fovSlider != null)
					fovSlider.value = _currentFoV;
			}
		}

		private void SetOrientationButton()
		{
			if (_dialog == null)
				return;

			if (_stateButtons == null)
				return;

			UIStateButton refreshButton = _stateButtons[2];

			GameObject parent = refreshButton.transform.parent.gameObject;			

			GameObject outline = MonoBehaviour.Instantiate(parent, parent.transform.parent) as GameObject;

			outline.name = "Outline_Orientation";

			FieldInfo disableField = typeof(KerbNetDialog).GetField("disableOnError", BindingFlags.NonPublic | BindingFlags.Instance);

			var disableObjects = disableField.GetValue(_dialog) as GameObject[];

			if (disableObjects != null)
			{
				List<GameObject> disableList = disableObjects.ToList();

				disableList.Add(outline);

				disableField.SetValue(_dialog, disableList.ToArray());
			}

			GameObject child = outline.GetChild("Button_Auto_Refresh");

			if (child != null)
			{
				child.transform.SetParent(null);
				MonoBehaviour.DestroyImmediate(child);
			}

			_orientButton = MonoBehaviour.Instantiate(refreshButton, outline.transform) as UIStateButton;

			_orientButton.name = "Button_Orientation";

			RectTransform rect = outline.GetComponent<RectTransform>();

			if (rect != null)
				rect.anchoredPosition = new Vector2(rect.anchoredPosition.x + rect.rect.width, rect.anchoredPosition.y);

			TooltipController_Text tooltip = outline.GetComponentInChildren<TooltipController_Text>();

			if (tooltip != null)
				tooltip.textString = "Toggle Map Orientation";

			_orientButton.states = new ButtonState[]
			{
				new ButtonState() {name = "north_up", normal = _northSprite},
				new ButtonState() {name = "orbit_up", normal = _orbitSprite}
			};

			_orientButton.SetState(GameSettings.KERBNET_ALIGNS_WITH_ORBIT ? "orbit_up" : "north_up", false);

			_orientButton.onValueChanged.AddListener(new UnityAction<UIStateButton>(OnOrientationChange));
		}

		private void OnClose()
		{
			_dialog = null;

			if (KerbNetToolbar.Instance != null)
				KerbNetToolbar.Instance.SetToolbarState(false);
		}

		private void OnOverylayChange(UIStateButton button)
		{
			if (_settings.rememberOverlay)
				_visibilityMode = button.currentState;
		}

		private void OnRefreshChange(UIStateButton button)
		{
			if (_settings.autoRefresh)
				_autoRefreshState = button.currentState;
		}

		private void OnOrientationChange(UIStateButton button)
		{
			if (!_settings.orientationButton)
				return;

			if (button.currentState == "north_up")
				GameSettings.KERBNET_ALIGNS_WITH_ORBIT = false;
			else if (button.currentState == "orbit_up")
				GameSettings.KERBNET_ALIGNS_WITH_ORBIT = true;

			if (_dialog != null)
				UpdateDisplay();
		}

		private void OnFoVChange(float value)
		{
			if (_settings.rememberFoV && _dialog != null)
				_currentFoV = value;
		}

		private void OnFoVChange()
		{
			if (_settings.rememberFoV && _dialog != null)
				_currentFoV = _dialog.fovCurrent;
		}

		private void OnModeChange(UIStateButton button)
		{
			string mode = button.currentState;

			if (!_kerbnets.ContainsKey(mode))
				return;

			_currentMode = mode;

			UpdateValues(mode);

			if (_dialog != null)
				UpdateDisplay();
		}

		private void UpdateValues(string mode)
		{
			if (!_kerbnets.ContainsKey(mode))
				return;

			KerbNetStorage storage = _kerbnets[mode];

			switch(_settings.setting)
			{
				case 0:
				default:
					_currentMinFoV = _bestMinFoV;
					_currentMaxFoV = _bestMaxFoV;
					_currentAnomaly = _bestAnomaly;
					break;
				case 1:
					_currentMinFoV = storage.BestMinFoV;
					_currentMaxFoV = storage.BestMaxFoV;
					_currentAnomaly = storage.BestAnomalyChance;
					break;
				case 2:
					_currentMinFoV = storage.MinFoV;
					_currentMaxFoV = storage.MaxFoV;
					_currentAnomaly = storage.BestAnomalyChance;					
					break;
			}

			_currentPart = storage.CurrentPart;
			_currentInterface = storage.Accessor;
		}

		private void UpdateDisplay()
		{
			_dialog.SetFoVBounds(_currentMinFoV, _currentMaxFoV);
			_dialog.AnomalyChance = _currentAnomaly;
			_dialog.FullRefresh(true, true);
		}

		public float GetKerbNetAnomalyChance()
		{
			//MonoBehaviour.print("[KerbNet Controller] Get Anomaly: " + _currentAnomaly.ToString("N2"));

			return _currentAnomaly;
		}

		public List<string> GetKerbNetDisplayModes()
		{
			//MonoBehaviour.print("[KerbNet Controller] Get Modes: " + _kerbnets.Count);

			return new List<string>(_kerbnets.Keys);
		}

		public string GetKerbNetErrorState()
		{
			//MonoBehaviour.print("[KerbNet Controller] Get Error State");

			if (_currentInterface != null)
				return _currentInterface.GetKerbNetErrorState();

			return "";
		}

		public float GetKerbNetMaximumFoV()
		{
			//MonoBehaviour.print("[KerbNet Controller] Get Max Fov: " + _currentMaxFoV.ToString("N2"));

			return _currentMaxFoV;
		}

		public float GetKerbNetMinimumFoV()
		{
			//MonoBehaviour.print("[KerbNet Controller] Get Min Fov: " + _currentMinFoV.ToString("N2"));

			return _currentMinFoV;
		}

		public Part GetKerbNetPart()
		{
			//MonoBehaviour.print("[KerbNet Controller] Get Part: " + _currentPart.partInfo.title);

			return _currentPart; ;
		}

		public Vessel GetKerbNetVessel()
		{
			//MonoBehaviour.print("[KerbNet Controller] Get Vessel: " + _activeVessel.vesselName);

			return _activeVessel;
		}
    }
}
