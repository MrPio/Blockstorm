using System;
using Alteruna.Trinity;
using UnityEngine;

namespace Alteruna.TextChatCommands
{
	public class CommandSync : ITextChatCommand
	{
		public string Command { get; } = "sync";
		public string Description { get; } = "Sync ether a last target from /uid or the object in front of the main camera.";
		public string Usage { get; } = "/sync <uid(optional)>";
		public bool IsCheat { get; } = true;
		public bool IgnoreCase { get; } = true;
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
		public static void Init() => TextChatSynchronizable.Commands.Add(new CommandSync());

		public string Execute(TextChatSynchronizable textChat, string[] args)
		{
			if (args.Length == 0)
			{
				if (TextChatCommandHelper.LastUidTarget)
				{
					textChat.Multiplayer.Sync(TextChatCommandHelper.LastUidTarget, Reliability.Reliable);
				}

				return "No valid target. Use /uid to set target.";
			}

			if (!Guid.TryParse(args[0], out Guid guid))
			{
				textChat.LogError("Invalid UID");
				return null;
			}
			
			textChat.Multiplayer.Sync(guid, Reliability.Reliable);
			return null;
		}
	}
}