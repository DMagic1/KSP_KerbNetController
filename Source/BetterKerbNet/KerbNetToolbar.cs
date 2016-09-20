#region license
/*The MIT License (MIT)
KerbNetToolbar - A MonoBehaviour for setuping the toolbar button
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
using KSP.UI.Screens;
using UnityEngine;

namespace BetterKerbNet
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class KerbNetToolbar : MonoBehaviour
	{
		private ApplicationLauncherButton button;
		private static Texture2D icon;
		private ModuleKerbNetDevourer devourer;
		private int timer;

		private static KerbNetToolbar instance;

		public static KerbNetToolbar Instance
		{
			get { return instance; }
		}

		public void SetToolbarState(bool isOn)
		{
			if (button != null)
				button.SetFalse(false);
		}

		public void SetToolbarEnabled(bool isOn)
		{
			if (button == null)
				return;

			if (isOn)
				button.Enable(false);
			else
				button.Disable(false);
		}

		private void Start()
		{
			instance = this;

			if (icon == null)
				icon = GameDatabase.Instance.GetTexture("KerbNetController/Resources/Toolbar_Icon", false);

			StartCoroutine(Startup());

			StartCoroutine(AddButton());

			GameEvents.onVesselWasModified.Add(onVesselModified);
			GameEvents.onVesselSituationChange.Add(onSituationChange);
			GameEvents.onVesselChange.Add(onVesselChange);
		}

		private void OnDestroy()
		{
			GameEvents.onGUIApplicationLauncherUnreadifying.Remove(RemoveButton);
			GameEvents.onVesselWasModified.Remove(onVesselModified);
			GameEvents.onVesselSituationChange.Remove(onSituationChange);
			GameEvents.onVesselChange.Remove(onVesselChange);
		}

		private IEnumerator Startup()
		{
			while (!FlightGlobals.ready || FlightGlobals.ActiveVessel == null)
				yield return null;
			
			while (timer < 60)
			{
				timer++;
				yield return null;
			}

			devourer = new ModuleKerbNetDevourer(FlightGlobals.ActiveVessel);
		}

		private IEnumerator AddButton()
		{
			while (!ApplicationLauncher.Ready)
				yield return null;

			button = ApplicationLauncher.Instance.AddModApplication(Open, Close, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW, icon);

			GameEvents.onGUIApplicationLauncherUnreadifying.Add(RemoveButton);
		}

		private void RemoveButton(GameScenes scene)
		{
			if (button == null)
				return;

			ApplicationLauncher.Instance.RemoveModApplication(button);
			button = null;

		}

		private void Open()
		{
			if (devourer != null)
				devourer.Open();
		}

		private void Close()
		{
			if (devourer != null)
				devourer.Close();
		}

		private void onVesselChange(Vessel v)
		{
			if (v != FlightGlobals.ActiveVessel)
				return;

			if (devourer == null)
				return;

			devourer.Close();
			devourer.RefreshVessel(v);
		}

		private void onVesselModified(Vessel v)
		{
			if (v != FlightGlobals.ActiveVessel)
				return;

			if (devourer != null)
				devourer.RefreshVessel(v);
		}

		private void onSituationChange(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> VS)
		{
			if (VS.host != FlightGlobals.ActiveVessel)
				return;

			if (devourer != null)
				StartCoroutine(waitForChange(VS.host));
		}

		private IEnumerator waitForChange(Vessel v)
		{
			yield return new WaitForSeconds(0.5f);

			devourer.RefreshVessel(v);
		}
	}
}
