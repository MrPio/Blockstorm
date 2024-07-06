using System;
using UnityEngine;

namespace Alteruna.TextChatCommands
{
	public class CommandTarget : ITextChatCommand
	{
		public string Command { get; } = "target";
		public string Description { get; } = "set target and get name of object in front of main camera";
		public string Usage { get; } = "/target";
		public bool IsCheat { get; } = false;
		public bool IgnoreCase { get; } = true;
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
		public static void Init() => TextChatSynchronizable.Commands.Add(new CommandTarget());

		public string Execute(TextChatSynchronizable textChat, string[] args)
		{
			if (GetTarget(textChat, out RaycastHit hit))
			{
				TextChatCommandHelper.LastTransformTarget = hit.transform;
				return hit.transform.name;
			}

			return "No object found.";
		}

		public static bool GetTarget(TextChatSynchronizable textChat, out RaycastHit hit)
		{
			var camera = Camera.main;
			if (camera == null)
			{
				textChat.LogError("No main camera found.");
				hit = new RaycastHit();
				return false;
			}

			if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit h, 100f))
			{
				hit = h;
				return true;
			}
			
			hit = new RaycastHit();
			return false;
		}
	}
}