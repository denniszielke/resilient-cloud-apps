using System;
using System.IO;

public static class DotEnv
{
    public static void Load(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        foreach (var line in File.ReadAllLines(filePath))
        {
            var parts = line.Split(new[] { '=' }, 2);

            Environment.SetEnvironmentVariable(parts[0], parts[1].Trim('"'));
        }
    }
}