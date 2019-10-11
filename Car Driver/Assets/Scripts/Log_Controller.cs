using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class Log_Controller : MonoBehaviour
{
    public string path = "NNLogs\\";

    StreamWriter sw;
    string filename;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartLogging()
    {
        filename = string.Concat(DateTime.Now.Day, ".", DateTime.Now.Month, ".", DateTime.Now.Year, " - ", DateTime.Now.Hour, ".", DateTime.Now.Minute, "_NNLog");
        string fn = "NNLogs\\" + filename + ".txt";
        int a = 0;

        if (File.Exists(fn))
        {
            fn += string.Concat("_", a.ToString());
            StartLogging();
            return;
        }

        sw = File.CreateText(fn);
    }

    public void StartLogging(string header)
    {
        StartLogging();

        sw.WriteLine(header);
    }

    public void LogLine(string str)
    {
        sw.WriteLine(str);
    }

    public void EndLogging()
    {
        sw.Close();
    }

    private void OnDestroy()
    {
        EndLogging();
    }
}
