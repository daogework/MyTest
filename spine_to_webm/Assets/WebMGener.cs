using Spine.Unity;
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

[InitializeOnLoad]
[CustomEditor(typeof(WebMGener))]
public class WebMGenerInspector : Editor
{
    SerializedProperty startingAnimation;
    WebMGener webMGener;
    private void OnEnable()
    {
        webMGener = target as WebMGener;
        webMGener.skeletonDataAsset = webMGener.skeletonGraphic.skeletonDataAsset;
        var so = this.serializedObject;
        startingAnimation = so.FindProperty("startingAnimation");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        //EditorGUILayout.PropertyField(startingAnimation);
    }
}

public class WebMGener : MonoBehaviour
{
    public int FrameRate = 24;
    public int maxSize = 250;
    [SpineAnimation(dataField: "skeletonDataAsset")]
    public string stateName = "";
    public SkeletonGraphic skeletonGraphic;

    public SkeletonDataAsset skeletonDataAsset;

    //[SpineAnimation(dataField: "skeletonDataAsset")]
    //public string startingAnimation;




    float getClipLength(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError($"animState name is null or empty on {gameObject.name}");
            return 0;
        }
        if (gameObject.activeSelf == false)
            gameObject.SetActive(true);
        var anim = skeletonGraphic.AnimationState.Data.SkeletonData.FindAnimation(name);
        if (anim != null)
            return anim.Duration;
        else
            Debug.LogError($"animation {name} not found on {gameObject.name}");
        return 0;
    }

    float play(string name)
    {
        var duration = getClipLength(name);
        if (duration > 0)
        {
            skeletonGraphic.AnimationState.SetAnimation(0, name, false);
        }
        return duration;
    }



    // Start is called before the first frame update
    IEnumerator Start()
    {
        Debug.Log($"stateName:{stateName}");
        var len = play(stateName);

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var TestRecorderController = new RecorderController(controllerSettings);

        var videoRecorder = ScriptableObject.CreateInstance<ImageRecorderSettings>();
        videoRecorder.name = "My Video Recorder";
        videoRecorder.Enabled = true;

        Debug.Log($"{Screen.width}X{Screen.height}");

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
        videoRecorder.OutputFile = new DirectoryInfo(Application.dataPath).Parent.ToString() + "/records/image_<Frame>";

        controllerSettings.AddRecorderSettings(videoRecorder);
        controllerSettings.FrameRate = FrameRate;
        controllerSettings.SetRecordModeToTimeInterval(0, len>2.9f?2.9f:len); // 2.9s @ 30 FPS
        

        RecorderOptions.VerboseMode = false;
        TestRecorderController.PrepareRecording();
        TestRecorderController.StartRecording();


        yield return new WaitForSeconds(len + 0.1f);
        Run(@"E:\ffmpeg-master-latest-win64-lgpl\bin\ffmpeg.exe",
            $"-y -i \"{dir}/image_%04d.png\" -an -to 2.95 -pix_fmt yuva420p -vf \"scale = w =if (gt(iw\\, ih)\\,512\\,-2):h =if (gt(iw\\, ih)\\,-2\\,512):flags = spline\" -c:v vp9 -b:v 623K -bufsize 500K -row-mt 1 -auto-alt-ref 1 -f webm \"{dir}/{stateName}.webm\" -fs {maxSize}K", true);
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
        start.Arguments = Arguments;
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
