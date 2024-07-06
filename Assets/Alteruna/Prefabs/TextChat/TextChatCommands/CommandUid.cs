using System;
using UnityEngine;

namespace Alteruna.TextChatCommands
{
	public class CommandUid : ITextChatCommand
	{

		public string Command { get; } = "uid";
		public string Description { get; } = "get name, type, and UID of UID object in front of main camera. If \"target\" is passed as a argument, it will get the uid of the last object target. Additionally, a search word can be passed regardless if target was passed.";
		public string Usage { get; } = "/uid target(optional) <string search>(optional)";
		public bool IsCheat { get; } = false;
		public bool IgnoreCase { get; } = true;
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
		public static void Init() => TextChatSynchronizable.Commands.Add(new CommandUid());

		public string Execute(TextChatSynchronizable textChat, string[] args)
		{
			int i = 0;
			bool target = false;
			string search = "";
			bool useSearch = false;
			
			if (args.Length > 0 && args[0].ToLower() == "target")
			{
				if (!TextChatCommandHelper.LastTransformTarget)
				{
					textChat.LogError("No valid target. use /target to get one.");
					return null;
				}
				
				target = true;
				i++;
			}

			if (args.Length > i)
			{
				search = args[i];
				useSearch = true;
			}

			if (target)
			{
				if (useSearch)
				{
					var components = TextChatCommandHelper.LastTransformTarget.gameObject.GetComponents<CommunicationBridgeUID>();
					foreach (CommunicationBridgeUID component in components)
					{
						if (component.GetType().Name.Contains(search))
						{
							TextChatCommandHelper.LastUidTarget = component;
							TextChatCommandHelper.LastTransformTarget = component.transform;
							return component.name + " " + component.GetType() + " " + component.UIDString;
						}
					}
				}
				else
				{
					if (TextChatCommandHelper.LastTransformTarget.gameObject.TryGetComponent(out CommunicationBridgeUID u))
					{
						TextChatCommandHelper.LastUidTarget = u;
						TextChatCommandHelper.LastTransformTarget = u.transform;
						return u.name + " " + u.GetType() + " " + u.UIDString;
					}
				}
			}
			
			if (GetUidObj(textChat, out CommunicationBridgeUID uid))
			{
				TextChatCommandHelper.LastUidTarget = uid;
				TextChatCommandHelper.LastTransformTarget = uid.transform;
				return uid.name + " " + uid.GetType() + " " + uid.UIDString;
			}

			return "No UID object found.";
		}

		public static bool GetUidObj(TextChatSynchronizable textChat, out CommunicationBridgeUID hit, string search = "")
		{
			var camera = Camera.main;
			if (camera == null)
			{
				textChat.LogError("No main camera found.");
				hit = null;
				return false;
			}

			bool useSearch = !string.IsNullOrEmpty(search);

			RaycastHit[] hits = Physics.RaycastAll(camera.transform.position, camera.transform.forward, 100f);
						
			// sort hits by distance
			Array.Sort(hits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));
						
			foreach (RaycastHit h in hits) {
				if (useSearch)
				{
					var components = h.transform.gameObject.GetComponents<CommunicationBridgeUID>();
					foreach (CommunicationBridgeUID component in components)
					{
						if (component.GetType().Name.Contains(search))
						{
							hit = component;
							return true;
						}
					}
				}
				else
				{
					if (h.transform.gameObject.TryGetComponent(out CommunicationBridgeUID uid))
					{
						hit = uid;
						return true;
					}
				}
				
			}
			
			hit = null;
			return false;
		}
	}
}