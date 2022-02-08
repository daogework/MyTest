using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class test : MonoBehaviour
{
    public int maxSize = 300;
    public string stateName = "";
    public Animator animator;

    float getClipLength(string name)
    {
        var ac = animator.runtimeAnimatorController as AnimatorController;
        var sm = ac.layers[0].stateMachine;
        for (int i = 0; i < sm.states.Length; i++)
        {
            var state = sm.states[i].state;
            if (state.name == name)
            {
                AnimationClip clip = state.motion as AnimationClip;
                if (clip != null)
                {
                    return clip.length;
                }
            }
        }
        return 0;
    }

    float play(string name)
    {
        animator.Play(name);
        return getClipLength(name);
    }

    // Start is called before the first frame update
    IEnumerator Start()
    {
        var len = play(stateName);

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var TestRecorderController = new RecorderController(controllerSettings);

        var videoRecorder = ScriptableObject.CreateInstance<ImageRecorderSettings>();
        videoRecorder.name = "My Video Recorder";
        videoRecorder.Enabled = true;

        videoRecorder.imageInputSettings = new CameraInputSettings
        {
            OutputWidth = Screen.width,
            OutputHeight = Screen.height,
            RecordTransparency = true,
            CameraTag = "MainCamera",
        };

        videoRecorder.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
        videoRecorder.CaptureAlpha = true;
        var dir = new DirectoryInfo(Application.dataPath).Parent.ToString() + "/records";
        Debug.Log(dir);
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, true);
        }
        videoRecorder.OutputFile = new DirectoryInfo(Application.dataPath).Parent.ToString()+ "/records/image_<Frame>";

        controllerSettings.AddRecorderSettings(videoRecorder);
        controllerSettings.SetRecordModeToTimeInterval(0, len); // 2.9s @ 30 FPS
        controllerSettings.FrameRate = 24;

        RecorderOptions.VerboseMode = false;
        TestRecorderController.PrepareRecording();
        TestRecorderController.StartRecording();


        yield return new WaitForSeconds(len+0.1f);
        Run(@"E:\ffmpeg-master-latest-win64-lgpl\bin\ffmpeg.exe", 
            $"-y -i \"{dir}/image_%04d.png\" -an -to 2.95 -pix_fmt yuva420p -vf \"scale = w =if (gt(iw\\, ih)\\,512\\,-2):h =if (gt(iw\\, ih)\\,-2\\,512):flags = spline\" -c:v vp9 -b:v 623K -minrate 500K -maxrate 1000K -bufsize 250K -row-mt 1 -auto-alt-ref 1 -f webm \"{dir}/{stateName}.webm\" -fs {maxSize}K",true);
        //TestRecorderController.StopRecording();
        //UnityEditor.stop
        //Debug.DebugBreak();
        EditorApplication.isPlaying = false;
    }

    public static void Run(string exe, string Arguments = "", bool WaitForExit = false)
    {
        //Console.InputEncoding = Encoding.UTF8;
        Debug.Log("RunScript Arguments= " + Arguments);
        Process process = new Process();
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = exe;
        start.Arguments =  Arguments;
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        start.RedirectStandardInput = true;
        start.RedirectStandardError = true;
        start.CreateNoWindow = true;
        //start.StandardOutputEncoding = Encoding.GetEncoding("GBK");
        //start.StandardErrorEncoding = Encoding.GetEncoding("GBK");
        process.StartInfo = start;
        process.EnableRaisingEvents = true;
        process.Exited += (s, e) => { Debug.Log("exe Exited."); };
        process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Debug.Log("out info:" + e.Data); };
        process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Debug.LogError("out info:" + e.Data); };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        if (WaitForExit)
            process.WaitForExit();
    }
}
