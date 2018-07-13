UNITY_SETUP_INSTANCE_ID(v);

float entityId = UNITY_ACCESS_INSTANCED_PROP(Props, _EntityID);

float key = entityId;
float4 runtimeData = _RandomWrite[key];

float queuedClip = UNITY_ACCESS_INSTANCED_PROP(Props, _CurrentAnimation);
if(_Clip != 0) {
    queuedClip = _Clip;
}
float lastWrapMode = runtimeData.x;
float lastClip     = runtimeData.y;
float offset       = runtimeData.z;

float xSet = (queuedClip + 0.5) * ts.x;
float ySet = 0.1 * ts.y;
float4 settings = tex2Dlod(_PosTex, float4(xSet, ySet, 0, 0));
float clipLength = settings.x;
float rate = settings.y;
float yOffset = settings.z;
float wrapMode = round(settings.w);
float runningFrame = ((_Time.y - offset) * 1/rate * _PlaybackSpeed) % 1.0;
float clamedFrame = clamp((_Time.y - offset) * 1/rate * _PlaybackSpeed, 0.0, 0.9999);
float frame = runningFrame;
float done = 0;
if(lastWrapMode == 1) {
    frame = clamedFrame;
    if(clamedFrame == 0.9999) {
        done = 1;
    }
} else if(lastWrapMode == 8) {
    frame = clamedFrame;
}

if(lastClip != queuedClip) {
    if(lastWrapMode != 1 || (lastWrapMode == 1 && done == 1)) {
        lastWrapMode = wrapMode;
        lastClip = queuedClip;
        offset = _Time.y;
    }
}
runtimeData.x = lastWrapMode;
runtimeData.y = lastClip;
runtimeData.z = offset;
_RandomWrite[key] = runtimeData;

float x = (vid + 0.5) * ts.x;
float y = frame * clipLength * ts.y + ts.y * yOffset;
float4 pos = tex2Dlod(_PosTex, float4(x, y, 0, 0));
float3 normal = tex2Dlod(_NmlTex, float4(x, y, 0, 0));