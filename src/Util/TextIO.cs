using System;
using System.IO;
using GreyAnnouncer;

namespace CybeRNG_LiFE.Util;

public static class TextIO
{
    public static void WriteTextToFile(string title, string content)
    {
        try
        {
            File.AppendAllText(PathManager.GetCurrentPluginPath(title), content);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning($"Error while write text to file: {ex.Message}");
        }
    }
}