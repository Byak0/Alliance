using Alliance.Common.Extensions.CustomScripts.Scripts;
using Alliance.Server.Core;
using Alliance.Server.Core.Database.Data;
using Alliance.Server.Core.Database.Models;
using Alliance.Server.Core.Security;
using Alliance.Server.Extensions.Zevent.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.Zevent
{
	public class ZeventService
	{
		private readonly static ZeventService _instance = new();
		public static ZeventService Instance => _instance;

		private readonly HttpClient client = new();
		private readonly AppDbContext dbContext = ServiceLocator.GetService<AppDbContext>();

		private ZeventService() { }

		/// <summary>
		/// Performs the following actions:
		/// <list type="bullet">
		/// <item>
		/// <description>Calls the Zevent GET endpoint to retrieve the current donation total.</description>
		/// </item>
		/// <item>
		/// <description>Updates the gold pile on the map, if found.</description>
		/// </item>
		/// <item>
		/// <description>Saves the new donation total into the database.</description>
		/// </item>
		/// </list>
		/// </summary>
		/// <param name="zEventEffect">The effect to apply based on the Zevent donation total.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		public async Task RefreshZeventGoldPileAsync()
		{
			// Call Zevent API in order to get new total donation amount
			string zEventDonationApi = SecretsManager.ZEVENT_AMOUNT_GET_API;

			try
			{
				string jsonResponse = await client.GetStringAsync(zEventDonationApi);
				DonationResponse response = JsonSerializer.Deserialize<DonationResponse>(jsonResponse);
				Log("Response from ZEVENT API : " + response, LogLevel.Debug);

				// Update gold pile with new target amount
				GameEntity gameEntity = Mission.Current.Scene.GetFirstEntityWithScriptComponent<CS_DynamicPile>();
				if (gameEntity == null)
				{
					Log("There is no gold pile in this map.", LogLevel.Warning);
					return;
				}

				CS_DynamicPile goldPileScript = gameEntity.GetFirstScriptOfType<CS_DynamicPile>();
				int pileTargetAmount = (int)response.total;

				// We need to divide by 1000 because CS_DynamicPile max value is 20_000 instead of 20_000_000
				goldPileScript.SetVolumeTarget(pileTargetAmount / 1000);
				ZeventMsg.RequestClientsToUpdateGoldPile(pileTargetAmount);

				// Save new total donation amount in DB
				ZeventGoldPile newZeventGoldPile = new ZeventGoldPile();
				newZeventGoldPile.InsertDate = DateTime.Now;
				newZeventGoldPile.LastUpdateDate = DateTime.Now;
				newZeventGoldPile.GoldAmount = pileTargetAmount;
				dbContext.ZeventGoldPiles.Add(newZeventGoldPile);
				try
				{
					dbContext.SaveChanges();
				}
				catch (DbUpdateException ex)
				{
					Log("Can't save into DB ! Due to : " + ex.Message, LogLevel.Error);
				}
			}
			catch (Exception ex)
			{
				Log("There was an issue during ZEVENT API call !\n " + ex.Message, LogLevel.Error);
			}
		}
	}
}
