using System;
using System.IO;
using Serilog;
namespace Services.Helpers;

public static class Env
{
    public static string GetOrDie(string key)
    {
        var result = Environment.GetEnvironmentVariable(key);
        return result ?? throw new Exception($"{key} key not in .env!");
    }

    public static bool IsSet(string key)
    {
        var result = Environment.GetEnvironmentVariable(key);

        return result is not null;
    }

    public static void Load(string filePath)
    {
        Log.Debug("[env]: loading .env from: {@filePath}", filePath);
        if (!File.Exists(filePath))
        {
            Log.Fatal("[env]: could not find .env");
            return;
        }

        foreach (var line in File.ReadAllLines(filePath))
        {
            var parts = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                    Log.Warning("[env]: invalid env: {@env}", line);

                continue;
            }

            Log.Verbose("[env]: setting {@env} to {@val}", parts[0], parts[1]);
            Environment.SetEnvironmentVariable(parts[0], parts[1]);
        }
    }

    public static void Find(string currentDir)
    {
        while (!File.Exists(Path.Combine(currentDir, ".env")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
            if (currentDir == null)
            {
                Log.Error("Ошибка: Файл .env не найден.");
                return; 
            }
        }
    }
    
    public static void FindAndLoadEnv(string currentDir)
    {
        Find(currentDir);
        Load(Path.Combine(currentDir, ".env"));
    }
}