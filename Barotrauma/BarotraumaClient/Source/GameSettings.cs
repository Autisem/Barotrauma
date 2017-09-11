﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Barotrauma.LegacyGUI;

namespace Barotrauma
{
    public enum WindowMode
    {
        Windowed, Fullscreen, BorderlessWindowed
    }

    public partial class GameSettings
    {
        private GUIFrame settingsFrame;
        private GUIButton applyButton;
        
        private float soundVolume, musicVolume;

        private WindowMode windowMode;

        public List<string> jobNamePreferences;

        private KeyOrMouse[] keyMapping;

        private bool unsavedSettings;
        
        public GUIFrame SettingsFrame
        {
            get
            {
                if (settingsFrame == null) CreateSettingsFrame();
                return settingsFrame;
            }
        }

        public KeyOrMouse KeyBind(InputType inputType)
        {
            return keyMapping[(int)inputType];
        }

        public int GraphicsWidth { get; set; }
        public int GraphicsHeight { get; set; }

        public bool VSyncEnabled { get; set; }

        public bool EnableSplashScreen { get; set; }

        //public bool FullScreenEnabled { get; set; }

        public WindowMode WindowMode
        {
            get { return windowMode; }
            set { windowMode = value; }
        }
        
        public List<string> JobNamePreferences
        {
            get { return jobNamePreferences; }
            set
            {
                // Begin saving coroutine. Remove any existing save coroutines if one is running.
                if (CoroutineManager.IsCoroutineRunning("saveCoroutine")) { CoroutineManager.StopCoroutines("saveCoroutine"); }
                CoroutineManager.StartCoroutine(ApplyUnsavedChanges(), "saveCoroutine");

                jobNamePreferences = value;
            }
        }
        
        public bool UnsavedSettings
        {
            get
            {
                return unsavedSettings;
            }
            private set
            {
                unsavedSettings = value;

                if (applyButton != null)
                {
                    //applyButton.Selected = unsavedSettings;
                    applyButton.Enabled = unsavedSettings;
                    applyButton.Text = unsavedSettings ? "Apply*" : "Apply";
                }
            }
        }

        public float SoundVolume
        {
            get { return soundVolume; }
            set
            {
                soundVolume = MathHelper.Clamp(value, 0.0f, 1.0f);
                Sounds.SoundManager.MasterVolume = soundVolume;
            }
        }

        public float MusicVolume
        {
            get { return musicVolume; }
            set
            {
                musicVolume = MathHelper.Clamp(value, 0.0f, 1.0f);
                SoundPlayer.MusicVolume = musicVolume;
            }
        }

        partial void InitProjSpecific(XDocument doc)
        {
            if (doc == null)
            {
                GraphicsWidth = 1024;
                GraphicsHeight = 678;
                
                MasterServerUrl = "";
                
                SelectedContentPackage = ContentPackage.list.Any() ? ContentPackage.list[0] : new ContentPackage("");

                JobNamePreferences = new List<string>();
                foreach (JobPrefab job in JobPrefab.List)
                {
                    JobNamePreferences.Add(job.Name);
                }
                return;
            }

            XElement graphicsMode = doc.Root.Element("graphicsmode");
            GraphicsWidth = ToolBox.GetAttributeInt(graphicsMode, "width", 0);
            GraphicsHeight = ToolBox.GetAttributeInt(graphicsMode, "height", 0);
            VSyncEnabled = ToolBox.GetAttributeBool(graphicsMode, "vsync", true);

            if (GraphicsWidth == 0 || GraphicsHeight == 0)
            {
                GraphicsWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                GraphicsHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }

            //FullScreenEnabled = ToolBox.GetAttributeBool(graphicsMode, "fullscreen", true);

            var windowModeStr = ToolBox.GetAttributeString(graphicsMode, "displaymode", "Fullscreen");
            if (!Enum.TryParse<WindowMode>(windowModeStr, out windowMode))
            {
                windowMode = WindowMode.Fullscreen;
            }

            SoundVolume = ToolBox.GetAttributeFloat(doc.Root, "soundvolume", 1.0f);
            MusicVolume = ToolBox.GetAttributeFloat(doc.Root, "musicvolume", 0.3f);

            EnableSplashScreen = ToolBox.GetAttributeBool(doc.Root, "enablesplashscreen", true);

            keyMapping = new KeyOrMouse[Enum.GetNames(typeof(InputType)).Length];
            keyMapping[(int)InputType.Up] = new KeyOrMouse(Keys.W);
            keyMapping[(int)InputType.Down] = new KeyOrMouse(Keys.S);
            keyMapping[(int)InputType.Left] = new KeyOrMouse(Keys.A);
            keyMapping[(int)InputType.Right] = new KeyOrMouse(Keys.D);
            keyMapping[(int)InputType.Run] = new KeyOrMouse(Keys.LeftShift);


            keyMapping[(int)InputType.Chat] = new KeyOrMouse(Keys.Tab);
            keyMapping[(int)InputType.CrewOrders] = new KeyOrMouse(Keys.C);

            keyMapping[(int)InputType.Select] = new KeyOrMouse(Keys.E);

            keyMapping[(int)InputType.Use] = new KeyOrMouse(0);
            keyMapping[(int)InputType.Aim] = new KeyOrMouse(1);

            foreach (XElement subElement in doc.Root.Elements())
            {
                switch (subElement.Name.ToString().ToLowerInvariant())
                {
                    case "keymapping":
                        foreach (XAttribute attribute in subElement.Attributes())
                        {
                            InputType inputType;
                            if (Enum.TryParse(attribute.Name.ToString(), true, out inputType))
                            {
                                int mouseButton;
                                if (int.TryParse(attribute.Value.ToString(), out mouseButton))
                                {
                                    keyMapping[(int)inputType] = new KeyOrMouse(mouseButton);
                                }
                                else
                                {
                                    Keys key;
                                    if (Enum.TryParse(attribute.Value.ToString(), true, out key))
                                    {
                                        keyMapping[(int)inputType] = new KeyOrMouse(key);
                                    }
                                }
                            }
                        }
                        break;
                    case "gameplay":
                        JobNamePreferences = new List<string>();
                        foreach (XElement ele in subElement.Element("jobpreferences").Elements("job"))
                        {
                            JobNamePreferences.Add(ToolBox.GetAttributeString(ele, "name", ""));
                        }
                        break;
                }
            }

            foreach (InputType inputType in Enum.GetValues(typeof(InputType)))
            {
                if (keyMapping[(int)inputType] == null)
                {
                    DebugConsole.ThrowError("Key binding for the input type \"" + inputType + " not set!");
                    keyMapping[(int)inputType] = new KeyOrMouse(Keys.D1);
                }
            }


            UnsavedSettings = false;
        }

        public void Save(string filePath)
        {
            UnsavedSettings = false;

            XDocument doc = new XDocument();

            if (doc.Root == null)
            {
                doc.Add(new XElement("config"));
            }

            doc.Root.Add(
                new XAttribute("masterserverurl", MasterServerUrl),
                new XAttribute("autocheckupdates", AutoCheckUpdates),
                new XAttribute("musicvolume", musicVolume),
                new XAttribute("soundvolume", soundVolume),
                new XAttribute("verboselogging", VerboseLogging),
                new XAttribute("enablesplashscreen", EnableSplashScreen));

            if (WasGameUpdated)
            {
                doc.Root.Add(new XAttribute("wasgameupdated", true));
            }

            XElement gMode = doc.Root.Element("graphicsmode");
            if (gMode == null)
            {
                gMode = new XElement("graphicsmode");
                doc.Root.Add(gMode);
            }

            if (GraphicsWidth == 0 || GraphicsHeight == 0)
            {
                gMode.ReplaceAttributes(new XAttribute("displaymode", windowMode));
            }
            else
            {
                gMode.ReplaceAttributes(
                    new XAttribute("width", GraphicsWidth),
                    new XAttribute("height", GraphicsHeight),
                    new XAttribute("vsync", VSyncEnabled),
                    new XAttribute("displaymode", windowMode));
            }


            if (SelectedContentPackage != null)
            {
                doc.Root.Add(new XElement("contentpackage",
                    new XAttribute("path", SelectedContentPackage.Path)));
            }

            var keyMappingElement = new XElement("keymapping");
            doc.Root.Add(keyMappingElement);
            for (int i = 0; i < keyMapping.Length; i++)
            {
                if (keyMapping[i].MouseButton == null)
                {
                    keyMappingElement.Add(new XAttribute(((InputType)i).ToString(), keyMapping[i].Key));
                }
                else
                {
                    keyMappingElement.Add(new XAttribute(((InputType)i).ToString(), keyMapping[i].MouseButton));
                }


            }

            var gameplay = new XElement("gameplay");

            var jobPreferences = new XElement("jobpreferences");
            foreach (string jobName in JobNamePreferences)
            {
                jobPreferences.Add(new XElement("job", new XAttribute("name", jobName)));
            }

            gameplay.Add(jobPreferences);
            doc.Root.Add(gameplay);

            doc.Save(filePath);
        }

        private bool ChangeSoundVolume(GUIScrollBar scrollBar, float barScroll)
        {
            UnsavedSettings = true;
            SoundVolume = barScroll;

            return true;
        }

        private bool ChangeMusicVolume(GUIScrollBar scrollBar, float barScroll)
        {
            UnsavedSettings = true;
            MusicVolume = barScroll;

            return true;
        }

        public void ResetSettingsFrame()
        {
            settingsFrame = null;
        }

        private void CreateSettingsFrame()
        {
            settingsFrame = new GUIFrame(new Rectangle(0, 0, 500, 500), null, Alignment.Center, "");

            new GUITextBlock(new Rectangle(0, -30, 0, 30), "Settings", "", Alignment.TopCenter, Alignment.TopCenter, settingsFrame, false, GUI.LargeFont);

            int x = 0, y = 10;

            new GUITextBlock(new Rectangle(0, y, 20, 20), "Resolution", "", Alignment.TopLeft, Alignment.TopLeft, settingsFrame);
            var resolutionDD = new GUIDropDown(new Rectangle(0, y + 20, 180, 20), "", "", settingsFrame);
            resolutionDD.OnSelected = SelectResolution;

            var supportedModes = new List<DisplayMode>();
            foreach (DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                if (supportedModes.FirstOrDefault(m => m.Width == mode.Width && m.Height == mode.Height) != null) continue;

                resolutionDD.AddItem(mode.Width + "x" + mode.Height, mode);
                supportedModes.Add(mode);

                if (GraphicsWidth == mode.Width && GraphicsHeight == mode.Height) resolutionDD.SelectItem(mode);
            }

            if (resolutionDD.SelectedItemData == null)
            {
                resolutionDD.SelectItem(GraphicsAdapter.DefaultAdapter.SupportedDisplayModes.Last());
            }

            y += 50;

            //var fullScreenTick = new GUITickBox(new Rectangle(x, y, 20, 20), "Fullscreen", Alignment.TopLeft, settingsFrame);
            //fullScreenTick.OnSelected = ToggleFullScreen;
            //fullScreenTick.Selected = FullScreenEnabled;

            new GUITextBlock(new Rectangle(x, y, 20, 20), "Display mode", "", Alignment.TopLeft, Alignment.TopLeft, settingsFrame);
            var displayModeDD = new GUIDropDown(new Rectangle(x, y + 20, 180, 20), "", "", settingsFrame);
            displayModeDD.AddItem("Fullscreen", WindowMode.Fullscreen);
            displayModeDD.AddItem("Windowed", WindowMode.Windowed);
            displayModeDD.AddItem("Borderless windowed", WindowMode.BorderlessWindowed);

            displayModeDD.SelectItem(GameMain.Config.WindowMode);

            displayModeDD.OnSelected = (guiComponent, obj) => { GameMain.Config.WindowMode = (WindowMode)guiComponent.UserData; return true; };

            y += 70;

            GUITickBox vsyncTickBox = new GUITickBox(new Rectangle(0, y, 20, 20), "Enable vertical sync", Alignment.CenterY | Alignment.Left, settingsFrame);
            vsyncTickBox.OnSelected = (GUITickBox box) =>
            {
                VSyncEnabled = !VSyncEnabled;
                GameMain.GraphicsDeviceManager.SynchronizeWithVerticalRetrace = VSyncEnabled;
                GameMain.GraphicsDeviceManager.ApplyChanges();
                UnsavedSettings = true;

                return true;
            };
            vsyncTickBox.Selected = VSyncEnabled;

            y += 70;

            new GUITextBlock(new Rectangle(0, y, 100, 20), "Sound volume:", "", settingsFrame);
            GUIScrollBar soundScrollBar = new GUIScrollBar(new Rectangle(0, y + 20, 150, 20), "", 0.1f, settingsFrame);
            soundScrollBar.BarScroll = SoundVolume;
            soundScrollBar.OnMoved = ChangeSoundVolume;
            soundScrollBar.Step = 0.05f;

            new GUITextBlock(new Rectangle(0, y + 40, 100, 20), "Music volume:", "", settingsFrame);
            GUIScrollBar musicScrollBar = new GUIScrollBar(new Rectangle(0, y + 60, 150, 20), "", 0.1f, settingsFrame);
            musicScrollBar.BarScroll = MusicVolume;
            musicScrollBar.OnMoved = ChangeMusicVolume;
            musicScrollBar.Step = 0.05f;

            x = 200;
            y = 10;

            new GUITextBlock(new Rectangle(x, y, 20, 20), "Content package", "", Alignment.TopLeft, Alignment.TopLeft, settingsFrame);
            var contentPackageDD = new GUIDropDown(new Rectangle(x, y + 20, 200, 20), "", "", settingsFrame);

            foreach (ContentPackage contentPackage in ContentPackage.list)
            {
                contentPackageDD.AddItem(contentPackage.Name, contentPackage);

                if (SelectedContentPackage == contentPackage) contentPackageDD.SelectItem(contentPackage);
            }

            y += 50;
            new GUITextBlock(new Rectangle(x, y, 100, 20), "Controls:", "", settingsFrame);
            y += 30;
            var inputNames = Enum.GetNames(typeof(InputType));
            for (int i = 0; i < inputNames.Length; i++)
            {
                new GUITextBlock(new Rectangle(x, y, 100, 18), inputNames[i] + ": ", "", Alignment.TopLeft, Alignment.CenterLeft, settingsFrame);
                var keyBox = new GUITextBox(new Rectangle(x + 100, y, 120, 18), null, null, Alignment.TopLeft, Alignment.CenterLeft, "", settingsFrame);

                keyBox.Text = keyMapping[i].ToString();
                keyBox.UserData = i;
                keyBox.OnSelected += KeyBoxSelected;
                keyBox.SelectedColor = Color.Gold * 0.3f;

                y += 20;
            }

            applyButton = new GUIButton(new Rectangle(0, 0, 100, 20), "Apply", Alignment.BottomRight, "", settingsFrame);
            applyButton.OnClicked = ApplyClicked;
        }

        private void KeyBoxSelected(GUITextBox textBox, Keys key)
        {
            textBox.Text = "";
            CoroutineManager.StartCoroutine(WaitForKeyPress(textBox));
        }

        private bool MarkUnappliedChanges(GUIButton button, object obj)
        {
            UnsavedSettings = true;

            return true;
        }

        private bool SelectResolution(GUIComponent selected, object userData)
        {
            DisplayMode mode = selected.UserData as DisplayMode;
            if (mode == null) return false;

            if (GraphicsWidth == mode.Width && GraphicsHeight == mode.Height) return false;

            GraphicsWidth = mode.Width;
            GraphicsHeight = mode.Height;


            //GameMain.Graphics.PreferredBackBufferWidth = GraphicsWidth;
            //GameMain.Graphics.PreferredBackBufferHeight = GraphicsHeight;
            //GameMain.Graphics.ApplyChanges();

            //CoroutineManager.StartCoroutine(GameMain.Instance.Load());

            UnsavedSettings = true;

            return true;
        }



        private IEnumerable<object> WaitForKeyPress(GUITextBox keyBox)
        {
            yield return CoroutineStatus.Running;

            while (keyBox.Selected && PlayerInput.GetKeyboardState.GetPressedKeys().Length == 0
                && !PlayerInput.LeftButtonClicked() && !PlayerInput.RightButtonClicked())
            {
                if (Screen.Selected != GameMain.MainMenuScreen && !GUI.SettingsMenuOpen) yield return CoroutineStatus.Success;

                yield return CoroutineStatus.Running;
            }

            UnsavedSettings = true;

            int keyIndex = (int)keyBox.UserData;

            if (PlayerInput.LeftButtonClicked())
            {
                keyMapping[keyIndex] = new KeyOrMouse(0);
                keyBox.Text = "Mouse1";
            }
            else if (PlayerInput.LeftButtonClicked())
            {
                keyMapping[keyIndex] = new KeyOrMouse(1);
                keyBox.Text = "Mouse2";
            }
            else if (PlayerInput.GetKeyboardState.GetPressedKeys().Length > 0)
            {
                Keys key = PlayerInput.GetKeyboardState.GetPressedKeys()[0];
                keyMapping[keyIndex] = new KeyOrMouse(key);
                keyBox.Text = key.ToString("G");
            }
            else
            {
                yield return CoroutineStatus.Success;
            }

            keyBox.Deselect();

            yield return CoroutineStatus.Success;
        }

        private IEnumerable<object> ApplyUnsavedChanges()
        {
            yield return new WaitForSeconds(10.0f);

            Save("config.xml");
        }

        private bool ApplyClicked(GUIButton button, object userData)
        {
            Save("config.xml");

            settingsFrame.Flash(Color.Green);

            if (GameMain.GraphicsWidth != GameMain.Config.GraphicsWidth || GameMain.GraphicsHeight != GameMain.Config.GraphicsHeight)
            {
                new GUIMessageBox("Restart required", "You need to restart the game for the resolution changes to take effect.");
            }

            return true;
        }
    }
}
