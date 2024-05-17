<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output omit-xml-declaration="yes" />
  <xsl:template match="@*|node()">
	<xsl:copy>
	  <xsl:apply-templates select="@*|node()" />
	</xsl:copy>
  </xsl:template>

  <xsl:template match='monster_usage_set[@id="human"]/monster_usage_mountings'>
	<xsl:copy>
	  <xsl:copy-of select="@*" />
	  <xsl:copy-of select="*" />
		
	  <monster_usage_mounting mount_id="warg"
						is_mounted="False"
						is_fast="False"
						direction="left"
						action="act_mount_horse_from_left" />
	  <monster_usage_mounting mount_id="warg"
						is_mounted="False"
						is_fast="True"
						direction="left"
						action="act_mount_horse_fast_from_left" />
	  <monster_usage_mounting mount_id="warg"
						is_mounted="False"
						is_fast="False"
						direction="right"
						action="act_mount_horse_from_right" />
	  <monster_usage_mounting mount_id="warg"
						is_mounted="False"
						is_fast="True"
						direction="right"
						action="act_mount_horse_fast_from_right" />
	  <monster_usage_mounting mount_id="warg"
						is_mounted="True"
						is_fast="False"
						direction="left"
						action="act_dismount_horse_to_left" />
	  <monster_usage_mounting mount_id="warg"
						is_mounted="True"
						is_fast="False"
						direction="right"
						action="act_dismount_horse_to_right" />
		
	</xsl:copy>
  </xsl:template>

  <xsl:template match='monster_usage_set[@id="human"]/monster_usage_strikes'>
	<xsl:copy>
	  <xsl:copy-of select="@*" />
	  <xsl:copy-of select="*" />
	
	  <monster_usage_strike mount_id="warg"
							is_heavy="False"
							is_left_stance="False"
							direction="front_right"
							body_part="chest"
							impact="5"
							action="act_rider_only_fall_right_front" />
	  <monster_usage_strike mount_id="warg"
							is_heavy="False"
							is_left_stance="False"
							direction="front_left"
							body_part="chest"
							impact="5"
							action="act_rider_only_fall_left_front" />
	  <monster_usage_strike mount_id="warg"
							is_heavy="False"
							is_left_stance="False"
							direction="back_right"
							body_part="chest"
							impact="5"
							action="act_rider_only_fall_right_back" />
	  <monster_usage_strike mount_id="warg"
							is_heavy="False"
							is_left_stance="False"
							direction="back_left"
							body_part="chest"
							impact="5"
							action="act_rider_only_fall_left_back" />
	  <monster_usage_strike mount_id="warg"
							is_heavy="True"
							is_left_stance="False"
							direction="front_right"
							body_part="chest"
							impact="5"
							action="act_rider_only_fall_right_front_heavy" />
	  <monster_usage_strike mount_id="warg"
							is_heavy="True"
							is_left_stance="False"
							direction="front_left"
							body_part="chest"
							impact="5"
							action="act_rider_only_fall_left_front_heavy" />
	  <monster_usage_strike mount_id="warg"
							is_heavy="True"
							is_left_stance="False"
							direction="back_right"
							body_part="chest"
							impact="5"
							action="act_rider_only_fall_right_back_heavy" />
	  <monster_usage_strike mount_id="warg"
							is_heavy="True"
							is_left_stance="False"
							direction="back_left"
							body_part="chest"
							impact="5"
							action="act_rider_only_fall_left_back_heavy" />
		
	</xsl:copy>
  </xsl:template>

	<xsl:template match='monster_usage_set[@id="human"]/monster_usage_falls'>
		<xsl:copy>
			<xsl:copy-of select="@*" />
			<xsl:copy-of select="*" />

			<monster_usage_fall mount_id="warg"
								is_heavy="False"
								is_left_stance="False"
								direction="right"
								body_part="none"
								death_type="other"
								action="act_fall_rider_forward_left" />
			<monster_usage_fall mount_id="warg"
								is_heavy="False"
								is_left_stance="False"
								direction="left"
								body_part="none"
								death_type="other"
								action="act_fall_rider_forward_right" />
			<monster_usage_fall mount_id="warg"
								is_heavy="True"
								is_left_stance="False"
								direction="right"
								body_part="none"
								death_type="other"
								action="act_fall_rider_forward_left" />
			<monster_usage_fall mount_id="warg"
								is_heavy="True"
								is_left_stance="False"
								direction="left"
								body_part="none"
								death_type="other"
								action="act_fall_rider_forward_right" />
			<monster_usage_fall mount_id="warg"
								is_heavy="True"
								is_left_stance="False"
								direction="front"
								body_part="none"
								death_type="other"
								action="act_fall_rider_back" />
			<monster_usage_fall mount_id="warg"
								is_heavy="False"
								is_left_stance="False"
								direction="front"
								body_part="none"
								death_type="other"
								action="act_fall_rider_back" />
			<monster_usage_fall mount_id="warg"
								is_heavy="False"
								is_left_stance="False"
								direction="back"
								body_part="none"
								death_type="other"
								action="act_fall_rider_forward_right_slow" />
			<monster_usage_fall mount_id="warg"
								is_heavy="True"
								is_left_stance="False"
								direction="back"
								body_part="none"
								death_type="other"
								action="act_fall_rider_forward_right_slow" />
			
		</xsl:copy>
	</xsl:template>

</xsl:stylesheet>