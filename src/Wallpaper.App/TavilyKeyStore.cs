using System.Runtime.InteropServices;
using System.Text;
using Wallpaper.Core;

namespace Wallpaper.App;

internal sealed record TavilyKeyStatus(bool Configured,string Source,bool HasSavedKey);

// Generic credentials are encrypted and owned by the current Windows user.
internal sealed class TavilyKeyStore(string target,Func<string?>? environment=null)
{
    private const uint Generic=1;
    private const int NotFound=1168;
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    private struct Credential
    {
        public uint Flags,Type;
        public string? TargetName,Comment;
        public long LastWritten;
        public uint CredentialBlobSize;
        public nint CredentialBlob;
        public uint Persist,AttributeCount;
        public nint Attributes;
        public string? TargetAlias,UserName;
    }
    [DllImport("advapi32.dll",EntryPoint="CredReadW",CharSet=CharSet.Unicode,SetLastError=true)]
    private static extern bool ReadCredential(string name,uint type,uint flags,out nint credential);
    [DllImport("advapi32.dll",EntryPoint="CredWriteW",CharSet=CharSet.Unicode,SetLastError=true)]
    private static extern bool WriteCredential(ref Credential credential,uint flags);
    [DllImport("advapi32.dll",EntryPoint="CredDeleteW",CharSet=CharSet.Unicode,SetLastError=true)]
    private static extern bool DeleteCredential(string name,uint type,uint flags);
    [DllImport("advapi32.dll")] private static extern void CredFree(nint credential);

    internal TavilyKeyStatus Status()
    {
        var saved=ReadSaved();
        return saved is not null?new(true,"saved",true):!string.IsNullOrWhiteSpace(EnvironmentKey())?new(true,"environment",false):new(false,"none",false);
    }
    internal string Read() => ReadSaved()??EnvironmentKey()??"";
    private string? EnvironmentKey() => environment is not null?environment():
        Environment.GetEnvironmentVariable("TAVILY_API_KEY")??Environment.GetEnvironmentVariable("TAVILY_API_KEY",EnvironmentVariableTarget.User);
    private string? ReadSaved()
    {
        if(!ReadCredential(target,Generic,0,out var pointer))
        {
            if(Marshal.GetLastWin32Error()==NotFound)return null;
            throw new InvalidDataException("无法读取 Windows 凭据，请重新打开程序后再试。");
        }
        try
        {
            var credential=Marshal.PtrToStructure<Credential>(pointer);
            if(credential.CredentialBlobSize is 0 or >4096)throw new InvalidDataException("保存的 Tavily 凭据格式不正确，请重新配置。");
            var bytes=new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob,bytes,0,bytes.Length);
            try { return Encoding.UTF8.GetString(bytes); }
            finally { Array.Clear(bytes); }
        }
        finally { CredFree(pointer); }
    }
    internal void Save(string value)
    {
        var key=TavilyWorkshopClient.ValidateKey(value);
        var bytes=Encoding.UTF8.GetBytes(key); var blob=Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes,0,blob,bytes.Length);
            var credential=new Credential { Type=Generic,TargetName=target,CredentialBlobSize=(uint)bytes.Length,CredentialBlob=blob,Persist=2,UserName="Tavily" };
            if(!WriteCredential(ref credential,0))throw new InvalidDataException("无法保存 Windows 凭据，请检查当前用户会话。");
        }
        finally { Array.Clear(bytes); Marshal.Copy(bytes,0,blob,bytes.Length); Marshal.FreeHGlobal(blob); }
    }
    internal void Clear()
    {
        if(!DeleteCredential(target,Generic,0) && Marshal.GetLastWin32Error()!=NotFound)
            throw new InvalidDataException("无法删除 Windows 凭据，请稍后再试。");
    }
}
