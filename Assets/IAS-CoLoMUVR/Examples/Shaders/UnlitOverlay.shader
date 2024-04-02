Shader "MyShaders/Unlit Overlay" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
	_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
}

SubShader {
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
    LOD 100
    Fog {Mode Off}

    ZTest Always
    Blend SrcAlpha OneMinusSrcAlpha
    Color [_Color]

    Pass {
	SetTexture[_MainTex]{
	constantColor[_Color]
	Combine texture * constant, texture * constant
} }
}
}
