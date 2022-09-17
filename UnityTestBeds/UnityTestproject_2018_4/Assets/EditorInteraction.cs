using UnityEditor;
using UnityEngine;
using NLSPlain;
using System.Linq;


/*
    this needs Unity 2018 for UnityEditor.SceneView 
*/



[InitializeOnLoad]    // kommt aus UnityEditor 
class EditorInteraction{
    static EditorInteraction() {
        // init on load 

        "static initializer via [InitializeOnLoad] meta prop".NLSend(); 
        // the ShellServer initializes itself, also with a static constructor, trigger dll-load via access 

        ShellServer.ding = true ; 
        EditorApplication.update = Update;
    }

    
    static void Update() {
        // "update".NLSend() ;
        if( ShellServer.EvalAll() ) {
            if(!Application.isPlaying  ) { 
                
                UnityEditor.SceneView.RepaintAll();  // during playmode scene Update happens regardless 
                                                     // unsurprisingly, this updates only the SceneViews - not, for example, inspectors 
                                                     // 

                // UnityEditor.EditorApplication.QueuePlayerLoopUpdate(); // nor does this 

                // fuck it - for now just live with inspectors not properly updating until the window gets to the foreground 
                
            }
    
        }
    }
}



