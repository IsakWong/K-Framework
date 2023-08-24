using System.Text;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 支持Log到File的一个Log单例
/// </summary>
public class KLog : KSingleton<KLog>
{
    StringBuilder tempBuilder = new StringBuilder();
    StreamWriter logFile = null;

    public KLog()
    {
        var logFilePath = Path.Combine(Application.dataPath, "../log.txt");
        var fileStream = File.OpenWrite(logFilePath);
        try
        {
            logFile = new StreamWriter(fileStream, Encoding.UTF8);
            logFile.AutoFlush = true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    /// <summary>
    /// color 转换hex
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public static string ColorToHex(Color color)
    {
        int r = Mathf.RoundToInt(color.r * 255.0f);
        int g = Mathf.RoundToInt(color.g * 255.0f);
        int b = Mathf.RoundToInt(color.b * 255.0f);
        int a = Mathf.RoundToInt(color.a * 255.0f);
        string hex = $"{r:X2}{g:X2}{b:X2}{a:X2}";
        return hex;
    }

    /// <summary>
    /// hex转换到color
    /// </summary>
    /// <param name="hex"></param>
    /// <returns></returns>
    public static Color HexToColor(string hex)
    {
        byte br = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte bg = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte bb = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        byte cc = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
        float r = br / 255f;
        float g = bg / 255f;
        float b = bb / 255f;
        float a = cc / 255f;
        return new Color(r, g, b, a);
    }

    public static void LogError(string message)
    {
        var sb = global::KLog.Instance.tempBuilder;
        var mainFile = global::KLog.Instance.logFile;
        DateTime dt = System.DateTime.Now;
        sb.AppendFormat("[{0}]:{1}", dt.ToString("HH:mm:ss.fff"), message);
        Debug.LogError(sb.ToString());
        if (mainFile != null)
            mainFile.WriteLine(sb.ToString());
        sb.Clear();
    }

    public static void LogErrorFormat(string message)
    {
        var sb = global::KLog.Instance.tempBuilder;
        var mainFile = global::KLog.Instance.logFile;
        DateTime dt = DateTime.Now;
        sb.AppendFormat("[{0}]:{1}", dt.ToString("HH:mm:ss.fff"), message);
        Debug.LogError(sb.ToString());
        sb.Clear();
    }

    public static void Log(string message)
    {
        var sb = global::KLog.Instance.tempBuilder;
        var mainFile = global::KLog.Instance.logFile;
        DateTime dt = DateTime.Now;
        sb.AppendFormat("[{0}]:{1}", dt.ToString("HH:mm:ss.fff"), message);
        Debug.Log(sb.ToString());
        if (mainFile != null)
            mainFile.WriteLine(sb.ToString());
        sb.Clear();
    }

    public static void Log(string message, Color color)
    {
        var sb = global::KLog.Instance.tempBuilder;
        var mainFile = global::KLog.Instance.logFile;
        DateTime dt = DateTime.Now;
        sb.AppendFormat("<color={0}>[{1}]:{2}</color>", ColorToHex(color), dt.ToString(), message);
        Debug.Log(sb.ToString());
        if (mainFile != null)
            mainFile.WriteLine(sb.ToString());
        sb.Clear();
    }
}