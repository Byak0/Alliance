<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output omit-xml-declaration="yes" />
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()" />
        </xsl:copy>
    </xsl:template>

  
    <xsl:template match='sound_and_collision_info_class_definitions'>
        <xsl:copy>
            <xsl:copy-of select="@*" />
            <xsl:copy-of select="*" />

			<sound_and_collision_info_class_definition name="warg" />
			
        </xsl:copy>
    </xsl:template>

</xsl:stylesheet>