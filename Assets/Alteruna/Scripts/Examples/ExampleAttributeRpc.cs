using UnityEngine;
using Alteruna;

public class ExampleAttributeRpc : AttributesSync
{
	public void SendRpc()
	{
		// Invoke method by name. alternatively, we can call by index.
		BroadcastRemoteMethod(nameof(ReceiveMessage), "Hello, world!");
	}

	// the SynchronizableMethod attribute marks methods available for remote invocation.
	[SynchronizableMethod]
	private void ReceiveMessage(string msg)
	{
		Debug.Log(msg);
	}
}