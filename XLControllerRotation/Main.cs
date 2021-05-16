using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using XInputDotNetPure;
using static InputThread;

namespace XLControllerRotation {
	[Serializable]
	public class ControllerSettings : UnityModManager.ModSettings {
		public float regularControlRotation = 0f;
		public float switchControlRotation = 0f;
		public bool regularFlipSticks = false;
		public bool switchFlipSticks = false;
		public bool regularInvertX = false;
		public bool switchInvertX = false;
		public bool regularInvertY = false;
		public bool switchInvertY = false;

		public override void Save(UnityModManager.ModEntry modEntry) {
			Save(this, modEntry);
		}
	}

    public class Main {
		private static bool enabled = false;
		private static UnityModManager.ModEntry modEntry;
		private static string modId;
		private static Harmony harmonyInstance;

		public static ControllerSettings settings;

		private static void Load(UnityModManager.ModEntry modEntry) {
			Main.modEntry = modEntry;
			Main.modId = modEntry.Info.Id;

			settings = UnityModManager.ModSettings.Load<ControllerSettings>(modEntry);

			modEntry.OnToggle = OnToggle;
			modEntry.OnGUI = OnSettingsGUI;
			modEntry.OnSaveGUI = OnSaveGUI;
		}

		private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
			if (value == enabled) return true;
			enabled = value;

			if (enabled) {
				harmonyInstance = new Harmony(modEntry.Info.Id);
				harmonyInstance.PatchAll();
			} else {
				harmonyInstance.UnpatchAll();
			}

			return true;
		}

		private static void OnSettingsGUI(UnityModManager.ModEntry obj) {
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Regular Rotation", GUILayout.MinWidth(100));
				GUILayout.FlexibleSpace();
				float newRotation = settings.regularControlRotation;
				if (float.TryParse(GUILayout.TextField(settings.regularControlRotation.ToString("0."), GUILayout.Width(50)), out float value)) {
					newRotation = value;
				}
				newRotation = GUILayout.HorizontalSlider(newRotation, 0f, 360f, GUILayout.MinWidth(450));
				if (newRotation != settings.regularControlRotation) {
					settings.regularControlRotation = newRotation;
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Switch Rotation", GUILayout.MinWidth(100));
				GUILayout.FlexibleSpace();
				float newRotation = settings.switchControlRotation;
				if (float.TryParse(GUILayout.TextField(settings.switchControlRotation.ToString("0."), GUILayout.Width(50)), out float value)) {
					newRotation = value;
				}
				newRotation = GUILayout.HorizontalSlider(newRotation, 0f, 360f, GUILayout.MinWidth(450));
				if (newRotation != settings.switchControlRotation) {
					settings.switchControlRotation = newRotation;
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Swap Sticks", GUILayout.MinWidth(100));
				settings.regularFlipSticks = GUILayout.Toggle(settings.regularFlipSticks, "Regular", GUILayout.MinWidth(100));
				settings.switchFlipSticks = GUILayout.Toggle(settings.switchFlipSticks, "Switch", GUILayout.MinWidth(100));
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Invert X Axis", GUILayout.MinWidth(100));
				settings.regularInvertX = GUILayout.Toggle(settings.regularInvertX, "Regular", GUILayout.MinWidth(100));
				settings.switchInvertX = GUILayout.Toggle(settings.switchInvertX, "Switch", GUILayout.MinWidth(100));
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Invert Y Axis", GUILayout.MinWidth(100));
				settings.regularInvertY = GUILayout.Toggle(settings.regularInvertY, "Regular", GUILayout.MinWidth(100));
				settings.switchInvertY = GUILayout.Toggle(settings.switchInvertY, "Switch", GUILayout.MinWidth(100));
			}
			GUILayout.EndHorizontal();
		}
		
		static void OnSaveGUI(UnityModManager.ModEntry modEntry) {
			settings.Save(modEntry);
		}


		[HarmonyPatch(typeof(InputThread), "InputUpdate")]
		static class wowWTF {
			static Vector2 RotateInputVector2(float x, float y) {
				Vector2 inputDir = new Vector2(x, y);
				float rot = Mathf.Deg2Rad * (PlayerController.Instance.IsSwitch ? Main.settings.switchControlRotation : Main.settings.regularControlRotation);
				Vector2 newInput = new Vector2(inputDir.x * Mathf.Cos(rot) - inputDir.y * Mathf.Sin(rot), inputDir.y * Mathf.Cos(rot) + inputDir.x * Mathf.Sin(rot));
				return newInput;
			}

			static void RotateInput(ref InputStruct __instance) {
				Vector2 leftInput = RotateInputVector2(__instance.leftX, __instance.leftY);
				Vector2 rightInput = RotateInputVector2(__instance.rightX, __instance.rightY);

				if ((settings.switchFlipSticks && PlayerController.Instance.IsSwitch) || (settings.regularFlipSticks && !PlayerController.Instance.IsSwitch)) {
					__instance.leftX = rightInput.x;
					__instance.leftY = rightInput.y;
					__instance.rightX = leftInput.x;
					__instance.rightY = leftInput.y;
				} else {
					__instance.leftX = leftInput.x;
					__instance.leftY = leftInput.y;
					__instance.rightX = rightInput.x;
					__instance.rightY = rightInput.y;
				}

				if ((settings.switchInvertX && PlayerController.Instance.IsSwitch) || (settings.regularInvertX && !PlayerController.Instance.IsSwitch)) {
					__instance.leftX = -__instance.leftX;
					__instance.rightX = -__instance.rightX;
				}

				if ((settings.switchInvertY && PlayerController.Instance.IsSwitch) || (settings.regularInvertY && !PlayerController.Instance.IsSwitch))
				{
					__instance.leftY = -__instance.leftY;
					__instance.rightY = -__instance.rightY;
				}
			}

			static bool Prefix(InputThread __instance) {
				int _pos = Traverse.Create(__instance).Field("_pos").GetValue<int>();
				GamePadState state = Traverse.Create(__instance).Field("state").GetValue<GamePadState>();
				InputStruct _lastFrameData = Traverse.Create(__instance).Field("_lastFrameData").GetValue<InputStruct>();

				if (_pos < __instance._maxLength) {
					if (state.IsConnected) {
						__instance.inputsIn[_pos].leftX = __instance.leftXFilter.Filter((double)state.ThumbSticks.Left.X);
						__instance.inputsIn[_pos].leftY = __instance.leftYFilter.Filter((double)state.ThumbSticks.Left.Y);
						__instance.inputsIn[_pos].rightX = __instance.rightXFilter.Filter((double)state.ThumbSticks.Right.X);
						__instance.inputsIn[_pos].rightY = __instance.rightYFilter.Filter((double)state.ThumbSticks.Right.Y);
						RotateInput(ref __instance.inputsIn[_pos]);
						__instance.inputsIn[_pos].time = DateTime.UtcNow.Ticks;
						__instance.inputsIn[_pos].leftXVel = Traverse.Create(__instance).Method("GetVel", __instance.inputsIn[_pos].leftX, _lastFrameData.leftX, __instance.inputsIn[_pos].time, _lastFrameData.time).GetValue<float>();
						__instance.inputsIn[_pos].leftYVel = Traverse.Create(__instance).Method("GetVel", __instance.inputsIn[_pos].leftY, _lastFrameData.leftY, __instance.inputsIn[_pos].time, _lastFrameData.time).GetValue<float>();
						__instance.inputsIn[_pos].rightXVel = Traverse.Create(__instance).Method("GetVel", __instance.inputsIn[_pos].rightX, _lastFrameData.rightX, __instance.inputsIn[_pos].time, _lastFrameData.time).GetValue<float>();
						__instance.inputsIn[_pos].rightYVel = Traverse.Create(__instance).Method("GetVel", __instance.inputsIn[_pos].rightY, _lastFrameData.rightY, __instance.inputsIn[_pos].time, _lastFrameData.time).GetValue<float>();
						_lastFrameData.leftX = __instance.inputsIn[_pos].leftX;
						_lastFrameData.leftY = __instance.inputsIn[_pos].leftY;
						_lastFrameData.rightX = __instance.inputsIn[_pos].rightX;
						_lastFrameData.rightY = __instance.inputsIn[_pos].rightY;
						_lastFrameData.time = __instance.inputsIn[_pos].time;
						Traverse.Create(__instance).Field("_lastFrameData").SetValue(_lastFrameData);
						Traverse.Create(__instance).Field("_pos").SetValue(_pos + 1);
						return false;
					}
					if (__instance.inputController == null || Traverse.Create(__instance.inputController).Field("player").GetValue() == null) {
						return false;
					}
					__instance.inputsIn[_pos].leftX = __instance.leftXFilter.Filter((double)((Mathf.Abs(Traverse.Create(__instance).Method("GetAxisLX").GetValue<float>()) < 0.1f) ? 0f : Traverse.Create(__instance).Method("GetAxisLX").GetValue<float>()));
					__instance.inputsIn[_pos].leftY = __instance.leftYFilter.Filter((double)Traverse.Create(__instance).Method("GetAxisLY").GetValue<float>());
					__instance.inputsIn[_pos].rightX = __instance.rightXFilter.Filter((double)((Mathf.Abs(Traverse.Create(__instance).Method("GetAxisRX").GetValue<float>()) < 0.1f) ? 0f : Traverse.Create(__instance).Method("GetAxisRX").GetValue<float>()));
					__instance.inputsIn[_pos].rightY = __instance.rightYFilter.Filter((double)Traverse.Create(__instance).Method("GetAxisRY").GetValue<float>());
					RotateInput(ref __instance.inputsIn[_pos]);
					__instance.inputsIn[_pos].time = DateTime.UtcNow.Ticks;
					__instance.inputsIn[_pos].leftXVel = Traverse.Create(__instance).Method("GetVel", __instance.inputsIn[_pos].leftX, _lastFrameData.leftX, __instance.inputsIn[_pos].time, _lastFrameData.time).GetValue<float>();
					__instance.inputsIn[_pos].leftYVel = Traverse.Create(__instance).Method("GetVel", __instance.inputsIn[_pos].leftY, _lastFrameData.leftY, __instance.inputsIn[_pos].time, _lastFrameData.time).GetValue<float>();
					__instance.inputsIn[_pos].rightXVel = Traverse.Create(__instance).Method("GetVel", __instance.inputsIn[_pos].rightX, _lastFrameData.rightX, __instance.inputsIn[_pos].time, _lastFrameData.time).GetValue<float>();
					__instance.inputsIn[_pos].rightYVel = Traverse.Create(__instance).Method("GetVel", __instance.inputsIn[_pos].rightY, _lastFrameData.rightY, __instance.inputsIn[_pos].time, _lastFrameData.time).GetValue<float>();
					_lastFrameData.leftX = __instance.inputsIn[_pos].leftX;
					_lastFrameData.leftY = __instance.inputsIn[_pos].leftY;
					_lastFrameData.rightX = __instance.inputsIn[_pos].rightX;
					_lastFrameData.rightY = __instance.inputsIn[_pos].rightY;
					_lastFrameData.time = __instance.inputsIn[_pos].time;
					Traverse.Create(__instance).Field("_lastFrameData").SetValue(_lastFrameData);
					Traverse.Create(__instance).Field("_pos").SetValue(_pos + 1);
				}
				return false;
			}
		}
	}
}
