using System;
using UnityEngine;

namespace Alteruna.TextChatCommands
{
	public class CommandMove : ITextChatCommand
	{
		public string Command { get; } = "move";
		public string Description { get; } = "move target (get target using /target) currently looking at (use ~ for relative coordinates)";
		public string Usage { get; } = "/move x y z";
		public bool IsCheat { get; } = true;
		public bool IgnoreCase { get; } = true;
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
		public static void Init() => TextChatSynchronizable.Commands.Add(new CommandMove());

		public string Execute(TextChatSynchronizable textChat, string[] args)
		{
			if (!TextChatCommandHelper.LastTransformTarget)
			{
				textChat.LogError("No valid target. use /target to get one.");
				return null;
			}

			TextChatCommandHelper.LastTransformTarget.position = CommandTp.StringToVector3(TextChatCommandHelper.LastTransformTarget.position, args);

			return null;
		}
	}
}