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
		private Button[] _dialogButtons;
		private static string _autoRefreshState;
		private static string _currentMode;
		private static string _visibilityMode;
		private static float _currentFoV;

		public ModuleKerbNetDevourer(Vessel v)
		{
			_activeVessel = v;

			_settings = HighLogic.CurrentGame.Parameters.CustomParams<KerbNetDevourerSettings>();
			
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

			AddModeListener();

			SetCloseListener();

			if (!_settings.showTooltips)
				KillTooltips();

			if (_settings.rememberOverlay)
				SetOverlayListener();

			if (_settings.autoRefresh)
				SetAutoRefreshListener();

			if (_settings.rememberFoV)
				SetFoVListener();

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

			MonoBehaviour.print("[KND] Scanning Vessel [" + _activeVessel.vesselName + "]... Found [" + accessors.Count + "] KerbNet Parts");

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
			//MonoBehaviour.print("[KND] Get Anomaly: " + _currentAnomaly.ToString("N2"));

			return _currentAnomaly;
		}

		public List<string> GetKerbNetDisplayModes()
		{
			//MonoBehaviour.print("[KND] Get Modes: " + _kerbnets.Count);

			return new List<string>(_kerbnets.Keys);
		}

		public string GetKerbNetErrorState()
		{
			//MonoBehaviour.print("[KND] Get Error State");

			if (_currentInterface != null)
				return _currentInterface.GetKerbNetErrorState();

			return "";
		}

		public float GetKerbNetMaximumFoV()
		{
			//MonoBehaviour.print("[KND] Get Max Fov: " + _currentMaxFoV.ToString("N2"));

			return _currentMaxFoV;
		}

		public float GetKerbNetMinimumFoV()
		{
			//MonoBehaviour.print("[KND] Get Min Fov: " + _currentMinFoV.ToString("N2"));

			return _currentMinFoV;
		}

		public Part GetKerbNetPart()
		{
			//MonoBehaviour.print("[KND] Get Part: " + _currentPart.partInfo.title);

			return _currentPart; ;
		}

		public Vessel GetKerbNetVessel()
		{
			//MonoBehaviour.print("[KND] Get Vessel: " + _activeVessel.vesselName);

			return _activeVessel;
		}
    }
}
