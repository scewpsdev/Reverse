using Rainfall;


public class EntryPoint : Game3D<EntryPoint>
{
	const string PROJECT_PATH = "D:\\Dev\\2025\\Reverse\\Reverse"; // TODO fill in

	const int VERSION_MAJOR = 0;
	const int VERSION_MINOR = 0;
	const int VERSION_PATCH = 1;
	const char VERSION_SUFFIX = 'a';

#if DISTRIBUTION_BUILD
	const bool LOAD_PACKAGES = true;
#else
	const bool LOAD_PACKAGES = false;
#endif


	public EntryPoint()
		: base(VERSION_MAJOR, VERSION_MINOR, VERSION_PATCH, VERSION_SUFFIX, LOAD_PACKAGES)
	{
	}

	public override void init()
	{
		base.init();

		pushState(new GameState());
	}

	public static void Main(string[] args)
	{
		LaunchParams launchParams = new LaunchParams(args);
#if DISTRIBUTION_BUILD
		launchParams.width = 1280;
		launchParams.height = 720;
		launchParams.fpsCap = 60;
		launchParams.fullscreen = true;
#else
		launchParams.width = 1280;
		launchParams.height = 720;
		launchParams.fpsCap = 0;
		//launchParams.fullscreen = true;
#endif

		EntryPoint game = new EntryPoint();

#if !DISTRIBUTION_BUILD
#if DEBUG
		string config = "Debug";
#else
		string config = "Release";
#endif
		//Utils.RunCommand("xcopy", $"/y \"{PROJECT_PATH}\\lib\\Rainfall\\{config}\\RainfallNative.dll\" \"{PROJECT_PATH}\\bin\\{config}\\net8.0\"");
		Utils.RunCommand("xcopy", $"/y \"D:\\Dev\\Rainfall\\RainfallNative\\bin\\x64\\{config}\\RainfallNative.dll\" \"{PROJECT_PATH}\\bin\\{config}\\net8.0\"");
		//int exitCode = game.compileResources(PROJECT_PATH, PROJECT_PATH + $"\\bin\\{config}\\net8.0\\", "lib\\Rainfall\\ResourceCompiler\\RainfallResourceCompiler.exe");
		int exitCode = game.compileResources(PROJECT_PATH, PROJECT_PATH + $"\\bin\\{config}\\net8.0\\", "D:\\Dev\\Rainfall\\RainfallResourceCompiler\\bin\\x64\\Debug\\RainfallResourceCompiler.exe");
		if (exitCode != 0)
			Debug.Error("Resource compilation exited with code " + exitCode);
		game.compileResources("D:\\Dev\\Rainfall\\RainfallNative", PROJECT_PATH + $"\\bin\\{config}\\net8.0\\", "D:\\Dev\\Rainfall\\RainfallResourceCompiler\\bin\\x64\\Debug\\RainfallResourceCompiler.exe");
#endif

		game.run(launchParams);
	}
}
