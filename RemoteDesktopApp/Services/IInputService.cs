namespace RemoteDesktopApp.Services
{
    public interface IInputService
    {
        /// <summary>
        /// Simulates a mouse click at the specified coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="button">Mouse button (left, right, middle)</param>
        /// <param name="isDoubleClick">Whether this is a double click</param>
        void MouseClick(int x, int y, MouseButton button = MouseButton.Left, bool isDoubleClick = false);
        
        /// <summary>
        /// Simulates mouse movement to the specified coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        void MouseMove(int x, int y);
        
        /// <summary>
        /// Simulates mouse drag operation
        /// </summary>
        /// <param name="startX">Start X coordinate</param>
        /// <param name="startY">Start Y coordinate</param>
        /// <param name="endX">End X coordinate</param>
        /// <param name="endY">End Y coordinate</param>
        /// <param name="button">Mouse button to drag with</param>
        void MouseDrag(int startX, int startY, int endX, int endY, MouseButton button = MouseButton.Left);
        
        /// <summary>
        /// Simulates mouse wheel scroll
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="delta">Scroll delta (positive for up, negative for down)</param>
        void MouseWheel(int x, int y, int delta);
        
        /// <summary>
        /// Simulates a key press
        /// </summary>
        /// <param name="key">Virtual key code</param>
        /// <param name="isKeyDown">True for key down, false for key up</param>
        void KeyPress(int key, bool isKeyDown);
        
        /// <summary>
        /// Simulates typing text
        /// </summary>
        /// <param name="text">Text to type</param>
        void TypeText(string text);
        
        /// <summary>
        /// Simulates key combination (e.g., Ctrl+C)
        /// </summary>
        /// <param name="modifierKeys">Modifier keys (Ctrl, Alt, Shift)</param>
        /// <param name="key">Main key</param>
        void KeyCombination(ModifierKeys modifierKeys, int key);
    }
    
    public enum MouseButton
    {
        Left,
        Right,
        Middle
    }
    
    [Flags]
    public enum ModifierKeys
    {
        None = 0,
        Ctrl = 1,
        Alt = 2,
        Shift = 4,
        Windows = 8
    }
}
