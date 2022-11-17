using UnityEngine;

public static class DummySceneGraph1 { 
    public static GameObject[] roots;
    static DummySceneGraph1 ()  { 
        var go = new GameObject() ;
        go.name = "Cube_1" ; 

        // hehe :) -- totally fell for this. The Component Constructors ( that do not exist in the original ) are not 
        // intended to be used directly, they do not create the neccesary links in the (mock)GameObject instance 
        // var mfil = new MeshFilter(go) ;    <------ do not do this! 

        go.AddComponent<MeshFilter>();
        var mfil = go.GetComponent<MeshFilter>();

        var mesh = new Mesh() ;
        mfil.mesh = mesh ; 

        roots = new GameObject[] { go } ;
    }
}