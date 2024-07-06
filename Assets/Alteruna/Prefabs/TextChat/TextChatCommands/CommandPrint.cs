using UnityEngine;

namespace Alteruna.TextChatCommands
{
	public class CommandPrint : ITextChatCommand
	{
		public string Command { get; } = "print";
		public string Description { get; } = "print message to local chat.";
		public string Usage { get; } = "/print <string>";
		public bool IsCheat { get; } = false;
		public bool IgnoreCase { get; } = true;
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
		public static void Init() => TextChatSynchronizable.Commands.Add(new CommandPrint());

		public string Execute(TextChatSynchronizable textChat, string[] args)
		{
			if (args.Length == 0)
			{
				textChat.LogError("No message provided.");
				return null;
			}

			return string.Join(" ", args);
		}
	}
}