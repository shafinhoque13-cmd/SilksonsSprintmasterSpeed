using BepInEx;
using UnityEngine;
using System;
using System.Reflection;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.7.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        private Rect _bubbleRect = new Rect(50, 300, 200, 100); 
        private Rect _windowRect = new Rect(100, 100, 600, 750);
        
        private int _selectedMode = 2; 
        private float _level = 1.0f;
        private float _currentSpeed = 1.0f;

        void Awake()
        {
            _bubbleRect.x = PlayerPrefs.GetFloat("Mod_BubbleX", 50);
            _bubbleRect.y = PlayerPrefs.GetFloat("Mod_BubbleY", 300);
            _selectedMode = PlayerPrefs.GetInt("Mod_SpeedMode", 2);
            _level = PlayerPrefs.GetFloat("Mod_SpeedLevel", 1.0f);
        }

        void OnGUI()
        {
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1));
            if (!_showMenu)
                _bubbleRect = GUI.Window(99, _bubbleRect, DrawBubble, "DRAG");
            else
                _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "VARIABLE SNIPER");
        }

        void DrawBubble(int windowID) {
            if (GUI.Button(new Rect(10, 35, 180, 55), "MENU")) _showMenu = true;
            GUI.DragWindow(new Rect(0, 0, 200, 30));
        }

        void DrawMainWindow(int windowID) {
            GUILayout.BeginVertical();
            float val = Mathf.Floor(_level);
            _currentSpeed = (_selectedMode == 0) ? val : (_selectedMode == 1 ? 1f / val : 1f);
            
            GUILayout.Label($"<size=35>FORCED SPEED: {_currentSpeed:F2}x</size>");
            if (GUILayout.Toggle(_selectedMode == 0, " FAST")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " SLOW")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " NORMAL")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 10f);
            GUILayout.Space(20);
            GUILayout.Label("<color=cyan>Status: Sniping FSM Float Variables...</color>");
            
            if (GUILayout.Button("CLOSE", GUILayout.Height(60))) { 
                PlayerPrefs.Save();
                _showMenu = false; 
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void Update() {
            if (_selectedMode != 2) SnipeVariables();
        }

        private void SnipeVariables() {
            GameObject[] targets = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in targets) {
                if (obj == null) continue;
                // Target Layer 19 and names from your radar
                if (obj.layer == 19 || obj.name.ToLower().Contains("sprintmaster") || obj.name.ToLower().Contains("runner")) {
                    Component[] comps = obj.GetComponents<Component>();
                    foreach (var c in comps) {
                        if (c == null) continue;
                        // Search for the Fsm component specifically
                        if (c.GetType().Name.Contains("PlayMakerFSM")) {
                            try {
                                // Direct Variable Injection via Reflection
                                object fsm = c.GetType().GetProperty("Fsm").GetValue(c, null);
                                object variables = fsm.GetType().GetProperty("Variables").GetValue(fsm, null);
                                Array floatVars = (Array)variables.GetType().GetProperty("FloatVariables").GetValue(variables, null);

                                foreach (object fv in floatVars) {
                                    string varName = (string)fv.GetType().GetProperty("Name").GetValue(fv, null);
                                    // Target common speed variable names
                                    if (varName.ToLower().Contains("speed") || varName.ToLower().Contains("vel")) {
                                        PropertyInfo valProp = fv.GetType().GetProperty("Value");
                                        float original = (float)valProp.GetValue(fv, null);
                                        valProp.SetValue(fv, original * _currentSpeed, null);
                                    }
                                }
                            } catch { /* Silent fail for incompatible FSMs */ }
                        }
                    }
                    // Also force the Animator/TimeScale just in case
                    obj.SendMessage("set_timeScale", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }
}
