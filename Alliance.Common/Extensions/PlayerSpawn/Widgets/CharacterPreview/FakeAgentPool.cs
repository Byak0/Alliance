#if !SERVER
using System;
using System.Collections.Generic;
using Alliance.Common.Core.Utils;

namespace Alliance.Common.Extensions.PlayerSpawn.Widgets.CharacterPreview
{
	/// <summary>
	/// Pool of dressed tableaus fakes, keyed by character (id + culture + gender + equipment seed).
	/// Formation/team refreshes recreate every widget: without the pool, each refresh destroyed and
	/// recreated all the fakes, reloading all their meshes (the add-agent lag). Parked fakes stay
	/// invisible in the tableau scene until a widget showing the same character acquires them.
	/// </summary>
	public static class FakeAgentPool
	{
		private const int Capacity = 64;

		private static readonly Dictionary<string, List<FakeAgent>> _byKey = new Dictionary<string, List<FakeAgent>>();
		private static readonly Queue<(string Key, FakeAgent Fake)> _parked = new Queue<(string, FakeAgent)>();

		/// <summary>Pool key of a dressed fake: character identity (id + race), equipment seed,
		/// equipment code and weapon state - the same character displayed with a different race,
		/// perks or drawn weapons must never swap each other's fakes through the pool.</summary>
		public static string BuildKey(AllianceData.CharacterDefinition definition, int equipmentSeed, string equipmentCode, bool weaponsDrawn)
		{
			return $"{(weaponsDrawn ? "armed" : "plain")}|{definition.Id}|{definition.RaceId}|{definition.CultureId}|{definition.IsFemale}|{equipmentSeed}|{equipmentCode ?? ""}";
		}

		/// <summary>Returns a parked fake for the key, or creates a new one through <paramref name="create"/>.</summary>
		public static FakeAgent Acquire(string key, Func<FakeAgent> create)
		{
			if (_byKey.TryGetValue(key, out List<FakeAgent> parked))
			{
				while (parked.Count > 0)
				{
					FakeAgent fake = parked[parked.Count - 1];
					parked.RemoveAt(parked.Count - 1);
					if (fake.IsValid) return fake;
				}
			}
			return create();
		}

		/// <summary>Parks a fake invisible under its key, ready for the next acquire. Evicts the
		/// oldest parked fake past the capacity (despawned - its meshes reload on next use).</summary>
		public static void Return(string key, FakeAgent fake)
		{
			if (fake == null || !fake.IsValid) return;
			fake.SetVisible(false);
			if (!_byKey.TryGetValue(key, out List<FakeAgent> parked))
			{
				_byKey[key] = parked = new List<FakeAgent>();
			}
			else if (parked.Contains(fake))
			{
				// Already parked (a refresh sequence returning the same fake twice) - double
				// entries would double-evict and despawn fakes re-acquired in between.
				return;
			}
			parked.Add(fake);
			_parked.Enqueue((key, fake));
			while (_parked.Count > Capacity)
			{
				(string evictedKey, FakeAgent evicted) = _parked.Dequeue();
				// The queue also carries stale entries of re-acquired fakes - only despawn the
				// fake if it is still parked under its key (an in-use fake must never die here).
				if (_byKey.TryGetValue(evictedKey, out List<FakeAgent> evictedList) && evictedList.Remove(evicted))
				{
					evicted.Despawn();
				}
			}
		}
	}
}
#endif
