using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.AnimationPlayer.Models
{
	/// <summary>
	/// Store users personal animations and favorites ones.    
	/// </summary>
	public class AnimationUserStore
	{
		private static AnimationUserStore _instance;
		public static AnimationUserStore Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new AnimationUserStore();
				}
				return _instance;
			}
		}

		public List<int> FavoriteAnimations { get; set; }
		public List<AnimationSequence> AnimationSequences { get; set; }
		public List<AnimationSet> AnimationSets { get; set; }

		private string _filePath;

		private AnimationUserStore()
		{
		}

		public void Init()
		{
			FavoriteAnimations = new List<int>();
			AnimationSequences = new List<AnimationSequence>();
			AnimationSets = new List<AnimationSet>();
			AnimationSets.Add(new AnimationSet(0, "Default"));
			if (GameNetwork.IsServer)
			{
				_filePath = Path.Combine(ModuleHelper.GetModuleFullPath(SubModule.CurrentModuleName), "Animations/AvailableAnimations.xml");
				if (File.Exists(_filePath))
				{
					Deserialize(_filePath);
					Log($"Alliance - Loaded {_filePath} : {AnimationSequences.Count} sequences and {AnimationSets.Count} sets.", LogLevel.Information);
				}
			}
			else
			{
				_filePath = PathHelper.GetAllianceDocumentFilePath("MyAnimations.xml");
				// Only loads user config for admins for now
				if (File.Exists(_filePath) && GameNetwork.MyPeer != null && GameNetwork.MyPeer.IsAdmin())
				{
					Deserialize(_filePath);
					string log = "Alliance - Favorites : " + FavoriteAnimations.Count +
						" / Sequences : " + AnimationSequences.Count +
						" / Sets : " + AnimationSets.Count;
					Log("Alliance - Loaded user custom animations :", LogLevel.Debug);
					Log(log, LogLevel.Debug);
				}
				else
				{
					try
					{
						string path = Path.Combine(ModuleHelper.GetModuleFullPath(SubModule.CurrentModuleName), "Animations/AvailableAnimations.xml");
						if (File.Exists(path))
						{
							Deserialize(path);
						}

						string directory = Path.GetDirectoryName(_filePath);
						if (!Directory.Exists(directory))
						{
							Directory.CreateDirectory(directory);
						}
						Serialize();
					}
					catch (Exception ex)
					{
						Log("Alliance - Failed to save user animations.", LogLevel.Error);
						Log(ex.Message, LogLevel.Error);
					}
				}
			}
		}

		/// <summary>
		/// Serialize current instance and save it to user personal file.
		/// </summary>
		public void Serialize()
		{
			try
			{
				using (FileStream fs = new FileStream(_filePath, FileMode.Create))
				{
					XmlSerializer serializer = new XmlSerializer(typeof(AnimationUserStore));
					serializer.Serialize(fs, this);
				}
			}
			catch (Exception ex)
			{
				Log("Alliance - Failed to save file " + _filePath, LogLevel.Error);
				Log(ex.Message, LogLevel.Error);
			}
		}

		/// <summary>
		/// Deserialize file into current instance.
		/// </summary>
		public void Deserialize(string path)
		{
			try
			{
				using (FileStream fs = new FileStream(path, FileMode.Open))
				{
					XmlSerializer serializer = new XmlSerializer(typeof(AnimationUserStore));
					AnimationUserStore deserializedStore = (AnimationUserStore)serializer.Deserialize(fs);
					FavoriteAnimations = new List<int>(deserializedStore.FavoriteAnimations);
					AnimationSequences = new List<AnimationSequence>(deserializedStore.AnimationSequences);
					AnimationSets = new List<AnimationSet>(deserializedStore.AnimationSets);
				}
			}
			catch (Exception ex)
			{
				Log("Alliance - Failed to load user animations.", LogLevel.Error);
				Log(ex.Message, LogLevel.Error);
			}
		}

		/// <summary>
		/// Add or remove given animation from favorites using its unique index.
		/// </summary>
		public void ToggleFavoriteAnimation(int animationIndex, bool isFavorite)
		{
			if (!isFavorite && FavoriteAnimations.Contains(animationIndex))
			{
				FavoriteAnimations.Remove(animationIndex);
			}
			else if (isFavorite)
			{
				FavoriteAnimations.Add(animationIndex);
			}
		}

		/// <summary>
		/// Bind given animation and shortcut.
		/// </summary>
		public void BindAnimation(AnimationSet animSet, int shortcutIndex, string shortcut, int animSequenceIndex)
		{
			AnimationSets.Find(x => x.Index == animSet.Index).BindedAnimSequence[shortcutIndex] = new BindedAnimation(shortcut, animSequenceIndex);
		}
	}

	/// <summary>
	/// Store a set of BindedAnimation
	/// </summary>
	[Serializable]
	public class AnimationSet
	{
		public int Index { get; set; }
		public string Name { get; set; }
		public BindedAnimation[] BindedAnimSequence { get; set; }

		public AnimationSet()
		{
		}

		public AnimationSet(int index, string name)
		{
			Index = index;
			Name = name;
			BindedAnimSequence = new BindedAnimation[9];
		}
	}

	/// <summary>
	/// Serializable class to store animations sequences and their associated keybinds.
	/// </summary>
	[Serializable]
	public class BindedAnimation
	{
		public string Keybind { get; set; }
		public int AnimSequenceIndex { get; set; }

		public BindedAnimation() { }

		public BindedAnimation(string keybind, int animSequenceIndex)
		{
			Keybind = keybind;
			AnimSequenceIndex = animSequenceIndex;
		}
	}
}