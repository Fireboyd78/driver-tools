using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Windows.Forms
{
    [Flags]
    public enum MessageBoxExFlags
    {
        ErrorBoxOK                  = MessageBoxIcon.Error | MessageBoxButtons.OK,
        ErrorBoxOKCancel            = MessageBoxIcon.Error | MessageBoxButtons.OKCancel,
        ErrorBoxAbortRetryIgnore    = MessageBoxIcon.Error | MessageBoxButtons.AbortRetryIgnore,
        ErrorBoxYesNoCancel         = MessageBoxIcon.Error | MessageBoxButtons.YesNoCancel,
        ErrorBoxYesNo               = MessageBoxIcon.Error | MessageBoxButtons.YesNo,
        ErrorBoxRetryCancel         = MessageBoxIcon.Error | MessageBoxButtons.AbortRetryIgnore,

        InfoBoxOK                   = MessageBoxIcon.Information | MessageBoxButtons.OK,
        InfoBoxOKCancel             = MessageBoxIcon.Information | MessageBoxButtons.OKCancel,
        InfoBoxAbortRetryIgnore     = MessageBoxIcon.Information | MessageBoxButtons.AbortRetryIgnore,
        InfoBoxYesNoCancel          = MessageBoxIcon.Information | MessageBoxButtons.YesNoCancel,
        InfoBoxYesNo                = MessageBoxIcon.Information | MessageBoxButtons.YesNo,
        InfoBoxRetryCancel          = MessageBoxIcon.Information | MessageBoxButtons.AbortRetryIgnore,

        WarningBoxOK                = MessageBoxIcon.Warning | MessageBoxButtons.OK,
        WarningBoxOKCancel          = MessageBoxIcon.Warning | MessageBoxButtons.OKCancel,
        WarningBoxAbortRetryIgnore  = MessageBoxIcon.Warning | MessageBoxButtons.AbortRetryIgnore,
        WarningBoxYesNoCancel       = MessageBoxIcon.Warning | MessageBoxButtons.YesNoCancel,
        WarningBoxYesNo             = MessageBoxIcon.Warning | MessageBoxButtons.YesNo,
        WarningBoxRetryCancel       = MessageBoxIcon.Warning | MessageBoxButtons.AbortRetryIgnore,

        RtlReading                  = MessageBoxOptions.RtlReading,
        RightAlign                  = MessageBoxOptions.RightAlign,
        DefaultDesktopOnly          = MessageBoxOptions.DefaultDesktopOnly,
        ServiceNotification         = MessageBoxOptions.ServiceNotification,

        DefaultButton1              = MessageBoxDefaultButton.Button1,
        DefaultButton2              = MessageBoxDefaultButton.Button2,
        DefaultButton3              = MessageBoxDefaultButton.Button3
    }

    public static class MessageBoxEx
    {
        private static MessageBoxButtons? GetButtons(int flags)
        {
            int flag = flags & 0x7;

            if (Enum.IsDefined(typeof(MessageBoxButtons), flag))
                return (MessageBoxButtons)flag;
            
            return null;
        }

        private static MessageBoxDefaultButton? GetDefaultButton(int flags)
        {
            int flag = flags & 0x300;

            if (Enum.IsDefined(typeof(MessageBoxDefaultButton), flag))
                return (MessageBoxDefaultButton)flag;
            
            return null;
        }

        private static MessageBoxIcon? GetIcon(int flags)
        {
            int flag = flags & 0x70;

            if (Enum.IsDefined(typeof(MessageBoxIcon), flag))
                return (MessageBoxIcon)flag;

            return null;
        }

        private static MessageBoxOptions? GetOptions(int flags)
        {
            int flag = flags & 0x3A0000;

            if (Enum.IsDefined(typeof(MessageBoxOptions), flag))
                return (MessageBoxOptions)flag;

            return null;
        }

        private static DialogResult ShowCore(string text,
            string caption,
            MessageBoxExFlags flags,
            [Optional] bool displayHelpButton,
            [Optional] string helpFilePath,
            [Optional] string keyword,
            [Optional] HelpNavigator navigator,
            [Optional] object param)
        {
            int _flags = (int)flags;

            MessageBoxButtons? _buttons              = GetButtons(_flags);
            MessageBoxDefaultButton? _defaultButton  = GetDefaultButton(_flags);
            MessageBoxIcon? _icon                    = GetIcon(_flags);
            MessageBoxOptions? _options              = GetOptions(_flags);

            if (caption == null)
                caption = Application.ProductName;

            MessageBoxButtons buttons               = _buttons ?? MessageBoxButtons.OK;
            MessageBoxDefaultButton defaultButton   = _defaultButton ?? MessageBoxDefaultButton.Button1;
            MessageBoxIcon icon                     = _icon ?? MessageBoxIcon.None;

            
            if (_options != null)
            {
                MessageBoxOptions options = (MessageBoxOptions)_options;

                if (helpFilePath != null)
                {
                    if (keyword != null)
                    {
                        return MessageBox.Show(text, caption, buttons, icon, defaultButton, options, helpFilePath, keyword);
                    }
                    else if (navigator != 0)
                    {
                        HelpNavigator _navigator = navigator;

                        if (param != null)
                        {
                            return MessageBox.Show(text, caption, buttons, icon, defaultButton, options, helpFilePath, navigator, param);
                        }
                        else
                        {
                            return MessageBox.Show(text, caption, buttons, icon, defaultButton, options, helpFilePath, navigator);
                        }
                    }
                    else
                    {
                        return MessageBox.Show(text, caption, buttons, icon, defaultButton, options, helpFilePath);
                    }
                }
                else if (displayHelpButton)
                {
                    return MessageBox.Show(text, caption, buttons, icon, defaultButton, options, displayHelpButton);
                }
                else
                {
                    return MessageBox.Show(text, caption, buttons, icon, defaultButton, options);
                }
            }
            else if (displayHelpButton)
            {
                return MessageBox.Show(text, caption, buttons, icon, defaultButton, 0, displayHelpButton);
            }
            else
            {
                return MessageBox.Show(text, caption, buttons, icon, defaultButton);
            }
        }

        public static DialogResult Show(string text)
        {
            return Show(text, 0, false);
        }

        public static DialogResult Show(string text, MessageBoxExFlags flags)
        {
            return Show(text, flags, false);
        }

        public static DialogResult Show(string text, MessageBoxExFlags flags, bool displayHelpButton)
        {
            return ShowCore(text, null, flags, displayHelpButton: displayHelpButton);
        }

        public static DialogResult Show(string text, MessageBoxExFlags flags, string helpFilePath)
        {
            return ShowCore(text, null, flags, helpFilePath: helpFilePath);
        }

        public static DialogResult Show(string text, MessageBoxExFlags flags, string helpFilePath, string keyword)
        {
            return ShowCore(text, null, flags, helpFilePath: helpFilePath, keyword: keyword);
        }

        public static DialogResult Show(string text, MessageBoxExFlags flags, string helpFilePath, HelpNavigator navigator)
        {
            return ShowCore(text, null, flags, helpFilePath: helpFilePath, navigator: navigator);
        }

        public static DialogResult Show(string text, MessageBoxExFlags flags, string helpFilePath, HelpNavigator navigator, object param)
        {
            return ShowCore(text, null, flags, helpFilePath: helpFilePath, navigator: navigator, param: param);
        }

        public static DialogResult Show(string text, string caption)
        {
            return Show(text, caption, 0, false);
        }

        public static DialogResult Show(string text, string caption, MessageBoxExFlags flags)
        {
            return Show(text, caption, flags, false);
        }

        public static DialogResult Show(string text, string caption, MessageBoxExFlags flags, bool displayHelpButton)
        {
            return ShowCore(text, caption, flags, displayHelpButton);
        }

        public static DialogResult Show(string text, string caption, MessageBoxExFlags flags, string helpFilePath)
        {
            return ShowCore(text, caption, flags, helpFilePath: helpFilePath);
        }

        public static DialogResult Show(string text, string caption, MessageBoxExFlags flags, string helpFilePath, string keyword)
        {
            return ShowCore(text, caption, flags, helpFilePath: helpFilePath, keyword: keyword);
        }

        public static DialogResult Show(string text, string caption, MessageBoxExFlags flags, string helpFilePath, HelpNavigator navigator)
        {
            return ShowCore(text, caption, flags, helpFilePath: helpFilePath, navigator: navigator);
        }

        public static DialogResult Show(string text, string caption, MessageBoxExFlags flags, string helpFilePath, HelpNavigator navigator, object param)
        {
            return ShowCore(text, caption, flags, helpFilePath: helpFilePath, navigator: navigator, param: param);
        }
    }
}
