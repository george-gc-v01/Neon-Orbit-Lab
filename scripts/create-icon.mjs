// Copyright © 2026 George Culache (george-gc-v01)
// SPDX-License-Identifier: GPL-3.0-only
// Original geometric N; write a Windows multi-resolution ICO with no third-party dependencies.
import fs from 'node:fs';
import path from 'node:path';
const sizes = [16,20,24,32,48,64];
function segmentDistance(x,y,ax,ay,bx,by) {
  const t=Math.max(0,Math.min(1,((x-ax)*(bx-ax)+(y-ay)*(by-ay))/((bx-ax)**2+(by-ay)**2)));
  return Math.hypot(x-ax-t*(bx-ax),y-ay-t*(by-ay));
}
const frames=sizes.map(n=>{
  const stride=Math.ceil(n/32)*4, data=Buffer.alloc(40+n*n*4+stride*n);
  data.writeUInt32LE(40,0);data.writeInt32LE(n,4);data.writeInt32LE(n*2,8);data.writeUInt16LE(1,12);data.writeUInt16LE(32,14);
  for(let y=0;y<n;y++)for(let x=0;x<n;x++){
    let coverage=0;
    for(let j=0;j<4;j++)for(let i=0;i<4;i++){
      const u=(x+(i+.5)/4)/n,v=(y+(j+.5)/4)/n;
      if(Math.min(segmentDistance(u,v,.24,.78,.24,.22),segmentDistance(u,v,.24,.22,.76,.78),segmentDistance(u,v,.76,.78,.76,.22))<.075)coverage++;
    }
    const t=(x+y)/(2*n-2), pos=40+((n-1-y)*n+x)*4;
    data[pos]=Math.round(153+(255-153)*t);data[pos+1]=Math.round(255+(124-255)*t);data[pos+2]=Math.round(102+(40-102)*t);data[pos+3]=Math.round(255*coverage/16);
  }
  return data;
});
const header=Buffer.alloc(6+16*sizes.length);header.writeUInt16LE(1,2);header.writeUInt16LE(sizes.length,4);
let offset=header.length;
frames.forEach((frame,i)=>{const at=6+i*16;header[at]=sizes[i];header[at+1]=sizes[i];header.writeUInt16LE(1,at+4);header.writeUInt16LE(32,at+6);header.writeUInt32LE(frame.length,at+8);header.writeUInt32LE(offset,at+12);offset+=frame.length;});
const output='src/NeonOrbit.App/Assets/NeonOrbit.ico';fs.mkdirSync(path.dirname(output),{recursive:true});fs.writeFileSync(output,Buffer.concat([header,...frames]));
console.log('Generated multi-resolution N icon.');
