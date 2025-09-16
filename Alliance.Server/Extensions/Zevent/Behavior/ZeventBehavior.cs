using Alliance.Common.Extensions.CustomScripts.Scripts;
using Alliance.Common.Extensions.Zevent;
using Alliance.Common.Extensions.Zevent.Behaviors;
using Alliance.Server.Core;
using Alliance.Server.Core.Database.Data;
using Alliance.Server.Core.Database.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.Zevent.Behavior
{
	public class ZeventBehavior : MissionNetwork, IMissionBehavior
	{
		struct DonatorInfo
		{
			public string Username;
			public int Tag;
			public int Tier;
			public int Variant;
			public decimal TotalDonation;
			public string LastMessage;
			public bool IsNew;
		}

		public static Dictionary<int, int> PriceForTier = new Dictionary<int, int>()
		{
			{ 1, 10 },
			{ 2, 20 },
			{ 3, 40 },
			{ 4, 80 },
			{ 5, 300 }
		};

		public static Dictionary<int, int> VariantsPerTier = new Dictionary<int, int>()
		{
			{ 1, 4 },
			{ 2, 4 },
			{ 3, 2 },
			{ 4, 2 },
			{ 5, 1 }
		};

		private ZeventCommonBehavior _tentBehavior;
		private Dictionary<string, DonatorInfo> DonatorsInfo = new Dictionary<string, DonatorInfo>();
		private Random _random = new Random();
		private float _lastUpdate = 0f;
		private bool _enabled = false;
		private bool _updateTent = false;
		private int _lastUsedTag = -1;

		private const float UPDATE_INTERVAL = 10;

		public ZeventBehavior() : base()
		{
		}

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();

			if (Mission.Current?.SceneName != ZeventConst.ZEVENT_MAP_NAME && Mission.Current?.SceneName != ZeventConst.ZEVENT_MAP_NIGHT_NAME) return;

			_tentBehavior = Mission.Current.GetMissionBehavior<ZeventCommonBehavior>();

			InitGold();

			UpdateRewardsDB();

			InitDonatorsInfo();

			_enabled = true;
		}

		public override void AfterStart()
		{
			base.AfterStart();

			if (!_enabled) return;
			UpdateTents();
		}

		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);

			if (!_enabled) return;

			_lastUpdate += dt;
			if (_lastUpdate >= UPDATE_INTERVAL)
			{
				_lastUpdate = 0f;
				UpdateRewardsDB();
				UpdateDonatorsInfo();
				UpdateTents();
			}
		}

		private void InitDonatorsInfo()
		{
			try
			{
				AppDbContext dbContext = ServiceLocator.GetService<AppDbContext>();
				Dictionary<string, decimal> donationsByDonator = dbContext.ZeventDonators.AsNoTracking().Select(donator => new KeyValuePair<string, decimal>(donator.Username, donator.ZeventDonations.Sum(donation => donation.DonationAmount))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
				Dictionary<string, ZeventReward> rewardByDonator = dbContext.ZeventDonators.AsNoTracking().Select(donator => new KeyValuePair<string, ZeventReward>(donator.Username, donator.ZeventRewards.OrderByDescending(reward => reward.Tier).FirstOrDefault())).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
				foreach (KeyValuePair<string, decimal> donator in donationsByDonator)
				{
					if (rewardByDonator.TryGetValue(donator.Key, out ZeventReward reward) && reward != null)
					{
						string message = dbContext.ZeventDonators.AsNoTracking().Where(d => d.Username == donator.Key).Select(d => d.ZeventDonations.OrderByDescending(dd => dd.InsertDate).First().DonationComment).FirstOrDefault();
						DonatorsInfo[donator.Key] = new DonatorInfo()
						{
							Username = donator.Key,
							Tag = reward.RewardTag,
							Tier = reward.Tier,
							Variant = reward.Variant,
							TotalDonation = donator.Value,
							LastMessage = message,
							IsNew = true
						};
					}
				}
			}
			catch (System.Exception ex)
			{
				Log("Error while initializing donators info : " + ex.Message, LogLevel.Error);
			}
		}

		private void UpdateDonatorsInfo()
		{
			try
			{
				AppDbContext dbContext = ServiceLocator.GetService<AppDbContext>();
				Dictionary<string, decimal> donationsByDonator = dbContext.ZeventDonators.AsNoTracking().Select(donator => new KeyValuePair<string, decimal>(donator.Username, donator.ZeventDonations.Sum(donation => donation.DonationAmount))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
				Dictionary<string, ZeventReward> rewardByDonator = dbContext.ZeventDonators.AsNoTracking().Select(donator => new KeyValuePair<string, ZeventReward>(donator.Username, donator.ZeventRewards.OrderByDescending(reward => reward.Tier).FirstOrDefault())).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

				foreach (KeyValuePair<string, decimal> donator in donationsByDonator)
				{
					if (rewardByDonator.TryGetValue(donator.Key, out ZeventReward reward) && reward != null)
					{
						bool hasPreviousInfo = DonatorsInfo.TryGetValue(donator.Key, out DonatorInfo existingInfo);
						if (hasPreviousInfo && existingInfo.Tier == reward.Tier && existingInfo.Variant == reward.Variant && existingInfo.Tag == reward.RewardTag && existingInfo.TotalDonation == donator.Value)
						{
							// No change
							continue;
						}
						else
						{
							string message = dbContext.ZeventDonators.AsNoTracking().Where(d => d.Username == donator.Key).Select(d => d.ZeventDonations.OrderByDescending(dd => dd.InsertDate).First().DonationComment).FirstOrDefault();
							// Update existing info
							existingInfo.Username = reward.Username;
							existingInfo.Tier = reward.Tier;
							existingInfo.Variant = reward.Variant;
							existingInfo.Tag = reward.RewardTag;
							existingInfo.TotalDonation = donator.Value;
							existingInfo.LastMessage = message;
							existingInfo.IsNew = true;
							DonatorsInfo[donator.Key] = existingInfo;
						}
					}
				}

				int totalDonationFracas = dbContext.ZeventDonations.Sum(d => (int?)d.DonationAmount) ?? 0;
				if (totalDonationFracas > 2500) _updateTent = true;
			}
			catch (System.Exception ex)
			{
				Log("Error while initializing donators info : " + ex.Message, LogLevel.Error);
			}
		}

		private void UpdateRewardsDB()
		{
			try
			{
				AppDbContext dbContext = ServiceLocator.GetService<AppDbContext>();
				Dictionary<string, decimal> donationsByDonator = dbContext.ZeventDonators.AsNoTracking().Select(donator => new KeyValuePair<string, decimal>(donator.Username, donator.ZeventDonations.Sum(donation => donation.DonationAmount))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
				Dictionary<string, int> rewardByDonator = dbContext.ZeventDonators.AsNoTracking().Select(donator => new
				{
					donator.Username,
					MaxTier = donator.ZeventRewards.Select(reward => (int?)reward.Tier).Max() ?? 0
				}).ToDictionary(kvp => kvp.Username, kvp => kvp.MaxTier);
				foreach (KeyValuePair<string, decimal> donator in donationsByDonator)
				{
					bool hasAReward = rewardByDonator.TryGetValue(donator.Key, out int oldRewardTier) && oldRewardTier > 0;
					int newRewardTier = GetRewardTierForAmount(donator.Value);
					if (newRewardTier > oldRewardTier)
					{
						string lastMessage = dbContext.ZeventDonators.AsNoTracking().Where(d => d.Username == donator.Key).Select(d => d.ZeventDonations.OrderByDescending(dd => dd.InsertDate).First().DonationComment).FirstOrDefault();
						int freeTag;
						if (hasAReward)
						{
							// Donator already has a reward, keep the same tag
							freeTag = dbContext.ZeventRewards.Where(d => d.Username == donator.Key).OrderByDescending(dd => dd.InsertDate).First().RewardTag;
						}
						else
						{
							// Get last used tag from DB and compare to last one used
							int _lastUsedTagDB = dbContext.ZeventRewards.Any() ? dbContext.ZeventRewards.Max(r => r.RewardTag) : 0;
							// If DB last used tag is higher than the one we have, use it
							if (_lastUsedTag == -1 || _lastUsedTagDB > _lastUsedTag) _lastUsedTag = _lastUsedTagDB;
							// Increment last used tag
							_lastUsedTag++;
							freeTag = _lastUsedTag;
						}
						AssignNewRewardToDonator(dbContext, donator.Key, donator.Value, freeTag, newRewardTier, GetRandomVariant(newRewardTier), lastMessage);
					}
				}
				try
				{
					dbContext.SaveChanges();
				}
				catch (DbUpdateException ex)
				{
					Log("Can't save into DB ! Due to : " + ex.Message, LogLevel.Error);
				}
			}
			catch (System.Exception ex)
			{
				Log("Error while updating rewards : " + ex.Message, LogLevel.Error);
			}
		}

		private int GetRandomVariant(int newRewardTier)
		{
			if (VariantsPerTier.TryGetValue(newRewardTier, out int variants))
			{
				return _random.Next(1, variants + 1);
			}
			return 1;
		}

		private void AssignNewRewardToDonator(AppDbContext dbContext, string donator, decimal totalDonation, int tag, int newRewardTier, int variant, string message)
		{
			// assign new reward
			ZeventReward reward = new ZeventReward()
			{
				InsertDate = System.DateTime.Now,
				LastUpdateDate = System.DateTime.Now,
				Username = donator,
				Tier = newRewardTier,
				Variant = variant,
				RewardTag = tag
			};

			// Add to db
			dbContext.ZeventRewards.Add(reward);
		}

		private int GetRewardTierForAmount(decimal amount)
		{
			int tier = 0;
			foreach (KeyValuePair<int, int> priceForTier in PriceForTier)
			{
				if (amount >= priceForTier.Value)
				{
					tier = priceForTier.Key;
				}
			}
			return tier;
		}

		private void UpdateTents()
		{
			if (!_updateTent) return;

			try
			{
				// Iterate over donator infos to setup tents
				for (int i = 0; i < DonatorsInfo.Count; i++)
				{
					DonatorInfo donatorInfo = DonatorsInfo.ElementAt(i).Value;
					if (!donatorInfo.IsNew) continue;

					Log($"Hey {donatorInfo.Username}, you are tier {donatorInfo.Tier} variant {donatorInfo.Variant} with tag {donatorInfo.Tag} and total donation of {donatorInfo.TotalDonation}€", LogLevel.Information);

					// Spawn tent
					_tentBehavior.SpawnTent(donatorInfo.Tag, donatorInfo.Tier, donatorInfo.Variant, (int)donatorInfo.TotalDonation, donatorInfo.Username, donatorInfo.LastMessage);

					donatorInfo.IsNew = false;
					DonatorsInfo[donatorInfo.Username] = donatorInfo;
				}
			}
			catch (System.Exception ex)
			{
				Log("Error while updating tents : " + ex.Message, LogLevel.Error);
			}
		}

		private void InitGold()
		{
			try
			{
				Log("Map Zevent detected, sync gold pile with last known value", LogLevel.Information);

				// Set gold pile to last know value from DB

				AppDbContext dbContext = ServiceLocator.GetService<AppDbContext>();
				ZeventGoldPile moreRecentGoldPile = dbContext.ZeventGoldPiles.OrderByDescending(e => e.InsertDate).FirstOrDefault();

				if (moreRecentGoldPile == null)
				{
					moreRecentGoldPile = new ZeventGoldPile();
					// There is no gold pile in DB yet we need to init it
					moreRecentGoldPile.InsertDate = System.DateTime.Now;
					moreRecentGoldPile.LastUpdateDate = System.DateTime.Now;
					moreRecentGoldPile.GoldAmount = 0;
				}

				// If map is Zevent, we need to sync gold pile
				GameEntity gameEntity = Mission.Current.Scene.GetFirstEntityWithScriptComponent<CS_DynamicPile>();
				if (gameEntity == null)
				{
					Log("There is no gold pile in this map.", LogLevel.Warning);
					return;
				}
				CS_DynamicPile goldPileScript = gameEntity.GetFirstScriptOfType<CS_DynamicPile>();
				goldPileScript.SetVolume(moreRecentGoldPile.GoldAmount / 1000f);
				goldPileScript.SetVolumeTarget(moreRecentGoldPile.GoldAmount / 1000f);

				ZeventMsg.RequestClientsToUpdateGoldPile(moreRecentGoldPile.GoldAmount, moreRecentGoldPile.GoldAmount);

			}
			catch (System.Exception ex)
			{
				Log("Error while initializing gold pile : " + ex.Message, LogLevel.Error);
			}
		}

		protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
		{
			base.HandleNewClientAfterSynchronized(networkPeer);
			SyncGoldPileOfConnectingUsers(networkPeer);
		}

		public static void SyncGoldPileOfConnectingUsers(NetworkCommunicator networkPeer)
		{
			if (Mission.Current?.SceneName != ZeventConst.ZEVENT_MAP_NAME && Mission.Current?.SceneName != ZeventConst.ZEVENT_MAP_NIGHT_NAME) return;

			Log("Player joining on Zevent map, sync gold pile to him", LogLevel.Information);

			// If map is Zevent, we need to sync gold pile
			GameEntity gameEntity = Mission.Current.Scene.GetFirstEntityWithScriptComponent<CS_DynamicPile>();
			if (gameEntity == null)
			{
				Log("There is no gold pile in this map.", LogLevel.Warning);
				return;
			}

			CS_DynamicPile goldPileScript = gameEntity.GetFirstScriptOfType<CS_DynamicPile>();

			int realBaseVolume = (int)(goldPileScript.CurrentVolume * 1000f);
			int realTargetVolume = (int)(goldPileScript.VolumeTargetted * 1000f);

			ZeventMsg.RequestClientToUpdateGoldPile(realTargetVolume, networkPeer, realBaseVolume);
		}
	}
}
