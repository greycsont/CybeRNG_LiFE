using GameConsole;
using GameConsole.CommandTree;
using plog;
using System.Linq;

namespace CybeRNG_LiFE.Commands;

public sealed class CommandsToRegister(Console con) : CommandRoot(con), IConsoleLogger
{

    public override string Name => "abcde";
    public override string Description => "tons of setting";

    public override Branch BuildTree(Console con)
    {
        return Branch(Name,
                      GetBranches(),
                      SetBranches(),
                      Leaf("help", () => ListCommands())
                      );
    }

    private Branch GetBranches()
    {
        return Branch("get",
            Leaf("seed", () => Log.Info($"{EndlessGridPatch.seed}"))
        );
    }

    private Branch SetBranches()
    {
        return Branch("set",
            Leaf<int>("seed", seed => EndlessGridPatch.seed = seed)
        );
    }

    private void ListCommands()
    {
        Log.Info("GreyCGSEED RNG Commands");
        Log.Info("========================");
        
        Log.Info("Available commands:");
        
        Log.Info("  get");
        Log.Info("    ├─ seed                     Get global seed");
        Log.Info("    └─ references               List algorithm references");
        
        Log.Info("  set");
        Log.Info("    └─ seed <int>               Set global seed");
        
        Log.Info("  help                          Show this help");
        Log.Info("");
        
        Log.Info("Examples:");
        Log.Info("  rng get seed");
        Log.Info("  rng set seed 123456");
    }

    public Logger Log { get; } = new("cybernglife");
}