using UnityEngine;

namespace Alteruna.TextChatCommands
{
	public class CommandMe : ITextChatCommand
	{
		public string Command { get; } = "me";
		public string Description { get; } = "Get name and id of yourself. If you have a avatar, it will be set as target.";
		public string Usage { get; } = "/me";
		public bool IsCheat { get; } = false;
		public bool IgnoreCase { get; } = true;
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
		public static void Init() => TextChatSynchronizable.Commands.Add(new CommandMe());

		public string Execute(TextChatSynchronizable textChat, string[] args)
		{

			if (textChat.Multiplayer == null)
			{
				textChat.LogError("No valid Multiplayer component.");
				return null;
			}

			if (textChat.Multiplayer.Me == null)
			{
				textChat.LogError("No user information available.");
				return null;
			}
			
			Avatar avatar = textChat.Multiplayer.GetAvatar();
			if (avatar != null)
			{
				TextChatCommandHelper.LastTransformTarget = avatar.transform;
				Vector3 pos = avatar.transform.position;
				return $"{textChat.Multiplayer.Me}\n x:{pos.x} y:{pos.y} z:{pos.z}";
			}
			else
			{
				return textChat.Multiplayer.Me;
			}
		}
	}
}