using System;
using UnityEngine;

namespace Alteruna.TextChatCommands
{
	public class CommandRotate : ITextChatCommand
	{
		public string Command { get; } = "rotate";
		public string Description { get; } = "rotate target (get target using /target) currently looking at (use ~ for relative coordinates)";
		public string Usage { get; } = "/rotate x y z";
		public bool IsCheat { get; } = true;
		public bool IgnoreCase { get; } = true;
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
		public static void Init() => TextChatSynchronizable.Commands.Add(new CommandRotate());

		public string Execute(TextChatSynchronizable textChat, string[] args)
		{
			if (!TextChatCommandHelper.LastTransformTarget)
			{
				textChat.LogError("No valid target. use /target to get one.");
				return null;
			}

			TextChatCommandHelper.LastTransformTarget.eulerAngles = CommandTp.StringToVector3(TextChatCommandHelper.LastTransformTarget.eulerAngles, args);

			return null;
		}
	}
}