using Alliance.Server.Core.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Alliance.Server.Core.Database.Data
{
	public partial class AppDbContext : DbContext
	{
		public AppDbContext()
		{
		}

		public AppDbContext(DbContextOptions<AppDbContext> options)
			: base(options)
		{
		}

		public virtual DbSet<BnlGame> BnlGames { get; set; }
		public virtual DbSet<BnlMsg> BnlMsgs { get; set; }
		public virtual DbSet<BnlPlayer> BnlPlayers { get; set; }
		public virtual DbSet<BnlScenario> BnlScenarios { get; set; }
		public virtual DbSet<BnlTk> BnlTks { get; set; }
		public virtual DbSet<DiscordEvent> DiscordEvents { get; set; }
		public virtual DbSet<DiscordSpamContentMsgDico> DiscordSpamContentMsgDicos { get; set; }
		public virtual DbSet<DiscordSpamMsg> DiscordSpamMsgs { get; set; }
		public virtual DbSet<DiscordUser> DiscordUsers { get; set; }
		public virtual DbSet<DiscordUserWarning> DiscordUserWarnings { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				optionsBuilder.UseNpgsql(DbConfig.DbConnectionString);
			}
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.HasPostgresExtension("uuid-ossp");

			modelBuilder.Entity<BnlGame>(entity =>
			{
				entity.ToTable("bnl_game");

				entity.Property(e => e.Id).HasColumnName("id");

				entity.Property(e => e.BnlScenarioId).HasColumnName("bnl_scenario_id");

				entity.Property(e => e.LstUpdTmstmp)
					.HasColumnType("timestamp without time zone")
					.HasColumnName("lst_upd_tmstmp");

				entity.Property(e => e.MapName)
					.HasMaxLength(50)
					.HasColumnName("map_name");

				entity.HasOne(d => d.BnlScenario)
					.WithMany(p => p.BnlGames)
					.HasForeignKey(d => d.BnlScenarioId)
					.HasConstraintName("FK_725e436adfc629f84b68cb56dd1");
			});

			modelBuilder.Entity<BnlMsg>(entity =>
			{
				entity.ToTable("bnl_msg");

				entity.Property(e => e.Id).HasColumnName("id");

				entity.Property(e => e.BnlGameId).HasColumnName("bnl_game_id");

				entity.Property(e => e.BnlPlayerId).HasColumnName("bnl_player_id");

				entity.Property(e => e.LstUpdTmstmp)
					.HasColumnType("timestamp without time zone")
					.HasColumnName("lst_upd_tmstmp");

				entity.Property(e => e.Msg)
					.IsRequired()
					.HasMaxLength(500)
					.HasColumnName("msg");

				entity.Property(e => e.PlayerName)
					.IsRequired()
					.HasMaxLength(50)
					.HasColumnName("player_name");

				entity.HasOne(d => d.BnlGame)
					.WithMany(p => p.BnlMsgs)
					.HasForeignKey(d => d.BnlGameId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("FK_105abc8a5948adea354ccf26cf2");

				entity.HasOne(d => d.BnlPlayer)
					.WithMany(p => p.BnlMsgs)
					.HasForeignKey(d => d.BnlPlayerId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("FK_5b557c5df00b75000651f0a7689");
			});

			modelBuilder.Entity<BnlPlayer>(entity =>
			{
				entity.ToTable("bnl_player");

				entity.Property(e => e.Id).HasColumnName("id");

				entity.Property(e => e.DiscordId).HasColumnName("discord_id");

				entity.Property(e => e.IsrtTmstmp)
					.HasColumnType("timestamp without time zone")
					.HasColumnName("isrt_tmstmp");

				entity.Property(e => e.LstUpdTmstmp)
					.HasColumnType("timestamp without time zone")
					.HasColumnName("lst_upd_tmstmp");

				entity.Property(e => e.Username)
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnName("username");

				entity.HasOne(d => d.Discord)
					.WithMany(p => p.BnlPlayers)
					.HasForeignKey(d => d.DiscordId)
					.HasConstraintName("FK_9ed60a7483375913cfa6a4e367c");
			});

			modelBuilder.Entity<BnlScenario>(entity =>
			{
				entity.ToTable("bnl_scenario");

				entity.Property(e => e.Id).HasColumnName("id");

				entity.Property(e => e.GameMode)
					.IsRequired()
					.HasMaxLength(10)
					.HasColumnName("game_mode");

				entity.Property(e => e.LstUpdTmstmp)
					.HasColumnType("timestamp without time zone")
					.HasColumnName("lst_upd_tmstmp");

				entity.Property(e => e.Name)
					.IsRequired()
					.HasMaxLength(10)
					.HasColumnName("name");
			});

			modelBuilder.Entity<BnlTk>(entity =>
			{
				entity.ToTable("bnl_tk");

				entity.Property(e => e.Id).HasColumnName("id");

				entity.Property(e => e.AttackerName)
					.IsRequired()
					.HasMaxLength(50)
					.HasColumnName("attacker_name");

				entity.Property(e => e.BnlGameId).HasColumnName("bnl_game_id");

				entity.Property(e => e.DamageTaken).HasColumnName("damage_taken");

				entity.Property(e => e.LstUpdTmstmp)
					.HasColumnType("timestamp without time zone")
					.HasColumnName("lst_upd_tmstmp");

				entity.Property(e => e.VictimName)
					.IsRequired()
					.HasMaxLength(50)
					.HasColumnName("victim_name");

				entity.HasOne(d => d.BnlGame)
					.WithMany(p => p.BnlTks)
					.HasForeignKey(d => d.BnlGameId)
					.HasConstraintName("FK_7a6d1c0465017673e7aa550af34");
			});

			modelBuilder.Entity<DiscordEvent>(entity =>
			{
				entity.ToTable("discord_event");

				entity.Property(e => e.Id).HasColumnName("id");

				entity.Property(e => e.BannerUrl)
					.HasMaxLength(2000)
					.HasColumnName("banner_url");

				entity.Property(e => e.CreationTmstmp)
					.HasColumnType("timestamp without time zone")
					.HasColumnName("creation_tmstmp");

				entity.Property(e => e.CreatorDiscId)
					.HasMaxLength(100)
					.HasColumnName("creator_disc_id");

				entity.Property(e => e.Desc)
					.HasMaxLength(2000)
					.HasColumnName("desc");

				entity.Property(e => e.DiscordEventId)
					.HasMaxLength(100)
					.HasColumnName("discord_event_id");

				entity.Property(e => e.EventChannelId)
					.HasMaxLength(100)
					.HasColumnName("event_channel_id");

				entity.Property(e => e.EventDate)
					.HasColumnType("timestamp without time zone")
					.HasColumnName("event_date");

				entity.Property(e => e.GameType)
					.HasMaxLength(100)
					.HasColumnName("game_type");

				entity.Property(e => e.IsHabitue).HasColumnName("is_habitue");

				entity.Property(e => e.IsrtTmstmp)
					.HasColumnType("timestamp(6) without time zone")
					.HasColumnName("isrt_tmstmp")
					.HasDefaultValueSql("now()");

				entity.Property(e => e.LstUpdTmstmp)
					.HasColumnType("timestamp without time zone")
					.HasColumnName("lst_upd_tmstmp");

				entity.Property(e => e.Name)
					.HasMaxLength(100)
					.HasColumnName("name");

				entity.Property(e => e.RequestMsgId)
					.HasMaxLength(100)
					.HasColumnName("request_msg_id");

				entity.Property(e => e.RequesterDiscId)
					.IsRequired()
					.HasMaxLength(100)
					.HasColumnName("requester_disc_id");

				entity.Property(e => e.Status)
					.HasMaxLength(100)
					.HasColumnName("status");

				entity.Property(e => e.ValidationTmstmp)
					.HasColumnType("timestamp without time zone")
					.HasColumnName("validation_tmstmp");

				entity.Property(e => e.ValidatorDiscId)
					.HasMaxLength(100)
					.HasColumnName("validator_disc_id");
			});

			modelBuilder.Entity<DiscordSpamContentMsgDico>(entity =>
			{
				entity.HasKey(e => e.SpamMsgId)
					.HasName("pk_3f2c94a7400709aee4b52f4b7ca");

				entity.ToTable("discord_spam_content_msg_dico");

				entity.Property(e => e.SpamMsgId)
					.HasMaxLength(50)
					.HasColumnName("spam_msg_id");

				entity.Property(e => e.DeletedAt)
					.HasColumnType("timestamp without time zone")
					.HasColumnName("deletedAt");

				entity.Property(e => e.WasSafe)
					.HasMaxLength(1)
					.HasColumnName("was_safe");
			});

			modelBuilder.Entity<DiscordSpamMsg>(entity =>
			{
				entity.HasKey(e => e.TechId)
					.HasName("pk_feed7403abb095d591d6979a3fe");

				entity.ToTable("discord_spam_msg");

				entity.Property(e => e.TechId)
					.HasColumnName("tech_id")
					.HasDefaultValueSql("uuid_generate_v4()");

				entity.Property(e => e.ChannelId)
					.HasMaxLength(50)
					.HasColumnName("channel_id");

				entity.Property(e => e.Content)
					.HasMaxLength(1024)
					.HasColumnName("content");

				entity.Property(e => e.ContentLink)
					.HasMaxLength(1024)
					.HasColumnName("content_link");

				entity.Property(e => e.DeletedAt)
					.HasColumnType("timestamp without time zone")
					.HasColumnName("deletedAt");

				entity.Property(e => e.MsgId)
					.HasMaxLength(50)
					.HasColumnName("msg_id");

				entity.Property(e => e.OwnerId)
					.HasMaxLength(50)
					.HasColumnName("owner_id");

				entity.Property(e => e.OwnerName)
					.HasMaxLength(50)
					.HasColumnName("owner_name");

				entity.Property(e => e.RecieveDate)
					.HasColumnType("timestamp without time zone")
					.HasColumnName("recieve_date");

				entity.Property(e => e.SpamMsgDicoId)
					.HasMaxLength(50)
					.HasColumnName("spam_msg_dico_id");

				entity.HasOne(d => d.SpamMsgDico)
					.WithMany(p => p.DiscordSpamMsgs)
					.HasForeignKey(d => d.SpamMsgDicoId)
					.HasConstraintName("FK_75ce385ab35f12505b0e1735aaa");
			});

			modelBuilder.Entity<DiscordUser>(entity =>
			{
				entity.ToTable("discord_user");

				entity.Property(e => e.Id).HasColumnName("id");

				entity.Property(e => e.CreatedAt)
					.HasColumnType("timestamp without time zone")
					.HasColumnName("created_at")
					.HasDefaultValueSql("now()");

				entity.Property(e => e.DiscordId)
					.IsRequired()
					.HasMaxLength(50)
					.HasColumnName("discord_id");

				entity.Property(e => e.DiscordTagName)
					.HasMaxLength(50)
					.HasColumnName("discord_tag_name");

				entity.Property(e => e.DisplayedName)
					.HasMaxLength(50)
					.HasColumnName("displayed_name");

				entity.Property(e => e.LastEventScoreReceived).HasColumnName("last_event_score_received");

				entity.Property(e => e.LstUpdTmstmp)
					.HasColumnType("timestamp without time zone")
					.HasColumnName("lst_upd_tmstmp")
					.HasDefaultValueSql("now()");

				entity.Property(e => e.ParticipationPoints).HasColumnName("participation_points");
			});

			modelBuilder.Entity<DiscordUserWarning>(entity =>
			{
				entity.ToTable("discord_user_warning");

				entity.Property(e => e.Id).HasColumnName("id");

				entity.Property(e => e.CreatedAt)
					.HasColumnType("timestamp without time zone")
					.HasColumnName("created_at")
					.HasDefaultValueSql("now()");

				entity.Property(e => e.Reason)
					.IsRequired()
					.HasMaxLength(500)
					.HasColumnName("reason");

				entity.Property(e => e.SenderDiscordId)
					.IsRequired()
					.HasMaxLength(50)
					.HasColumnName("sender_discord_id");

				entity.Property(e => e.TargetDiscordId)
					.IsRequired()
					.HasMaxLength(50)
					.HasColumnName("target_discord_id");

				entity.Property(e => e.Username)
					.IsRequired()
					.HasMaxLength(50)
					.HasColumnName("username");

				entity.Property(e => e.WarningLevel)
					.IsRequired()
					.HasMaxLength(50)
					.HasColumnName("warning_level");
			});

			OnModelCreatingPartial(modelBuilder);
		}

		partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
	}
}
