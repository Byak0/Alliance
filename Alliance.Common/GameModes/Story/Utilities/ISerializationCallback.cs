namespace Alliance.Common.GameModes.Story.Utilities
{
	/// <summary>
	/// Interface for objects that need to perform operations before of after serialization.
	/// Used with the ScenarioSerializer.
	/// </summary>
	public interface ISerializationCallback
	{
		void OnBeforeSerialize();
		void OnAfterDeserialize();
	}
}