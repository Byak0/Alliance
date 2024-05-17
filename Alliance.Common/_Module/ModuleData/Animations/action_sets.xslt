<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output omit-xml-declaration="yes" />
	<xsl:template match="@*|node()">
		<xsl:copy>
			<xsl:apply-templates select="@*|node()" />
		</xsl:copy>
	</xsl:template>


	<xsl:template match='action_set[@id="as_human_warrior"]'>
		<xsl:copy>
			<xsl:copy-of select="@*" />
			<xsl:copy-of select="*" />

			<action type="act_horse_rear" animation="rider_forward_gallop_right_foot" />
			<action type="act_horse_rear_damaged" animation="rider_forward_gallop_right_foot" />

		</xsl:copy>
	</xsl:template>

</xsl:stylesheet>