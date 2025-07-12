using System.Runtime.InteropServices;

namespace RemoteDesktopApp.Services
{
    public class InputService : IInputService
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public INPUTUNION union;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        // Mouse event flags
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        // Keyboard event flags
        private const uint KEYEVENTF_KEYUP = 0x0002;

        // Input types
        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;

        // Virtual key codes
        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12; // Alt key
        private const int VK_SHIFT = 0x10;
        private const int VK_LWIN = 0x5B; // Left Windows key

        public void MouseClick(int x, int y, MouseButton button = MouseButton.Left, bool isDoubleClick = false)
        {
            SetCursorPos(x, y);
            
            uint downFlag, upFlag;
            GetMouseFlags(button, out downFlag, out upFlag);
            
            mouse_event(downFlag, (uint)x, (uint)y, 0, UIntPtr.Zero);
            Thread.Sleep(10);
            mouse_event(upFlag, (uint)x, (uint)y, 0, UIntPtr.Zero);
            
            if (isDoubleClick)
            {
                Thread.Sleep(50);
                mouse_event(downFlag, (uint)x, (uint)y, 0, UIntPtr.Zero);
                Thread.Sleep(10);
                mouse_event(upFlag, (uint)x, (uint)y, 0, UIntPtr.Zero);
            }
        }

        public void MouseMove(int x, int y)
        {
            SetCursorPos(x, y);
        }

        public void MouseDrag(int startX, int startY, int endX, int endY, MouseButton button = MouseButton.Left)
        {
            uint downFlag, upFlag;
            GetMouseFlags(button, out downFlag, out upFlag);
            
            SetCursorPos(startX, startY);
            mouse_event(downFlag, (uint)startX, (uint)startY, 0, UIntPtr.Zero);
            
            Thread.Sleep(50);
            SetCursorPos(endX, endY);
            
            Thread.Sleep(50);
            mouse_event(upFlag, (uint)endX, (uint)endY, 0, UIntPtr.Zero);
        }

        public void MouseWheel(int x, int y, int delta)
        {
            SetCursorPos(x, y);
            mouse_event(MOUSEEVENTF_WHEEL, (uint)x, (uint)y, (uint)delta, UIntPtr.Zero);
        }

        public void KeyPress(int key, bool isKeyDown)
        {
            uint flags = isKeyDown ? 0 : KEYEVENTF_KEYUP;
            keybd_event((byte)key, 0, flags, UIntPtr.Zero);
        }

        public void TypeText(string text)
        {
            foreach (char c in text)
            {
                short vk = VkKeyScan(c);
                byte virtualKey = (byte)(vk & 0xFF);
                byte shiftState = (byte)((vk >> 8) & 0xFF);
                
                // Handle shift key if needed
                if ((shiftState & 1) != 0)
                {
                    keybd_event(VK_SHIFT, 0, 0, UIntPtr.Zero);
                }
                
                keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
                Thread.Sleep(10);
                keybd_event(virtualKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                
                if ((shiftState & 1) != 0)
                {
                    keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                }
                
                Thread.Sleep(10);
            }
        }

        public void KeyCombination(ModifierKeys modifierKeys, int key)
        {
            // Press modifier keys
            if (modifierKeys.HasFlag(ModifierKeys.Ctrl))
                keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            if (modifierKeys.HasFlag(ModifierKeys.Alt))
                keybd_event(VK_MENU, 0, 0, UIntPtr.Zero);
            if (modifierKeys.HasFlag(ModifierKeys.Shift))
                keybd_event(VK_SHIFT, 0, 0, UIntPtr.Zero);
            if (modifierKeys.HasFlag(ModifierKeys.Windows))
                keybd_event(VK_LWIN, 0, 0, UIntPtr.Zero);
            
            Thread.Sleep(10);
            
            // Press main key
            keybd_event((byte)key, 0, 0, UIntPtr.Zero);
            Thread.Sleep(10);
            keybd_event((byte)key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            
            Thread.Sleep(10);
            
            // Release modifier keys
            if (modifierKeys.HasFlag(ModifierKeys.Windows))
                keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (modifierKeys.HasFlag(ModifierKeys.Shift))
                keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (modifierKeys.HasFlag(ModifierKeys.Alt))
                keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (modifierKeys.HasFlag(ModifierKeys.Ctrl))
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        private void GetMouseFlags(MouseButton button, out uint downFlag, out uint upFlag)
        {
            switch (button)
            {
                case MouseButton.Left:
                    downFlag = MOUSEEVENTF_LEFTDOWN;
                    upFlag = MOUSEEVENTF_LEFTUP;
                    break;
                case MouseButton.Right:
                    downFlag = MOUSEEVENTF_RIGHTDOWN;
                    upFlag = MOUSEEVENTF_RIGHTUP;
                    break;
                case MouseButton.Middle:
                    downFlag = MOUSEEVENTF_MIDDLEDOWN;
                    upFlag = MOUSEEVENTF_MIDDLEUP;
                    break;
                default:
                    downFlag = MOUSEEVENTF_LEFTDOWN;
                    upFlag = MOUSEEVENTF_LEFTUP;
                    break;
            }
        }
    }
}
