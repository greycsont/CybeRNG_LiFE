using System;

namespace CybeRNG_LiFE.Util;

public static class TimeUtil
{
    public static string GetLocalTimeWithUtcOffset(string format = "yyyy-MM-dd HH:mm:ss zzz")
    {
        DateTimeOffset now = DateTimeOffset.Now;
        return now.ToString(format);
    }
}