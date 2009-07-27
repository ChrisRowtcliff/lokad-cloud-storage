﻿<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="NavBar.ascx.cs" Inherits="Lokad.Cloud.Web.NavBar" %>
<ul>
	<li id="Home" runat="server"><asp:HyperLink runat="server" NavigateUrl="~/Home.aspx" Text="Home" /></li>
	<li id="Assemblies" runat="server"><asp:HyperLink runat="server" NavigateUrl="~/Assemblies.aspx" Text="Assemblies" /></li>
	<li id="Logs" runat="server"><asp:HyperLink runat="server" NavigateUrl="~/Logs.aspx" Text="Error Logs" /></li>
	<li id="Services" runat="server"><asp:HyperLink runat="server" NavigateUrl="~/Services.aspx" Text="Services" /></li>
	<li id="Scheduler" runat="server"><asp:HyperLink runat="server" NavigateUrl="~/Scheduler.aspx" Text="Scheduler" /></li>
	<li id="Workloads" runat="server"><asp:HyperLink runat="server" NavigateUrl="~/Workloads.aspx" Text="Queues" /></li>
</ul>