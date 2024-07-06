using UnityEngine;

namespace Alteruna.TextChatCommands
{
	public class CommandJoinRoom : ITextChatCommand
	{
		public string Command { get; } = "joinRoom";
		public string Description { get; } = "Join room. Any room, by name, or by ID.";
		public string Usage { get; } = "/joinRoom <string|number>(optional) <number password>(optional)";
		public bool IsCheat { get; } = true;
		public bool IgnoreCase { get; } = true;
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
		public static void Init() => TextChatSynchronizable.Commands.Add(new CommandJoinRoom());

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
			
			if (args.Length == 0)
			{
				textChat.Multiplayer.JoinFirstAvailable();
				return null;
			}

			ushort password = 0;
			
			if (args.Length > 1)
			{
				if (!ushort.TryParse(args[args.Length - 1], out password))
				{
					textChat.LogError("Invalid password.");
					return null;
				}
			}

			string roomName = args[0].ToUpper();
			if (ushort.TryParse(roomName, out ushort roomID))
			{

				foreach (Room room in textChat.Multiplayer.AvailableRooms)
				{
					if (room.ID == roomID)
					{
						room.Join(password);
						return null;
					}
				}

				textChat.LogError("No room found with given ID.");
				return null;
			}
			else
			{
				foreach (Room room in textChat.Multiplayer.AvailableRooms)
				{
					if (room.Name.ToUpper().Contains(roomName))
					{
						room.Join(password);
						return null;
					}
				}

				textChat.LogError("No room found containing given string.");
				return null;
			}
		}
	}
}