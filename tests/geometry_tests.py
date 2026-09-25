"""Offline asset geometry checks. Requires Python + numpy, no game process.
Tests the real B3D coordinates and proposed collider envelope, not Unity physics.
"""
import pathlib, struct
import numpy as np

BASE = pathlib.Path(__file__).resolve().parents[1] / 'release/Mods/BurebistaFishingShelter/iceland'
def load(path):
    data=path.read_bytes(); meshes=[]; triangles=[]
    def chunks(start,end,parent=np.eye(4)):
        p=start
        while p<end:
            tag=data[p:p+4].decode(); size=struct.unpack_from('<i',data,p+4)[0]; a=p+8; b=a+size
            if tag=='BB3D': chunks(a+4,b,parent)
            elif tag=='NODE':
                nul=data.index(0,a)
                x,y,z,sx,sy,sz,w,qx,qy,qz=struct.unpack_from('<10f',data,nul+1); qx,qy=-qx,-qy
                rot=np.array([[1-2*(qy*qy+qz*qz),2*(qx*qy-qz*w),2*(qx*qz+qy*w)],[2*(qx*qy+qz*w),1-2*(qx*qx+qz*qz),2*(qy*qz-qx*w)],[2*(qx*qz-qy*w),2*(qy*qz+qx*w),1-2*(qx*qx+qy*qy)]])
                m=np.eye(4);m[:3,:3]=rot@np.diag([sx,sy,sz]);m[:3,3]=[x,y,-z]
                chunks(nul+41,b,parent@m)
            elif tag=='MESH': chunks(a+4,b,parent)
            elif tag=='VRTS':
                flags,sets,ss=struct.unpack_from('<3i',data,a)
                stride=3+(3 if flags&1 else 0)+(4 if flags&2 else 0)+sets*ss
                vs=np.array([struct.unpack_from('<3f',data,o) for o in range(a+12,b,stride*4)]); vs[:,2]*=-1
                meshes.append((parent@np.column_stack([vs,np.ones(len(vs))]).T).T[:,:3])
            elif tag=='TRIS':
                ids=np.array(struct.unpack_from('<'+'i'*((b-a-4)//4),data,a+4)).reshape(-1,3)[:,[0,2,1]]
                triangles.extend(meshes[-1][ids])
            p=b
    chunks(0,len(data))
    return np.array(triangles)

def thickened(tris):
    result=[]
    ids=np.array([0,1,2,5,4,3,0,3,4,0,4,1,1,4,5,1,5,2,2,5,3,2,3,0]).reshape(-1,3)
    for a,b,c in tris:
        n=np.cross(b-a,c-a); length=np.linalg.norm(n)
        if length<1e-3: continue
        n=n/length*.35
        prism=np.array([a+n,b+n,c+n,a-n,b-n,c-n])
        result.extend(prism[ids])
    return np.array(result)

def hits(ts,o,d,distance):
    o=np.array(o,dtype=float); d=np.array(d,dtype=float);d/=np.linalg.norm(d)
    a=ts[:,0]; e1=ts[:,1]-a;e2=ts[:,2]-a
    h=np.cross(d,e2);det=np.sum(e1*h,axis=1);valid=np.abs(det)>1e-8
    inv=np.zeros_like(det);inv[valid]=1/det[valid]
    s=o-a;u=np.sum(s*h,axis=1)*inv;q=np.cross(s,e1);v=np.sum(d*q,axis=1)*inv;t=np.sum(e2*q,axis=1)*inv
    return np.any(valid & (u>=0) & (v>=0) & (u+v<=1) & (t>1e-5) & (t<distance))

checks=0
for name in ['iglu.b3d','iglu_2.b3d']:
    original=load(BASE/name)
    ts=thickened(original)
    for x in [-3,0,3]:
        for y in [0,5,9]:
            assert not hits(ts,[x,y,40],[0,0,-1],40), (name,'blocked entrance',x,y)
            checks+=1
    # Both sides of the walls/roof block rays, unlike a one-sided collision mesh.
    for direction in [[1,0,0],[-1,0,0],[0,0,-1],[0,1,0]]:
        assert hits(ts,[0,5,0],direction,100), (name,'missing wall',direction)
        start=np.array([0,5,0])+np.array(direction)*100
        assert hits(ts,start,-np.array(direction),100), (name,'missing inward face',direction)
        checks+=2
    for scale in [.05,.08,.13,1]:
        assert not hits(ts*scale,np.array([0,5,40])*scale,[0,0,-1],40*scale)
        checks+=1
    for a,b,c in original:
        n=np.cross(b-a,c-a);length=np.linalg.norm(n)
        if length<1e-3: continue
        n/=length
        if abs(n[1])>.35: continue
        forward=np.array([n[0],0,n[2]]);forward/=np.linalg.norm(forward)
        right=np.cross([0,1,0],forward);basis=np.column_stack([right,[0,1,0],forward])
        projected=np.array([a,b,c])@basis;low=projected.min(axis=0);high=projected.max(axis=0);size=high-low
        if size[0]<1 or size[1]<3: continue
        size[2]=max(.7,size[2]);center=(low+high)/2
        for z in np.linspace(0,40,81):
            p=np.array([0,5,z])@basis
            assert not np.all(np.abs(p-center)<size/2), (name,'nav obstacle crosses entry',z)
    checks+=1
    print('PASS',name,': entrance rays, two-sided wall/roof rays, four scales')
print(checks,'geometry checks passed; capsule motion and wildlife require in-game testing.')
