using UnityEngine;

namespace Alteruna.TextChatCommands
{
	public class CommandRoom : ITextChatCommand
	{
		public string Command { get; } = "room";
		public string Description { get; } = "Get information about current room.";
		public string Usage { get; } = "/room";
		public bool IsCheat { get; } = true;
		public bool IgnoreCase { get; } = true;
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
		public static void Init() => TextChatSynchronizable.Commands.Add(new CommandRoom());

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
			
			Room r = textChat.Multiplayer.CurrentRoom;
			return $"Room: {r.Name}@{r.ID}\nPlayers: {r.GetUserCount()}\nMax players:{r.MaxUsers}\nPrivate: {r.InviteOnly}\nOn demand: {r.OnDemand}\nPassword: {r.Pincode}\nLocked: {r.IsLocked}\n";
		}
	}
}