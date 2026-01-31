using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Managers
{
    /// <summary>
    /// <para>Initializes and provides access to primary input devices through Silk.NET input context</para>
    /// <para>Automatically selects the first available keyboard and mouse as primary devices</para>
    /// <para>Needs to be initialized once with the window creation</para>
    /// <para>Currently limitated to US-Layout but localazation can be done via ScanCodes</para>
    /// <para>See Tracking issue for Regional Keyboard Support <see cref="https://github.com/dotnet/Silk.NET/issues/737"/></para>
    /// <para>aswell</para>
    /// </summary>

    public static class InputDeviceHandler
    {
        public static IInputContext Input = null!;
        private static List<IKeyboard> Keyboards = [];

        public static IKeyboard? primaryKeyboard = null;
        public static IMouse? primaryMouse = null;

        public static void Init(IWindow window)
        {
            Input = window.CreateInput();
            Keyboards.AddRange(Input.Keyboards);

            if (Keyboards.Count > 0)
            {
                primaryKeyboard = Keyboards[0];
            }

            if (Input.Mice.Count > 0)
            {
                primaryMouse = Input.Mice[0];
            }
        }

        public static bool IsKeyPressed(Key key)
        {
            if (Keyboards.Count > 0)
            {
                return Keyboards[0].IsKeyPressed(key);
            }

            return false;
        }
    }
}