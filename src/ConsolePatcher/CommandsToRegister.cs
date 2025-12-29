using GameConsole;
using GameConsole.CommandTree;
using plog;
using CybeRNG_LiFE.RNG;

namespace CybeRNG_LiFE.Commands;

public sealed class CommandsToRegister(Console con) : CommandRoot(con), IConsoleLogger
{

    public override string Name => "cybernglife";
    public override string Description => "a mod for f";

    public override Branch BuildTree(Console con)
    {
        return Branch(Name,
                      GetBranches(),
                      SetBranches(),
                      Leaf("help", () => ListCommands()),
                      Leaf("reference", () => ListReference())
                      );
    }

    private Branch GetBranches()
    {
        return Branch("get",
            Leaf("seed", () => Log.Info($"{RandomManager.seed}"))
        );
    }

    private Branch SetBranches()
    {
        return Branch("set",
            Leaf<int>("seed", seed => RandomManager.seed = seed),
            Leaf<bool>("testmode", testmode => RandomManager.testMode = testmode),
            Leaf<bool>("fixedseed", fixedSeed => RandomManager.fixedSeed = fixedSeed)
        );
    }

    private void ListCommands()
    {
        Log.Info("CybeRNG_LiFE Commands");
        Log.Info("========================");

        Log.Info("Available commands:");

        Log.Info("  get");
        Log.Info("    └─ seed                     Get global seed");

        Log.Info("  set");
        Log.Info("    └─ seed <int>               Set global seed");
        Log.Info("    └─ testmode <bool>          enable testmode (using a const seed)");


        Log.Info("  help                          List all commands");
        Log.Info("  reference                     List all references ");

        Log.Info("Examples:");
        Log.Info("  cybernglife get seed");
        Log.Info("  cybernglife set seed 1114");
    }

    private void ListReference()
    {

    }

    public Logger Log { get; } = new("cybernglife");
}