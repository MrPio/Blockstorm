using Alteruna;
using Alteruna.Trinity;
using UnityEngine;

public class ExampleRpcProcedureParameters : CommunicationBridge
{
	// Name of our RPC event.
	public const string RPC_NAME = "MyExampleRpc";

	private void Start()
	{
		// Register our remote procedure.
		Multiplayer.RegisterRemoteProcedure(RPC_NAME, RpcMethod);
	}

	// Call our rpc.
	public void SendRpc()
	{
		// Create parameters to store our data.
		ProcedureParameters parameters = new ProcedureParameters();
		// Write a message as a string.
		parameters.Set("msg", "Hello, world!");
		// Send our RPC event.
		Multiplayer.InvokeRemoteProcedure(RPC_NAME, UserId.All, parameters);
	}

	// Receive RPC event.
	private void RpcMethod(ushort fromUser, ProcedureParameters parameters, uint callId, ITransportStreamReader processor)
	{
		// Log our message.
		Debug.Log(parameters.Get("msg", ""));
	}
}