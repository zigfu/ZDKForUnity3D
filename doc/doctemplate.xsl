<?xml version="1.0"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output 
    encoding="UTF-8"
    indent="yes"
    method="xml"
    omit-xml-declaration="yes" 
  />

  <xsl:template match="Page">
    <html>
      <head>
        <title>
          <xsl:value-of select="Title" />
        </title>
        <meta http-equiv="Content-Type" content="text/html; charset=UTF-8" />
        <xsl:call-template name="create-default-style" />
        <xsl:call-template name="create-default-script" />
      </head>
      <body onload="onBodyLoad()">
        <!-- HEADER -->
        <!--<xsl:call-template name="create-default-collection-title" />-->
        <!--<xsl:call-template name="create-index" />-->
        <xsl:call-template name="create-default-title" />
        <xsl:call-template name="create-default-summary" />
        <xsl:call-template name="create-default-signature" />
        <xsl:call-template name="create-default-remarks" />
        <!--<xsl:call-template name="create-default-members" />-->
        <hr size="1" />
        <xsl:call-template name="create-default-copyright" />
      </body>
    </html>
  </xsl:template>

  <!-- IDENTITY TRANSFORMATION -->
  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()" />
    </xsl:copy>
  </xsl:template>

  <xsl:template name="create-default-style">
    <link rel="stylesheet" type="text/css" href="../docstyle.css" />
  </xsl:template>

  <xsl:template name="create-default-script">
    <script type="text/JavaScript">
	function removeElementsByClassName(name)
	{
		var children = document.getElementsByClassName(name);
		for (var childIter in children)
		{
			var child = children[childIter];
			var parent = child.parentNode;
			if (parent !== undefined) parent.removeChild(child);
		}
	}
	function removeRequirements()
	{
		var remarks = document.getElementsByClassName('Remarks')[0];
		if (remarks.children[2].className == "TypesListing")
			return;
		remarks.removeChild(remarks.children[2]); //requirements
		remarks.removeChild(remarks.children[2]); //namespaces
	}
    function toggle_display (block) 
	{
        var w = document.getElementById (block);
        var t = document.getElementById (block + ":toggle");
        if (w.style.display == "none") 
		{
			w.style.display = "block";
			t.innerHTML = "⊟";
        } 
		else 
		{
			w.style.display = "none";
			t.innerHTML = "⊞";
        }
    }
	function onBodyLoad()
	{
		removeElementsByClassName('Members');
		removeRequirements();
	}
    </script>
  </xsl:template>

  <xsl:template name="create-index">
  </xsl:template>

  <xsl:template name="create-default-collection-title">
    <div class="CollectionTitle">
      <xsl:apply-templates select="CollectionTitle/node()" />
    </div>
  </xsl:template>

  <xsl:template name="create-default-title">
    <h1 class="PageTitle">
      <xsl:if test="count(PageTitle/@id) &gt; 0">
        <xsl:attribute name="id">
          <xsl:value-of select="PageTitle/@id" />
        </xsl:attribute>
      </xsl:if>
      <xsl:apply-templates select="PageTitle/node()" />
    </h1>
  </xsl:template>

  <xsl:template name="create-default-summary">
    <p class="Summary">
      <xsl:if test="count(Summary/@id) &gt; 0">
        <xsl:attribute name="id">
          <xsl:value-of select="Summary/@id" />
        </xsl:attribute>
      </xsl:if>
      <xsl:apply-templates select="Summary/node()" />
    </p>
  </xsl:template>

  <xsl:template name="create-default-signature">
    <div>
      <xsl:if test="count(Signature/@id) &gt; 0">
        <xsl:attribute name="id">
          <xsl:value-of select="Signature/@id" />
        </xsl:attribute>
      </xsl:if>
      <xsl:apply-templates select="Signature/node()" />
    </div>
  </xsl:template>

  <xsl:template name="create-default-remarks">
    <div class="Remarks">
      <xsl:if test="count(Remarks/@id) &gt; 0">
        <xsl:attribute name="id">
          <xsl:value-of select="Remarks/@id" />
        </xsl:attribute>
      </xsl:if>
      <xsl:apply-templates select="Remarks/node()" />
    </div>
  </xsl:template>

  <xsl:template name="create-default-members">
    <div class="Members">
      <xsl:if test="count(Members/@id) &gt; 0">
        <xsl:attribute name="id">
          <xsl:value-of select="Members/@id" />
        </xsl:attribute>
      </xsl:if>
      <xsl:apply-templates select="Members/node()" />
    </div>
  </xsl:template>

  <xsl:template name="create-default-copyright">
    <div class="Copyright">
      <xsl:apply-templates select="Copyright/node()" />
    </div>
  </xsl:template>
</xsl:stylesheet>
