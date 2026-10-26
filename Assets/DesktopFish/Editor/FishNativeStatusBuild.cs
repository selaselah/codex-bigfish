using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DesktopFish.Editor
{
    static class FishNativeStatusBuild
    {
        public static void Ensure()
        {
            string root=Directory.GetParent(Application.dataPath).FullName;
            var start=new ProcessStartInfo("/usr/bin/python3","\""+Path.Combine(root,"Native/FishStatusGlass/build.py")+"\"") {
                WorkingDirectory=root, UseShellExecute=false,
                RedirectStandardOutput=true, RedirectStandardError=true
            };
            using(var process=Process.Start(start))
            {
                var output=process.StandardOutput.ReadToEndAsync();
                var errors=process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                string message=output.Result+errors.Result;
                if(process.ExitCode!=0)throw new InvalidOperationException("Native status glass build failed: "+message);
                UnityEngine.Debug.Log(message);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
