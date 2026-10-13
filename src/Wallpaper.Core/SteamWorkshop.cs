using System.Globalization;

namespace Wallpaper.Core;

public static class SteamWorkshop
{
    public static string? CanonicalUrl(string? value)
    {
        if (value is null || value.Length>4096 || !Uri.TryCreate(value,UriKind.Absolute,out var uri) ||
            uri.Scheme!="https" || !uri.Host.Equals("steamcommunity.com",StringComparison.OrdinalIgnoreCase) ||
            !uri.IsDefaultPort || uri.UserInfo.Length!=0) return null;
        if (uri.AbsolutePath.TrimEnd('/') is not ("/sharedfiles/filedetails" or "/workshop/filedetails")) return null;
        string? id=null;
        foreach(var pair in uri.Query.TrimStart('?').Split('&',StringSplitOptions.RemoveEmptyEntries))
        {
            var parts=pair.Split('=',2); var name=Uri.UnescapeDataString(parts[0]);
            var item=parts.Length==2?Uri.UnescapeDataString(parts[1]):"";
            if(name=="appid" && item!="431960") return null;
            if(name=="id") { if(id is not null) return null; id=item; }
        }
        if (id is null || !ulong.TryParse(id,NumberStyles.None,CultureInfo.InvariantCulture,out var number) || number==0) return null;
        return "https://steamcommunity.com/sharedfiles/filedetails/?id="+number.ToString(CultureInfo.InvariantCulture);
    }
}
