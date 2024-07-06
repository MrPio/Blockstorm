using System;
using UnityEngine;

namespace Alteruna.TextChatCommands
{
	public class CommandTp : ITextChatCommand
	{
		public string Command { get; } = "tp";
		public string Description { get; } = "teleport avatar (use ~ for relative coordinates)";
		public string Usage { get; } = "/tp <avatar name or index (optional)> x y z";
		public bool IsCheat { get; } = true;
		public bool IgnoreCase { get; } = true;
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
		public static void Init() => TextChatSynchronizable.Commands.Add(new CommandTp());

		public string Execute(TextChatSynchronizable textChat, string[] args)
		{
			if (args.Length < 3)
			{
				textChat.LogError("Unexpected number of arguments.");
				return null;
			}

			if (args.Length == 3)
			{
				Transform t = textChat.Multiplayer.GetAvatar().transform;
				t.position = StringToVector3(t.position, args);
			}
			else
			{
				Transform t;
				
				if (ushort.TryParse(args[0], out ushort id))
				{
					t = textChat.Multiplayer.GetAvatar(id).transform;
				}
				else
				{
					t = textChat.Multiplayer.GetAvatar(args[0]).transform;
				}

				t.position = StringToVector3(t.position, args);
			}
						
			return null;
		}

		public static Vector3 StringToVector3(Vector3 origin, string[] args, int offset = 0)
		{
			if (args.Length - offset < 3) throw new ArgumentException("Unexpected number of arguments.");
			return new Vector3(
				StringToAxis(origin.x, args[offset + 0]), 
				StringToAxis(origin.y, args[offset + 1]), 
				StringToAxis(origin.z, args[offset + 2]));
		}
		
		public static float StringToAxis(float origin, string arg)
		{
			if (arg[0] == '~')
			{
				return origin + float.Parse(arg.Substring(1));
			}
			return float.Parse(arg);
		}
	}
}