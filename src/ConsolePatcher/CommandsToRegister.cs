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
            Leaf("seed", () => Log.Info($"{RandomManager.seed}")),
            Leaf("testmode", () => Log.Info($"{RandomManager.testMode}")),
            Leaf("fixedseed", () => Log.Info($"{RandomManager.fixedSeed}"))
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
        Log.Info("    └─ testmode                 Get testmode status");
        Log.Info("    └─ fixedseed                Get fixedseed status");

        Log.Info("  set");
        Log.Info("    └─ seed <int>               Set global seed");
        Log.Info("    └─ testmode <bool>          Enable testmode (using a const seed /114514/)");
        Log.Info("    └─ fixedseed <bool>         Seed will not change after restart the cybergrind");

        Log.Info("  help                          List all commands");
        Log.Info("  reference                     List all references ");

        Log.Info("Examples:");
        Log.Info("  cybernglife get seed");
        Log.Info("  cybernglife set seed 1114");
    }

    private void ListReference()
    {
        Log.Info("10_days_till_xmas. (2025) cyberseedsetter, Available at: https://github.com/10-days-till-xmas/CyberSeedSetter (Accessed: 28 Dec 2025).");
        Log.Info("M.E. O'Neill. (2018) PCG, A Family of Better Random Number Generators, Available at: https://www.pcg-random.org/ (Accessed: 21 Dec 2025).");
        Log.Info("David Blackman and Sebastiano Vigna. (2018) xoshiro128starstar.c, Available at: https://xoshiro.di.unimi.it/xoshiro128starstar.c (Accessed: 19 Dec 2025)");
        Log.Info("Docs.rs. (IDK) Struct Xoshiro128StarStar, Available at: https://docs.rs/xoshiro/latest/xoshiro/struct.Xoshiro128StarStar.html (Accessed: 19 Dec 2025)");
        Log.Info("Wikipedia. (2025) Xorshift, Available at: https://en.wikipedia.org/wiki/Xorshift");
        Log.Info("Wikipedia. (2025) Permuted congruential generator, Available at: https://en.wikipedia.org/wiki/Permuted_congruential_generator (Accessed: 21 Dec 2025).");
    }

    public Logger Log { get; } = new("cybernglife");
}