﻿using System;
using System.Threading;
#if WINDOWS
using System.Windows;
#endif
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EventInput
{
    public interface IKeyboardSubscriber
    {
        void ReceiveTextInput(char inputChar);
        void ReceiveTextInput(string text);
        void ReceiveCommandInput(char command);
        void ReceiveSpecialInput(Keys key);

        bool Selected { get; set; } //or Focused
    }

    public class KeyboardDispatcher
    {
        public KeyboardDispatcher(GameWindow window)
        {
            EventInput.Initialize(window);
            EventInput.CharEntered += EventInput_CharEntered;
            EventInput.KeyDown += EventInput_KeyDown;
        }

        void EventInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (_subscriber == null)
                return;

            _subscriber.ReceiveSpecialInput(e.KeyCode);
        }

        void EventInput_CharEntered(object sender, CharacterEventArgs e)
        {
            if (_subscriber == null)
                return;
            if (char.IsControl(e.Character))
            {
                //ctrl-v
                if (e.Character == 0x16)
                {
#if WINDOWS
                    //XNA runs in Multiple Thread Apartment state, which cannot recieve clipboard
                    Thread thread = new Thread(PasteThread);
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();
                    _subscriber.ReceiveTextInput(_pasteResult);
#endif
                }
                else
                {
                    _subscriber.ReceiveCommandInput(e.Character);
                }
            }
            else
            {
                _subscriber.ReceiveTextInput(e.Character);
            }
        }

        IKeyboardSubscriber _subscriber;
        public IKeyboardSubscriber Subscriber
        {
            get { return _subscriber; }
            set
            {
                if (_subscriber != null)
                    _subscriber.Selected = false;
                _subscriber = value;
                if (value != null)
                    value.Selected = true;
            }
        }

#if WINDOWS
        //Thread has to be in Single Thread Apartment state in order to receive clipboard
        string _pasteResult = "";
        [STAThread]
        void PasteThread()
        {
            _pasteResult = Clipboard.ContainsText() ? Clipboard.GetText() : "";
        }
#endif

    }
}
