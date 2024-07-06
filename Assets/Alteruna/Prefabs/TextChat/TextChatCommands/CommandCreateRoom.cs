using UnityEngine;

namespace Alteruna.TextChatCommands
{
	public class CommandCreateRoom : ITextChatCommand
	{
		public string Command { get; } = "createRoom";
		public string Description { get; } = "Create room. All arguments are optional.";
		public string Usage { get; } = "/createRoom name:<string> private:<bool> password:<number> maxPlayers:<number>";
		public bool IsCheat { get; } = true;
		public bool IgnoreCase { get; } = true;
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
		public static void Init() => TextChatSynchronizable.Commands.Add(new CommandCreateRoom());

		public string Execute(TextChatSynchronizable textChat, string[] args)
		{

			if (textChat.Multiplayer == null)
			{
				textChat.LogError("No valid Multiplayer component.");
				return null;
			}

			if (textChat.Multiplayer.InRoom)
			{
				textChat.LogError("You are already in a room.");
				return null;
			}
			
			string name = null;
			bool isPrivate = false;
			ushort password = 0;
			bool onDemand = true;
			bool joinRoom = true;
			ushort maxUsers = 0;

			foreach (string arg in args)
			{
				string[] split = arg.Split(':');
				if (split.Length != 2)
				{
					textChat.LogError("Invalid argument: " + arg);
					return null;
				}

				switch (split[0].ToUpper())
				{
					case "NAME":
						name = split[1];
						break;
					case "PRIVATE":
						if (!TextChatCommandHelper.TrySetBoolArg(split[1], ref isPrivate))
						{
							textChat.LogError("Invalid argument: " + arg);
							return null;
						}
						break;
					case "PASSWORD":
						if (!ushort.TryParse(split[1], out password))
						{
							textChat.LogError("Invalid argument: " + arg);
							return null;
						}
						break;
					// ReSharper disable once StringLiteralTypo
					case "MAXPLAYERS":
					// ReSharper disable once StringLiteralTypo
					case "MAXUSERS":
						if (!ushort.TryParse(split[1], out maxUsers))
						{
							textChat.LogError("Invalid argument: " + arg);
							return null;
						}
						break;
					// ReSharper disable once StringLiteralTypo
					case "ONDEMAND":
						if (!TextChatCommandHelper.TrySetBoolArg(split[1], ref onDemand))
						{
							textChat.LogError("Invalid argument: " + arg);
							return null;
						}
						break;
					// ReSharper disable once StringLiteralTypo
					case "JOINROOM":
						if (!TextChatCommandHelper.TrySetBoolArg(split[1], ref joinRoom))
						{
							textChat.LogError("Invalid argument: " + arg);
							return null;
						}
						break;
					default:
						textChat.LogError("Invalid argument: " + arg);
						return null;
				}
			}

			textChat.Multiplayer.CreateRoom(name, isPrivate, password, onDemand, joinRoom, maxUsers);
			return null;
		}
	}
}