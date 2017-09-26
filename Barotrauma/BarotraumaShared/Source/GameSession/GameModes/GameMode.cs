﻿using System;
using System.Collections.Generic;

namespace Barotrauma
{
    partial class GameMode
    {
        public static List<GameModePreset> PresetList = new List<GameModePreset>();

        protected DateTime startTime;
        
        protected bool isRunning;
        
        protected GameModePreset preset;

        private string endMessage;

        public virtual Mission Mission
        {
            get { return null; }
        }

        public bool IsRunning
        {
            get { return isRunning; }
        }

        public bool IsSinglePlayer
        {
            get { return preset.IsSinglePlayer; }
        }

        public string Name
        {
            get { return preset.Name; }
        }

        public string EndMessage
        {
            get { return endMessage; }
        }

        public GameModePreset Preset
        {
            get { return preset; }
        }

        public GameMode(GameModePreset preset, object param)
        {
            this.preset = preset;
        }

        public virtual void Start()
        {
            startTime = DateTime.Now;

            endMessage = "The round has ended!";

            isRunning = true;
        }

        public virtual void MsgBox() { }

        public virtual void AddToGUIUpdateList() { }

        public virtual void Update(float deltaTime) { }

        public virtual void End(string endMessage = "")
        {
            isRunning = false;

            if (endMessage != "" || this.endMessage == null) this.endMessage = endMessage;

            GameMain.GameSession.EndRound(endMessage);
        }
        

    }
}
