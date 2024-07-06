using UnityEngine;

namespace Alteruna.TextChatCommands
{
	public class CommandLeaveRoom : ITextChatCommand
	{
		public string Command { get; } = "leaveRoom";
		public string Description { get; } = "Leave current room.";
		public string Usage { get; } = "/leaveRoom";
		public bool IsCheat { get; } = true;
		public bool IgnoreCase { get; } = true;
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
		public static void Init() => TextChatSynchronizable.Commands.Add(new CommandLeaveRoom());

		public string Execute(TextChatSynchronizable textChat, string[] args)
		{
			if (textChat.Multiplayer == null)
			{
				textChat.LogError("No valid Multiplayer component.");
				return null;
			}

			if (!textChat.Multiplayer.InRoom)
			{
				textChat.LogError("Not in a room.");
				return null;
			}
			
			textChat.Multiplayer.CurrentRoom.Leave();
			return null;
		}
	}
}