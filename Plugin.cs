using BepInEx;
using UnityEngine;
using System;
using System.Reflection;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        private Rect _bubbleRect = new Rect(20, 300, 160, 70); 
        private Rect _windowRect = new Rect(100, 100, 450, 600);
        
        private int _selectedMode = 2; 
        private float _level = 1.0f;
        private float _currentSpeedMult = 1.0f;
        private float _scanTimer = 0f;

        void Awake()
        {
            _bubbleRect.x = PlayerPrefs.GetFloat("Mod_BubbleX", 20);
            _bubbleRect.y = PlayerPrefs.GetFloat("Mod_BubbleY", 300);
            _selectedMode = PlayerPrefs.GetInt("Mod_SpeedMode", 2);
            _level = PlayerPrefs.GetFloat("Mod_SpeedLevel", 1.0f);
        }

        void OnGUI()
        {
            if (!_showMenu) _bubbleRect = GUI.Window(99, _bubbleRect, DrawBubble, "");
            else _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "NPC LOGIC SPEED");
        }

        void DrawBubble(int windowID) {
            if (GUI.Button(new Rect(5, 5, 150, 60), "SPEED MENU")) _showMenu = true;
            GUI.DragWindow();
        }

        void DrawMainWindow(int windowID) {
            GUILayout.BeginVertical();
            float displayVal = (_selectedMode == 1) ? (1.0f / Mathf.Floor(_level)) : Mathf.Floor(_level);
            GUILayout.Label($"Logic Multiplier: {displayVal:F2}x");

            if (GUILayout.Toggle(_selectedMode == 0, " FAST")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " SLOW")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " NORMAL")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 5f);
            
            GUILayout.Space(20);
            if (GUILayout.Button("FORCE UPDATE AREA", GUILayout.Height(60))) { ApplyDeepLogic(); }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SAVE & APPLY", GUILayout.Height(70))) { SaveSettings(); _showMenu = false; }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void Update()
        {
            float floorLevel = Mathf.Floor(_level);
            _currentSpeedMult = _selectedMode == 0 ? floorLevel : (_selectedMode == 1 ? 1.0f / floorLevel : 1.0f);

            _scanTimer += Time.deltaTime;
            if (_scanTimer >= 0.7f) {
                ApplyDeepLogic();
                _scanTimer = 0;
            }
        }

        private void ApplyDeepLogic()
        {
            // Keep player physics at 1.0
            Time.timeScale = 1.0f;

            // Search for all components that might hold speed logic
            Component[] allComponents = UnityEngine.Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (var comp in allComponents)
            {
                if (comp == null || comp.gameObject == null) continue;
                
                // IGNORE HORNET/PLAYER (Layer 9)
                if (comp.gameObject.layer == 9 || comp.gameObject.name.ToLower().Contains("hornet")) continue;

                // 1. Target standard Animators
                if (comp is Animator anim) {
                    anim.speed = _currentSpeedMult;
                }

                // 2. Target Spine Animations (SkeletonAnimation / SkeletonRenderer)
                // We use SendMessage because the DLLs might not be referenced directly
                comp.gameObject.SendMessage("set_timeScale", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);

                // 3. Target Custom Movement Scripts
                // Many NPCs use a 'speed' or 'walkSpeed' variable
                comp.gameObject.SendMessage("SetSpeed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                comp.gameObject.SendMessage("set_speed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);

                // 4. Target PlayMaker FSMs
                // This forces FSM timers to scale
                comp.gameObject.SendMessage("SetFsmSpeed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
            }
        }

        void SaveSettings() {
            PlayerPrefs.SetInt("Mod_SpeedMode", _selectedMode);
            PlayerPrefs.SetFloat("Mod_SpeedLevel", _level);
            PlayerPrefs.SetFloat("Mod_BubbleX", _bubbleRect.x);
            PlayerPrefs.SetFloat("Mod_BubbleY", _bubbleRect.y);
            PlayerPrefs.Save();
            ApplyDeepLogic();
        }
    }
}
