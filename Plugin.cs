using BepInEx;
using UnityEngine;
using System;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        private Rect _windowRect = new Rect(100, 100, 400, 450);
        
        // Settings to Save
        private int _selectedMode = 2; // 0=Inc, 1=Dec, 2=Norm
        private float _level = 1.0f;

        void Awake()
        {
            // Load saved settings when the game starts
            _selectedMode = PlayerPrefs.GetInt("Mod_SpeedMode", 2);
            _level = PlayerPrefs.GetFloat("Mod_SpeedLevel", 1.0f);
            Logger.LogInfo("Speed Mod: Settings Loaded from Memory");
        }

        void OnGUI()
        {
            // 1. Small "Floating Bubble" to open/re-open the menu
            if (!_showMenu)
            {
                if (GUI.Button(new Rect(10, 150, 100, 50), "MOD MENU")) _showMenu = true;
            }

            // 2. The Main Window
            if (_showMenu)
            {
                _windowRect = GUI.Window(0, _windowRect, DrawWindow, "MASTER MOD CONTROL");
            }
        }

        void DrawWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.Label("--- WORLD SPEED SETTINGS ---", GUI.skin.label);

            // Increase Mode
            if (GUILayout.Toggle(_selectedMode == 0, " INCREASE SPEED")) _selectedMode = 0;
            if (_selectedMode == 0)
            {
                GUILayout.Label($"Level: {(int)_level}");
                _level = GUILayout.HorizontalSlider(_level, 1f, 5f);
            }

            GUILayout.Space(10);

            // Decrease Mode
            if (GUILayout.Toggle(_selectedMode == 1, " DECREASE SPEED")) _selectedMode = 1;
            if (_selectedMode == 1)
            {
                GUILayout.Label($"Level: {(int)_level}");
                _level = GUILayout.HorizontalSlider(_level, 1f, 5f);
            }

            GUILayout.Space(10);

            // Normal Mode
            if (GUILayout.Toggle(_selectedMode == 2, " NORMAL (OFF)")) _selectedMode = 2;

            GUILayout.FlexibleSpace();

            // SAVE AND CLOSE BUTTON
            GUI.color = Color.green;
            if (GUILayout.Button("SAVE & CLOSE MENU", GUILayout.Height(50)))
            {
                SaveSettings();
                _showMenu = false;
            }
            GUI.color = Color.white;

            GUILayout.EndVertical();
            GUI.DragWindow(); // Makes the window draggable
        }

        void SaveSettings()
        {
            PlayerPrefs.SetInt("Mod_SpeedMode", _selectedMode);
            PlayerPrefs.SetFloat("Mod_SpeedLevel", _level);
            PlayerPrefs.Save();
            Logger.LogInfo("Speed Mod: Settings Saved!");
        }

        void Update()
        {
            float targetSpeed = 1.0f;
            if (_selectedMode == 0) targetSpeed = 1.0f + ((float)Math.Floor(_level) * 0.8f);
            else if (_selectedMode == 1) targetSpeed = 1.0f - ((float)Math.Floor(_level) * 0.15f);

            ApplySpeed(targetSpeed);
        }

        private void ApplySpeed(float speed)
        {
            // Improved Filter to ignore UI and Dialogues
            Animator[] anims = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var anim in anims)
            {
                if (anim == null) continue;
                GameObject obj = anim.gameObject;

                // Protect Player (9), UI (5), and common Dialogue/Menu names
                if (obj.layer == 9 || obj.layer == 5 || obj.name.ToLower().Contains("ui") || obj.name.ToLower().Contains("dialogue"))
                {
                    anim.speed = 1.0f;
                    continue;
                }
                anim.speed = speed;
            }
        }
    }
}
