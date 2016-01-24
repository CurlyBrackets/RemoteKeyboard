using NetMQ;
using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices;

namespace KeyboardClient
{
    class Program
    {
        #region Remote

        enum MessageType
        {
            KeyUp,
            KeyDown,
            MouseDown,
            MouseUp,
            MouseMove
        }

        struct Message
        {
            public MessageType Type { get; set; }
            public int Data { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
        }

        #endregion

        #region Marshalled Structres

        enum InputType : uint
        {
            Mouse = 0,
            Keyboard = 1,
            Hardware = 2
        }

        [Flags]
        enum KbFlags : uint
        {
            None = 0,
            Extended = 1,
            KeyUp = 2,
            ScanCode = 8,
            Unicode = 4
        }

        [Flags]
        enum MouseFlags : uint
        {
            None = 0,
            Absolute = 0x8000,
            HWheel = 0x1000,
            Move = 0x0001,
            MoveNoCoalesce = 0x2000,
            LeftDown = 0x0002,
            LeftUp = 0x0004,
            RightDown = 0x0008,
            RightUp = 0x0010,
            MiddleDown = 0x0020,
            MiddleUp = 0x0040,
            VirtualDesk = 0x4000,
            Wheel = 0x0800,
            XDown = 0x0080,
            XUp = 0x0100,
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KBINPUT
        {
            public ushort VirtualKey;
            public ushort Scan;
            public KbFlags Flags;
            public uint Time;
            public UIntPtr extrainfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int Dx;
            public int Dy;
            public uint Data;
            public MouseFlags Flags;
            public uint Time;
            public UIntPtr extrainfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct INPUT
        {
            [FieldOffset(0)]
            public InputType Type;
            [FieldOffset(4)]
            public KBINPUT Ki;
            [FieldOffset(4)]
            public MOUSEINPUT Mi;
        }

        #endregion

        static void Main(string[] args)
        {
            using (var context = NetMQContext.Create())
            using (var server = context.CreateResponseSocket())
            {
                server.Bind("tcp://0.0.0.0:7331");
               
                while (true)
                {
                    var message = JsonConvert.DeserializeObject<Message>(server.ReceiveFrameString());

                    switch (message.Type)
                    {
                        case MessageType.KeyDown:
                            SendKey(true, message.Data);
                            break;
                        case MessageType.KeyUp:
                            SendKey(false, message.Data);
                            break;
                        default:
                            Console.WriteLine("Not implemented: " + message.Type);
                            break;
                    }
                }
            }
        }

        static void SendKey(bool down, int keycode)
        {
            var toSend = new INPUT[1];
            toSend[0].Type = InputType.Keyboard;
            toSend[0].Ki.Flags = down ? KbFlags.KeyUp : KbFlags.None;
            toSend[0].Ki.VirtualKey = (ushort)keycode;

            SendInput(1, toSend, Marshal.SizeOf(toSend[0]));
        }

        [DllImport("user32", CallingConvention = CallingConvention.Winapi)]
        private static extern uint SendInput(
            [In] uint nInputs,
            [In] INPUT[] pInputs,
            [In] int cbSize
            );
    }
}
