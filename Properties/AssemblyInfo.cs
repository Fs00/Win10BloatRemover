using System.Reflection;
using System.Runtime.InteropServices;
using Win10BloatRemover;

[assembly: AssemblyTitle("Windows 10 Bloat Remover")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Windows 10 Bloat Remover")]
[assembly: AssemblyCopyright("Developed by Fs00")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Se si imposta ComVisible su false, i tipi in questo assembly non saranno visibili
// ai componenti COM. Se è necessario accedere a un tipo in questo assembly da
// COM, impostare su true l'attributo ComVisible per tale tipo.
[assembly: ComVisible(false)]

// Se il progetto viene esposto a COM, il GUID seguente verrà utilizzato come ID della libreria dei tipi
[assembly: Guid("759f045d-adbf-4867-a0f7-f2d066e0c390")]

// È possibile specificare tutti i valori oppure impostare valori predefiniti per i numeri relativi alla revisione e alla build
// usando l'asterisco '*' come illustrato di seguito:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("3.2." + Program.SUPPORTED_WINDOWS_RELEASE_ID)]
[assembly: AssemblyFileVersion("3.2." + Program.SUPPORTED_WINDOWS_RELEASE_ID)]
