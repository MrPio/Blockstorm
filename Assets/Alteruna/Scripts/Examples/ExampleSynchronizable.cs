using UnityEngine;
using Alteruna;

/// <summary>
/// Class <c>ExampleSynchronizable</c> is an example of how a <c>Synchronizable</c> can be defined.
/// </summary>
/// 
public class ExampleSynchronizable : Synchronizable
{
	// Data to be synchronized with other players in our playroom.
	public float SynchronizedFloat = 3.0f;

	// Used to store the previous version of our data so that we know when it has changed.
	private float _oldSynchronizedFloat;

	public override void DisassembleData(Reader reader, byte LOD)
	{
		// Set our data to the updated value we have recieved from another player.
		SynchronizedFloat = reader.ReadFloat();

		// Save the new data as our old data, otherwise we will immediatly think it changed again.
		_oldSynchronizedFloat = SynchronizedFloat;
	}

	public override void AssembleData(Writer writer, byte LOD)
	{
		// Write our data so that it can be sent to the other players in our playroom.
		writer.Write(SynchronizedFloat);
	}

	private void Update()
	{
		// If the value of our float has changed, sync it with the other players in our playroom.
		if (SynchronizedFloat != _oldSynchronizedFloat)
		{
			// Store the updated value
			_oldSynchronizedFloat = SynchronizedFloat;

			// Tell Alteruna that we want to commit our data.
			Commit();
		}

		// Update the Synchronizable
		base.SyncUpdate();
	}
}