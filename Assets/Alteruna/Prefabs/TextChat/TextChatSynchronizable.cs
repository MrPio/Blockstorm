using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alteruna.TextChatCommands;
using Alteruna.Trinity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Application = UnityEngine.Application;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Alteruna
{
	public class TextChatSynchronizable : Synchronizable
	{
		
		public static readonly List<ITextChatCommand> Commands = new List<ITextChatCommand>();
		
		[Tooltip("Amount of chat messages to keep in buffer")] [SerializeField]
		private int chatBuffer = 10;
		
		public bool LogSystemMessages = true;
		[Tooltip("Sort messages based on senders timestamp")]
		public bool UseTimeStamps = false;
		[Tooltip("when true, show message for sender when sending the message. Otherwise it shows when the message in created.")]
		public bool LogLocalOnSend = true;
		public bool LogErrors = false;
		public bool AllowCheats = false;
		[Tooltip("Always true in development build and editor")]
		public bool AllowHostToToggleCheats = true;
		public bool AllowCommands = true;

		public UnityEvent<string> TextChatUpdate;

		[Space] public bool UseRichText = true;
		
		[HideInInspector] public bool BoldNames = true;

		private ChatEvent[] _chat = Array.Empty<ChatEvent>();
		private readonly List<ChatEvent> _outgoingMessages = new List<ChatEvent>();
		
		// We define the StringBuilder here to avoid creating a new one every time we need it.
		[NonSerialized] private readonly StringBuilder _sb = new StringBuilder();
		[NonSerialized] private readonly StringBuilder _sb2 = new StringBuilder();

		private static readonly Color c = new Color(1f, .2f, 0, 1);

		/// <summary>
		/// Function to get user color.
		/// </summary>
		/// <example>
		///	<code>
		/// TextChatSynchronizable.GetUserColor = id => UniqueAvatarColor.HueFromId(Color.red, id);
		/// </code>
		/// </example>
		public static Func<ushort, Color> GetUserColor = id => UniqueAvatarColor.HueFromId(c, id);

		/// <summary>
		/// Change max number of buffered chat lines
		/// </summary>
		public int ChatBuffer
		{
			get => chatBuffer;
			set => UpdateBufferSize(chatBuffer = value);
		}

		private void Start()
		{
			if (Debug.isDebugBuild || Application.isEditor)
			{
				AllowHostToToggleCheats = true;
				AllowCommands = true;
			}
			
			_chat = new ChatEvent[chatBuffer];
			Multiplayer.OnRoomJoined.AddListener(OnJoinedRoom);
			Multiplayer.OnRoomLeft.AddListener(OnLeftRoom);
			Multiplayer.OnOtherUserJoined.AddListener(OnOtherUserJoined);
			Multiplayer.OnOtherUserLeft.AddListener(OnOtherUserLeft);
			Application.logMessageReceived += HandleLog;
		}

		private new void OnDestroy()
		{
			base.OnDestroy();
			Multiplayer.OnRoomJoined.RemoveListener(OnJoinedRoom);
			Multiplayer.OnRoomLeft.RemoveListener(OnLeftRoom);
			Multiplayer.OnOtherUserJoined.RemoveListener(OnOtherUserJoined);
			Multiplayer.OnOtherUserLeft.RemoveListener(OnOtherUserLeft);
			Application.logMessageReceived -= HandleLog;
		}

		private void OnJoinedRoom(Multiplayer arg0, Room arg1, User arg2)
		{
			if (LogSystemMessages)
			{
				AddChatEventToBuffer(new ChatEvent("joined the room", UseTimeStamps, arg2));
			}
		}

		private void OnLeftRoom(Multiplayer arg0)
		{
			if (LogSystemMessages)
			{
				AddChatEventToBuffer(new ChatEvent("User left room", UseTimeStamps));
			}
		}

		private void OnOtherUserJoined(Multiplayer arg0, User arg1)
		{
			if (LogSystemMessages)
			{
				AddChatEventToBuffer(new ChatEvent("joined the room", UseTimeStamps, arg1));
			}
		}

		private void OnOtherUserLeft(Multiplayer arg0, User arg1)
		{
			if (LogSystemMessages)
			{
				AddChatEventToBuffer(new ChatEvent("left the room", UseTimeStamps, arg1));
			}
		}
		
		void HandleLog(string logString, string stackTrace, LogType type) {
			if (LogErrors && type == LogType.Error) {
				AddChatEventToBuffer(new ChatEvent(logString, UseTimeStamps));
			}
		}

		private void UpdateBufferSize(int size)
		{
			var temp = _chat;
			_chat = new ChatEvent[size];
			Array.Copy(temp, _chat, Math.Min(temp.Length, _chat.Length));
		}
		
		public void LogError(string msg) => 
			AddChatEventToBuffer(new ChatEvent(UseRichText ? "<color=#ED4337>" + msg + "</color>" : msg, UseTimeStamps));

		private void AddChatEventToBuffer(ChatEvent chatEvent)
		{
			//shift array
			for (int i = chatBuffer - 2; i >= 0; i--)
			{
				_chat[i + 1] = _chat[i];
			}

			_chat[0] = chatEvent;

			//sort chat by time stamp
			if (UseTimeStamps)
			{
				for (int i = 1; i < chatBuffer && _chat[i].TimeStamp > _chat[i - 1].TimeStamp; i++)
				{
					chatEvent = _chat[i];
					_chat[i] = _chat[i - 1];
					_chat[i - 1] = chatEvent;
				}
			}
			
			if (chatEvent.Msg.Length > 0)
				Debug.Log(chatEvent.ToString(this));

			TextChatUpdate.Invoke(ToString());
		}

		public void SendChatMessage(string msg)
		{
			if (msg.Length == 0)
			{
				return;
			}

			if (AllowCommands && msg[0] == '/')
			{
				ExecuteCommand(msg, true);
				return;
			}

			ChatEvent chatEvent = new ChatEvent(msg, UseTimeStamps, Multiplayer.Me);
			if (!LogLocalOnSend)
			{
				AddChatEventToBuffer(chatEvent);
			}

			if (!Multiplayer.InRoom)
			{
				return;
			}

			_outgoingMessages.Add(chatEvent);
			Multiplayer.Sync(this, Reliability);
		}

		private void ExecuteCommandAsAdmin(string msg, bool invoker = false)
		{
			bool temp = AllowCommands;
			AllowCommands = true;
			ExecuteCommand(msg, invoker);
			AllowCommands = temp;
		}
		
		private void ExecuteCommand(string msg, bool invoker = false)
		{
			//if (!AllowCommands) return;

			if (invoker)
				AddChatEventToBuffer(new ChatEvent(msg, UseTimeStamps));

			if (msg[0] == '/')
			{
				msg = msg.Substring(1);
			}

			int spaceIndex = msg.IndexOf(' ');
			string command = spaceIndex == -1 ? msg : msg.Substring(0, spaceIndex);
			string commandCi = command.ToUpperInvariant();
			string[] args = spaceIndex == -1 ? Array.Empty<string>() :
				msg.Substring(spaceIndex + 1).Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);

			if (commandCi == "HELP")
			{
				_sb.AppendLine("/boldNames <bool> : Enable/disable bold names");
				_sb.AppendLine("/chatBuffer <int> : Change max number of buffered chat lines");
				_sb.AppendLine("/clear : Clear chat");
				_sb.AppendLine("/help : Show available commands");
				_sb.AppendLine("/logErrors <bool> : Enable/disable logging of errors");
				_sb.AppendLine("/logOnSend <bool> : When true, show message for sender when sending the message. Otherwise it shows when the message in created.");
				_sb.AppendLine("/logSystemMessages <bool> : Enable/disable logging of system messages");
				_sb.AppendLine("/useRichText <bool> : Enable/disable rich names");
				if (AllowHostToToggleCheats)
					_sb.AppendLine("/testingCheats <bool>: Enable/disable cheats");
				if (AllowCheats) 
					_sb.AppendLine("/execute as <all|allInclusive|user id|username> <command>: execute command for other users. The (as <target>) part can be repeated for multiple targets. The command should not start with a slash symbol and can include any number of args.");

				foreach (ITextChatCommand c in Commands)
				{
					if (c.IsCheat && !AllowCheats) continue;

					_sb.Append(c.Usage);
					_sb.Append(" : ");
					_sb.AppendLine(c.Description);
				}

				string s = _sb.ToString();
				_sb.Clear();
				AddChatEventToBuffer(new ChatEvent(s, UseTimeStamps));

			}
			else if (commandCi == "CLEAR")
			{
				for (int i = 0; i < _chat.Length; i++)
				{
					_chat[i] = new ChatEvent();
				}
				AddChatEventToBuffer(new ChatEvent("", UseTimeStamps));
			}
			// ReSharper disable once StringLiteralTypo
			else if (commandCi == "CHATBUFFER")
			{
				if (args.Length == 0)
				{
					AddChatEventToBuffer(new ChatEvent(chatBuffer.ToString(), UseTimeStamps));
				}
				else
				{
					if (int.TryParse(args[0], out int size))
					{
						ChatBuffer = size;
					}
					else
					{
						LogError("Invalid argument");
					}
				}
			}
			else if (commandCi == "CHATBUFFER") { }
			// ReSharper disable once StringLiteralTypo
			else if (ConfigCommand(commandCi == "BOLDNAMES", ref BoldNames, args)) { }
			// ReSharper disable once StringLiteralTypo
			else if (ConfigCommand(commandCi == "LOGERRORS", ref LogErrors, args)) { }
			// ReSharper disable once StringLiteralTypo
			else if (ConfigCommand(commandCi == "LOGONSEND", ref LogLocalOnSend, args)) { }
			// ReSharper disable once StringLiteralTypo
			else if (ConfigCommand(commandCi == "LOGSYSTEMMESSAGES", ref LogSystemMessages, args)) { }
			// ReSharper disable once StringLiteralTypo
			else if (ConfigCommand(commandCi == "USERICHTTEXT", ref UseRichText, args)) { }
			// ReSharper disable once StringLiteralTypo
			else if (commandCi == "TESTINGCHEATS")
			{
				if (AllowHostToToggleCheats && (!Multiplayer || !Multiplayer.IsConnected || Multiplayer.Me.IsHost))
				{
					ConfigCommand(ref AllowCheats, args);
				}
				else
				{
					LogError("Not allowed");
				}
			}
			else if (commandCi == "EXECUTE")
			{
				if (!AllowCheats)
				{
					LogError("Not allowed");
					return;
				}

				// get targets
				int i = 0;
				List<ushort> targets = new List<ushort>();
				while (args.Length >= i + 2 && args[i].ToUpperInvariant() == "AS")
				{
					i++;
					string s = args[i].ToUpperInvariant();
					if (s == "ALL")
					{
						if (targets.Count == 1)
						{
							if (targets[0] < (ushort)UserId.AllInclusive)
							{
								targets[0] = (ushort)UserId.All;
							}
						}
						else
						{
							targets.Clear();
							targets.Add((ushort)UserId.All);
						}
					}
					else if (s == "ALLI" || s == "ALLINCLUSIVE")
					{
						if (targets.Count == 1 && targets[0] != (ushort)UserId.AllInclusive)
						{
							targets[0] = (ushort)UserId.AllInclusive;
						}
						else
						{
							targets.Clear();
							targets.Add((ushort)UserId.AllInclusive);
						}
					}
					else if (ushort.TryParse(args[i], out ushort id))
					{
						targets.Add(id);
					}
					else
					{
						targets.Add(Multiplayer.GetUser(args[i]).Index);
					}
					i++;
				}

				// command to be executed
				string c = string.Join(" ", args.Skip(i));
				string c2 = "";
				bool multi = false;

				int nextExecute = c.IndexOf("EXECUTE", StringComparison.InvariantCultureIgnoreCase);
				if (nextExecute != -1)
				{
					c2 = c.Substring(nextExecute + 7);
					c = c.Substring(0, nextExecute);
					multi = true;
				}

				// execute command

				if (targets.Count == 0) { 
					ExecuteCommand(c);
					return;
				}

				if (targets[0] == (ushort)UserId.AllInclusive || targets.Any(id => id == Multiplayer.Me))
				{
					ExecuteCommand(c);
				}

				if (!invoker) return;
				// remote execute
				_outgoingMessages.Add(new ChatEvent('/' + c));

				if (multi)
				{
					_outgoingMessages.Add(new ChatEvent("/EXECUTE " + c2));
				}
				
				Multiplayer.Sync(this, targets, Reliability);
			}
			else
			{
				foreach (ITextChatCommand c in Commands)
				{
					if (c.IgnoreCase)
					{
						if (string.Equals(command, c.Command, StringComparison.InvariantCultureIgnoreCase))
						{
							Execute();
							return;
						}
					}
					else
					{
						if (command == c.Command)
						{
							Execute();
							return;
						}
					}

					void Execute()
					{
						if (c.IsCheat && !AllowCheats)
						{
							LogError("Not allowed");
							return;
						}
						string s = c.Execute(this, args);
						if (s != null)
							AddChatEventToBuffer(new ChatEvent(s, UseTimeStamps));
					}
				}

				LogError("Command not found");
			}
		}

		private bool ConfigCommand(bool condition, ref bool config, string[] args)
		{
			if (condition)
			{
				ConfigCommand(ref config, args);
				return true;
			}

			return false;
		}

		private bool ConfigCommand(ref bool config, string[] args)
		{
			if (args.Length == 0)
			{
				// Get current value.
				AddChatEventToBuffer(new ChatEvent(config.ToString(), UseTimeStamps));
				return true;
			}

			// Set new value.
			return TextChatCommandHelper.TrySetBoolArg(args[0], ref config);
		}

		public override void AssembleData(Writer writer, byte LOD = 100)
		{
			int messageCount = Math.Min(_outgoingMessages.Count, byte.MaxValue);
			writer.Write((byte)messageCount);
			for (int i = 0; i < messageCount; i++)
			{
				if (LogLocalOnSend && !(AllowCommands && _outgoingMessages[i].IsCommand))
				{
					AddChatEventToBuffer(_outgoingMessages[i]);
				}
				_outgoingMessages[i].Write(writer);
			}

			_outgoingMessages.Clear();
		}

		public override void DisassembleData(Reader reader, byte LOD = 100)
		{
			byte messageCount = reader.ReadByte();
			for (byte i = 0; i < messageCount; i++)
			{
				var e = new ChatEvent(reader);
				
				// execute received commands
				if (AllowCommands && e.Msg.Length > 0 && e.Msg[0] == '/')
				{
					ExecuteCommandAsAdmin(e.Msg);
					continue;
				}

				AddChatEventToBuffer(e);
			}
		}

		public override string ToString()
		{
			for (int i = chatBuffer - 1; i >= 0; i--)
			{
				_sb2.AppendLine(_chat[i].ToString(this));
			}

			string s = _sb2.ToString();
			_sb2.Clear();
			return s;
		}

		public new void Reset()
		{
			base.Reset();
			Reliability = Reliability.Reliable;
			EnsureEventSystem.Ensure(true);
		}

		private struct ChatEvent
		{
			public readonly ushort SenderId;
			public readonly int TimeStamp;
			public string Msg;
			private bool _compiled;
			
			public bool IsCommand => Msg.Length > 0 && Msg[0] == '/';

			public ChatEvent(string s, bool time, ushort senderId = UInt16.MaxValue) : this(s, time ? -1 : 0, senderId) { }

			public ChatEvent(string s = "", int time = -1, ushort senderId = UInt16.MaxValue)
			{
				Msg = s;
				SenderId = senderId;
				_compiled = false;
				if (time < 0)
				{
					var localNow = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
					var timeOffset = new DateTimeOffset(localNow, TimeZoneInfo.Utc.GetUtcOffset(localNow));
					TimeStamp = timeOffset.Millisecond + timeOffset.Second * 1000 + timeOffset.Minute * 60000 + timeOffset.Hour * 3600000;
				}
				else
				{
					TimeStamp = time;
				}
			}

			public ChatEvent(Reader reader)
			{
				_compiled = false;
				byte flags = reader.ReadByte();
				if ((flags & 1) != 0)
				{
					SenderId = reader.ReadUshort();
				}
				else
				{
					SenderId = UInt16.MaxValue;
				}

				if ((flags & 2) != 0)
				{
					TimeStamp = reader.ReadInt();
				}
				else
				{
					var localNow = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
					var timeOffset = new DateTimeOffset(localNow, TimeZoneInfo.Utc.GetUtcOffset(localNow));
					TimeStamp = timeOffset.Millisecond + timeOffset.Second * 1000 + timeOffset.Minute * 60000 + timeOffset.Hour * 3600000;;
				}
				
				Msg = reader.ReadString();
			}

			public void Write(Writer writer)
			{
				byte flags = (SenderId == UInt16.MaxValue ? (byte)0 : (byte)1) + TimeStamp == -1 || TimeStamp == 0 ? (byte)0 : (byte)2;
				writer.Write(flags);
				if ((flags & 1) != 0)
				{
					writer.Write(SenderId);
				}
				if (TimeStamp != -1 && TimeStamp != 0)
				{
					writer.Write(TimeStamp);
				}

				writer.Write(Msg);
			}

			public string ToString(TextChatSynchronizable textChat)
			{
				if (string.IsNullOrEmpty(Msg))
				{
					return null;
				}

				if (_compiled || SenderId == UInt16.MaxValue)
				{
					return Msg;
				}

				string name = textChat.Multiplayer?.GetUser(SenderId)?.Name;
				if (name == null) name = "Unknown";
				
				// Add sender name to message
				if (textChat.UseRichText)
				{
					if (textChat.BoldNames)
					{
						textChat._sb.Append("<b>");
					}

					textChat._sb.Append("<color=#");
					textChat._sb.Append(ColorUtility.ToHtmlStringRGB(GetUserColor(SenderId)));
					textChat._sb.Append('>');
					textChat._sb.Append(name);
					textChat._sb.Append("</color>");

					if (textChat.BoldNames)
					{
						textChat._sb.Append("</b>");
					}
				}
				else
				{
					textChat._sb.Append(name);
				}

				textChat._sb.Append(' ');
				textChat._sb.Append(Msg);
				string msg = textChat._sb.ToString();
				textChat._sb.Clear();

				// Store the user in memory so that it doesn't break when the user leaves
				if (!string.Equals(name, "unknown", StringComparison.InvariantCultureIgnoreCase))
				{
					Msg = msg;
					_compiled = true;
				}
				
				return msg;
			}
		}
	}
#if UNITY_EDITOR
	[CustomEditor(typeof(TextChatSynchronizable))]
	public class TextChatSynchronizableEditor : UniqueIDEditor
	{
		public override void OnInspectorGUI()
		{
			DrawUID();

			var textChat = (TextChatSynchronizable)target;

			DrawDefaultInspector();

			// ReSharper disable once AssignmentInConditionalExpression
			if (textChat.UseRichText)
			{
				textChat.BoldNames = EditorGUILayout.Toggle("Bold Names", textChat.BoldNames);
			}
		}
	}
#endif
}