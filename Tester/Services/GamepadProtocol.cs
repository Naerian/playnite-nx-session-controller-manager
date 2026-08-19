using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ControllerSessionManager.Tester.Models;

namespace ControllerSessionManager.Tester.Services
{
    public static class GamepadProtocol
    {
        public const string Magic = "CSMT1";
        public const string SnapshotCommand = "SNAP";
        public const string SelectCommand = "SELECT";
        public const string RumbleCommand = "RUMBLE";
        public const string PingCommand = "PING";

        public sealed class SnapshotMessage
        {
            public SnapshotMessage()
            {
                State = new GamepadState();
                Controllers = new List<GamepadControllerInfo>();
            }

            public GamepadState State { get; set; }
            public IList<GamepadControllerInfo> Controllers { get; set; }
        }

        public static string EncodeCommand(string token, string command, params string[] fields)
        {
            var parts = new List<string>();
            parts.Add(Magic);
            parts.Add(token ?? string.Empty);
            parts.Add(command ?? string.Empty);
            if (fields != null)
            {
                for (var i = 0; i < fields.Length; i++)
                {
                    parts.Add(EncodeField(fields[i]));
                }
            }

            return string.Join("|", parts.ToArray());
        }

        public static bool TryParseCommand(string line, string expectedToken, out string command, out string[] fields)
        {
            command = null;
            fields = new string[0];
            var parts = SplitFields(line);
            if (parts.Length < 3 || parts[0] != Magic || parts[1] != expectedToken)
            {
                return false;
            }

            command = parts[2];
            if (parts.Length == 3)
            {
                return true;
            }

            fields = new string[parts.Length - 3];
            Array.Copy(parts, 3, fields, 0, fields.Length);
            return true;
        }

        public static string EncodeSnapshot(string token, GamepadState state, IReadOnlyList<GamepadControllerInfo> controllers)
        {
            if (state == null)
            {
                state = new GamepadState();
            }

            if (controllers == null)
            {
                controllers = new List<GamepadControllerInfo>();
            }

            var fields = new List<string>();
            fields.Add(state.IsConnected ? "1" : "0");
            fields.Add(state.ControllerName);
            fields.Add(state.VendorId.ToString(CultureInfo.InvariantCulture));
            fields.Add(state.ProductId.ToString(CultureInfo.InvariantCulture));
            fields.Add(((int)state.Layout).ToString(CultureInfo.InvariantCulture));
            fields.Add(((int)state.EightBitDoModel).ToString(CultureInfo.InvariantCulture));
            fields.Add(state.SdlVersion);
            fields.Add(state.SdlGuid);
            fields.Add(state.SdlMapping);
            fields.Add(state.AxisCount.ToString(CultureInfo.InvariantCulture));
            fields.Add(state.ButtonCount.ToString(CultureInfo.InvariantCulture));
            fields.Add(state.HatCount.ToString(CultureInfo.InvariantCulture));
            fields.Add(FormatFloat(state.LeftStick == null ? 0f : state.LeftStick.X));
            fields.Add(FormatFloat(state.LeftStick == null ? 0f : state.LeftStick.Y));
            fields.Add(FormatFloat(state.RightStick == null ? 0f : state.RightStick.X));
            fields.Add(FormatFloat(state.RightStick == null ? 0f : state.RightStick.Y));
            fields.Add(FormatFloat(state.LeftTrigger));
            fields.Add(FormatFloat(state.RightTrigger));
            fields.Add(EncodeButtons(state.Buttons).ToString(CultureInfo.InvariantCulture));

            var extras = state.ExtraButtons ?? new List<ExtraButtonState>();
            fields.Add(extras.Count.ToString(CultureInfo.InvariantCulture));
            for (var i = 0; i < extras.Count; i++)
            {
                var extra = extras[i] ?? new ExtraButtonState();
                fields.Add(extra.RawIndex.ToString(CultureInfo.InvariantCulture));
                fields.Add(extra.Label);
                fields.Add(extra.IsPressed ? "1" : "0");
            }

            fields.Add(controllers.Count.ToString(CultureInfo.InvariantCulture));
            for (var i = 0; i < controllers.Count; i++)
            {
                var controller = controllers[i] ?? new GamepadControllerInfo();
                fields.Add(controller.InstanceId.ToString(CultureInfo.InvariantCulture));
                fields.Add(controller.JoystickIndex.ToString(CultureInfo.InvariantCulture));
                fields.Add(controller.Name);
                fields.Add(controller.VendorId.ToString(CultureInfo.InvariantCulture));
                fields.Add(controller.ProductId.ToString(CultureInfo.InvariantCulture));
                fields.Add(((int)controller.Layout).ToString(CultureInfo.InvariantCulture));
                fields.Add(((int)controller.EightBitDoModel).ToString(CultureInfo.InvariantCulture));
            }

            return EncodeCommand(token, SnapshotCommand, fields.ToArray());
        }

        public static bool TryParseSnapshot(string line, string expectedToken, out SnapshotMessage message)
        {
            message = null;
            string command;
            string[] fields;
            if (!TryParseCommand(line, expectedToken, out command, out fields) || command != SnapshotCommand)
            {
                return false;
            }

            if (fields.Length < 20)
            {
                return false;
            }

            var index = 0;
            var snapshot = new SnapshotMessage();
            var state = snapshot.State;
            state.IsConnected = fields[index++] == "1";
            state.ControllerName = fields[index++];
            state.VendorId = ParseUShort(fields[index++]);
            state.ProductId = ParseUShort(fields[index++]);
            state.Layout = (GamepadLayout)ParseInt(fields[index++]);
            state.EightBitDoModel = (EightBitDoModel)ParseInt(fields[index++]);
            state.SdlVersion = fields[index++];
            state.SdlGuid = fields[index++];
            state.SdlMapping = fields[index++];
            state.AxisCount = ParseInt(fields[index++]);
            state.ButtonCount = ParseInt(fields[index++]);
            state.HatCount = ParseInt(fields[index++]);
            state.LeftStick = new StickState
            {
                X = ParseFloat(fields[index++]),
                Y = ParseFloat(fields[index++])
            };
            state.RightStick = new StickState
            {
                X = ParseFloat(fields[index++]),
                Y = ParseFloat(fields[index++])
            };
            state.LeftTrigger = ParseFloat(fields[index++]);
            state.RightTrigger = ParseFloat(fields[index++]);
            state.Buttons = DecodeButtons(ParseUInt(fields[index++]));

            var extraCount = ParseInt(fields[index++]);
            state.ExtraButtons = new List<ExtraButtonState>();
            for (var i = 0; i < extraCount; i++)
            {
                if (index + 2 >= fields.Length)
                {
                    return false;
                }

                state.ExtraButtons.Add(new ExtraButtonState
                {
                    RawIndex = ParseInt(fields[index++]),
                    Label = fields[index++],
                    IsPressed = fields[index++] == "1"
                });
            }

            if (index >= fields.Length)
            {
                return false;
            }

            var controllerCount = ParseInt(fields[index++]);
            for (var i = 0; i < controllerCount; i++)
            {
                if (index + 6 >= fields.Length)
                {
                    return false;
                }

                snapshot.Controllers.Add(new GamepadControllerInfo
                {
                    InstanceId = ParseInt(fields[index++]),
                    JoystickIndex = ParseInt(fields[index++]),
                    Name = fields[index++],
                    VendorId = ParseUShort(fields[index++]),
                    ProductId = ParseUShort(fields[index++]),
                    Layout = (GamepadLayout)ParseInt(fields[index++]),
                    EightBitDoModel = (EightBitDoModel)ParseInt(fields[index++])
                });
            }

            message = snapshot;
            return true;
        }

        public static uint EncodeButtons(GamepadButtonState buttons)
        {
            if (buttons == null)
            {
                return 0;
            }

            uint bits = 0;
            if (buttons.South) bits |= 1u << 0;
            if (buttons.East) bits |= 1u << 1;
            if (buttons.West) bits |= 1u << 2;
            if (buttons.North) bits |= 1u << 3;
            if (buttons.LeftShoulder) bits |= 1u << 4;
            if (buttons.RightShoulder) bits |= 1u << 5;
            if (buttons.Back) bits |= 1u << 6;
            if (buttons.Start) bits |= 1u << 7;
            if (buttons.Guide) bits |= 1u << 8;
            if (buttons.Touchpad) bits |= 1u << 9;
            if (buttons.LeftStick) bits |= 1u << 10;
            if (buttons.RightStick) bits |= 1u << 11;
            if (buttons.DpadUp) bits |= 1u << 12;
            if (buttons.DpadDown) bits |= 1u << 13;
            if (buttons.DpadLeft) bits |= 1u << 14;
            if (buttons.DpadRight) bits |= 1u << 15;
            return bits;
        }

        public static GamepadButtonState DecodeButtons(uint bits)
        {
            return new GamepadButtonState
            {
                South = (bits & (1u << 0)) != 0,
                East = (bits & (1u << 1)) != 0,
                West = (bits & (1u << 2)) != 0,
                North = (bits & (1u << 3)) != 0,
                LeftShoulder = (bits & (1u << 4)) != 0,
                RightShoulder = (bits & (1u << 5)) != 0,
                Back = (bits & (1u << 6)) != 0,
                Start = (bits & (1u << 7)) != 0,
                Guide = (bits & (1u << 8)) != 0,
                Touchpad = (bits & (1u << 9)) != 0,
                LeftStick = (bits & (1u << 10)) != 0,
                RightStick = (bits & (1u << 11)) != 0,
                DpadUp = (bits & (1u << 12)) != 0,
                DpadDown = (bits & (1u << 13)) != 0,
                DpadLeft = (bits & (1u << 14)) != 0,
                DpadRight = (bits & (1u << 15)) != 0
            };
        }

        private static string EncodeField(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (ch == '\\')
                {
                    builder.Append("\\\\");
                }
                else if (ch == '|')
                {
                    builder.Append("\\p");
                }
                else if (ch == '\n')
                {
                    builder.Append("\\n");
                }
                else if (ch == '\r')
                {
                    builder.Append("\\r");
                }
                else
                {
                    builder.Append(ch);
                }
            }

            return builder.ToString();
        }

        private static string[] SplitFields(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return new string[0];
            }

            var fields = new List<string>();
            var current = new StringBuilder();
            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '\\' && i + 1 < line.Length)
                {
                    var next = line[i + 1];
                    if (next == '\\')
                    {
                        current.Append('\\');
                        i++;
                    }
                    else if (next == 'p')
                    {
                        current.Append('|');
                        i++;
                    }
                    else if (next == 'n')
                    {
                        current.Append('\n');
                        i++;
                    }
                    else if (next == 'r')
                    {
                        current.Append('\r');
                        i++;
                    }
                    else
                    {
                        current.Append(ch);
                    }
                }
                else if (ch == '|')
                {
                    fields.Add(current.ToString());
                    current.Length = 0;
                }
                else
                {
                    current.Append(ch);
                }
            }

            fields.Add(current.ToString());
            return fields.ToArray();
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("G9", CultureInfo.InvariantCulture);
        }

        private static float ParseFloat(string value)
        {
            float parsed;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ? parsed : 0f;
        }

        private static int ParseInt(string value)
        {
            int parsed;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) ? parsed : 0;
        }

        private static uint ParseUInt(string value)
        {
            uint parsed;
            return uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) ? parsed : 0u;
        }

        private static ushort ParseUShort(string value)
        {
            ushort parsed;
            return ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) ? parsed : (ushort)0;
        }
    }
}
