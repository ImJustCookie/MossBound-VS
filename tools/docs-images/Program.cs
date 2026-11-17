// Renders every circle used in docs/ to out/*.png (plus out/scenes.json with where things landed),
// using the mod's real canvas and panel code through reflection and the system's Cairo. The Python
// scripts then add titles and labels. Run through run.sh, from this folder.
using Cairo;
using Mossbound.Script;

System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(typeof(Context).Assembly, (name, _, _) =>
    name.Contains("cairo") && System.Runtime.InteropServices.NativeLibrary.TryLoad("libcairo.so.2", out var lib) ? lib : IntPtr.Zero);

Directory.CreateDirectory("out");
Scenes.Run(new WorldScript(12345), "../../mossbound/assets/mossbound/textures/gui/");
Console.WriteLine("rendered out/");
