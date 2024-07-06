using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alteruna.TextChatCommands
{
	public class CommandLoadScene : ITextChatCommand
	{
		public string Command { get; } = "loadScene";
		public string Description { get; } = "Loads scene by name or index. (no multiplayer behavior)";
		
		// ReSharper disable once StringLiteralTypo
		public string Usage { get; } = "/loadScene <int/string>";
		public bool IsCheat { get; } = true;
		public bool IgnoreCase { get; } = true;
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
		public static void Init() => TextChatSynchronizable.Commands.Add(new CommandLoadScene());

		public string Execute(TextChatSynchronizable textChat, string[] args)
		{
			if (args.Length < 1)
			{
				textChat.LogError("No argument. Usage: " + Usage);
			}
			else if (int.TryParse(args[0], out int id))
			{
				SceneManager.LoadScene(id);
			}
			else
			{
				SceneManager.LoadScene(args[0]);
			}

			return null;
		}
	}
}