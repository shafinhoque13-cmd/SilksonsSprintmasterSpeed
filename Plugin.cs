using BepInEx;
using UnityEngine;
using System;

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
        private float _updateTimer = 0f;

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
            else _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "UNIVERSAL SPEED CONTROL");
        }

        void DrawBubble(int windowID)
        {
            if (GUI.Button(new Rect(5, 5, 150, 60), "SPEED MOD\nMENU")) _showMenu = true;
            GUI.DragWindow();
        }

        void DrawMainWindow(int windowID)
        {
            GUILayout.BeginVertical();
            float displayVal = (_selectedMode == 1) ? (1.0f / Mathf.Floor(_level)) : Mathf.Floor(_level);
            GUILayout.Label($"Current Speed: {displayVal:F2}x", GUI.skin.label);

            if (GUILayout.Toggle(_selectedMode == 0, " FAST MODE")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " SLOW MODE")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " NORMAL MODE")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 5f);

            GUILayout.Space(30);
            if (GUILayout.Button("REFRESH ALL OBJECTS", GUILayout.Height(60))) { UniversalUpdate(); }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SAVE & APPLY", GUILayout.Height(70))) { SaveSettings(); _showMenu = false; }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void Update()
        {
            float floorLevel = Mathf.Floor(_level);
            _currentSpeedMult = _selectedMode == 0 ? floorLevel : (_selectedMode == 1 ? 1.0f / floorLevel : 1.0f);

            _updateTimer += Time.deltaTime;
            if (_updateTimer >= 0.8f) // Scan nearly every second
            {
                UniversalUpdate();
                _updateTimer = 0;
            }
        }

        private void UniversalUpdate()
        {
            // KEEP PLAYER PHYSICS NORMAL
            Time.timeScale = 1.0f;

            // Target EVERY Animator in the game
            Animator[] allAnims = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (Animator anim in allAnims)
            {
                if (anim == null) continue;

                // PROTECTION: Check by name and layer to ensure we don't touch Hornet
                string name = anim.gameObject.name.ToLower();
                int layer = anim.gameObject.layer;

                if (name.Contains("hornet") || name.Contains("player") || layer == 9)
                {
                    anim.speed = 1.0f; // Force Player to 1.0
                    continue;
                }

                // SPEED UP EVERYTHING ELSE
                anim.speed = _currentSpeedMult;

                // Force individual script speed if it exists
                anim.gameObject.SendMessage("set_speed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                anim.gameObject.SendMessage("set_timeScale", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
            }

            // Target EVERY Physics body that isn't the player
            Rigidbody2D[] allBodies = UnityEngine.Object.FindObjectsByType<Rigidbody2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Rigidbody2D rb in allBodies)
            {
                if (rb == null || rb.gameObject.layer == 9) continue;

                // Send speed values to FSMs (Flowchart logic)
                rb.gameObject.SendMessage("SetSpeed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                rb.gameObject.SendMessage("SetFsmSpeed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
            }
        }

        void SaveSettings()
        {
            PlayerPrefs.SetInt("Mod_SpeedMode", _selectedMode);
            PlayerPrefs.SetFloat("Mod_SpeedLevel", _level);
            PlayerPrefs.SetFloat("Mod_BubbleX", _bubbleRect.x);
            PlayerPrefs.SetFloat("Mod_BubbleY", _bubbleRect.y);
            PlayerPrefs.Save();
            UniversalUpdate();
        }
    }
}
