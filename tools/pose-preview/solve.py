"""Finds the ItemAnchor rotation that keeps the hand X axis vertical for each arm pose (see patches/player-cast-animation.json).
Usage: python3 -I solve.py arms.json  (arms.json: {name: {UpperArmR: {rotationX..}, LowerArmR: {..}}}); writes pose-<name>.json."""
import json, math, sys, numpy as np, os
VS=os.environ["VINTAGE_STORY"]
S=json.load(open(f"{VS}/assets/game/shapes/entity/humanoid/seraph-faceless.json"))
def rot(ax,d):
    a=math.radians(d);c,s=math.cos(a),math.sin(a);m=np.eye(4)
    if ax=="x":m[1:3,1:3]=[[c,-s],[s,c]]
    if ax=="y":m[0,0],m[0,2],m[2,0],m[2,2]=c,s,-s,c
    if ax=="z":m[0:2,0:2]=[[c,-s],[s,c]]
    return m
def tr(v):m=np.eye(4);m[:3,3]=v;return m
def am(pose):
    res={}
    def w(e,p):
        q=pose.get(e["name"],{});o=np.array(e.get("rotationOrigin",[0,0,0]))/16
        m=p@tr(o)@rot("x",e.get("rotationX",0)+q.get("rotationX",0))@rot("y",e.get("rotationY",0)+q.get("rotationY",0))@rot("z",e.get("rotationZ",0)+q.get("rotationZ",0))@tr(np.array(e["from"])/16-o)
        res[e["name"]]=m
        for c in e.get("children") or []: w(c,m)
    for e in S["elements"]: w(e,np.eye(4))
    return res
def solve(arm):
    best=None
    for rx in range(-90,91,1):
        for rz in range(-90,91,1):
            p=dict(arm); p["ItemAnchor"]={"rotationX":rx,"rotationY":0,"rotationZ":rz}
            x=am(p)["ItemAnchor"][:3,0]; err=np.linalg.norm(x-[0,-1,0])+0.0004*(abs(rx)+abs(rz))
            if best is None or err<best[0]: best=(err,rx,rz)
    return best
for name,arm in json.load(open(sys.argv[1])).items():
    e,rx,rz=solve(arm); arm["ItemAnchor"]={"rotationX":rx,"rotationY":0,"rotationZ":rz}
    m=am(arm); print(name, "anchor rx,rz", rx, rz, "err", round(e,3), "hand pos", np.round(m["ItemAnchor"][:3,3],3).tolist())
    json.dump(arm,open(f"pose-{name}.json","w"))
