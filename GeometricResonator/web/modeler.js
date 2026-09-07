const canvas = document.getElementById("canvas");
const wrap = document.getElementById("canvasWrap");
const ctx = canvas.getContext("2d");
const controls = Object.fromEntries(["instrument","pitch","excitation","damping","brightness","volume"].map(id => [id, document.getElementById(id)]));
const fields = Object.fromEntries(["objectWidth","objectHeight","objectX","objectY"].map(id => [id, document.getElementById(id)]));
const analysis = document.getElementById("analysis");
const selectionStatus = document.getElementById("selectionStatus");
let objects = [], selected = new Set(), tool = "select", operation = null, nextId = 1, nextGroup = 1;
let audioContext = null, synth = null, sounding = false, audioLevel = 0, animationPhase = 0;

const shapeModes = {
  rect:[1,1.41,2,2.24,2.83], ellipse:[1,1.59,2.14,2.65,3.16], triangle:[1,1.67,2.33,2.81,3.42]
};
const shapeNames = {rect:"Rechteck",ellipse:"Ellipse",triangle:"Dreieck"};

function resizeCanvas() { const r=wrap.getBoundingClientRect(),dpr=Math.min(devicePixelRatio,2); canvas.width=Math.round(r.width*dpr);canvas.height=Math.round(r.height*dpr);ctx.setTransform(dpr,0,0,dpr,0,0); }
new ResizeObserver(resizeCanvas).observe(wrap);
function point(event){const r=canvas.getBoundingClientRect();return{x:event.clientX-r.left,y:event.clientY-r.top};}
function normalize(object){if(object.w<0){object.x+=object.w;object.w=-object.w;}if(object.h<0){object.y+=object.h;object.h=-object.h;}return object;}
function hitTest(p){for(let i=objects.length-1;i>=0;i--){const o=objects[i];if(p.x>=o.x&&p.x<=o.x+o.w&&p.y>=o.y&&p.y<=o.y+o.h)return o;}return null;}
function selectedObjects(){return objects.filter(o=>selected.has(o.id));}
function setTool(value){tool=value;document.querySelectorAll("[data-tool]").forEach(b=>b.classList.toggle("active",b.dataset.tool===value));canvas.style.cursor=value==="select"?"default":"crosshair";}
document.getElementById("toolbar").addEventListener("click",e=>{const button=e.target.closest("[data-tool]");if(button)setTool(button.dataset.tool);});

canvas.addEventListener("pointerdown",e=>{
  const p=point(e);canvas.setPointerCapture(e.pointerId);
  if(tool!=="select"){
    const object={id:nextId++,type:tool,x:p.x,y:p.y,w:1,h:1,group:null};objects.push(object);selected=new Set([object.id]);operation={kind:"draw",object,start:p};
  }else{
    const object=hitTest(p);
    if(!object){if(!e.shiftKey)selected.clear();operation=null;updateInspector();return;}
    if(e.shiftKey){selected.has(object.id)?selected.delete(object.id):selected.add(object.id);}else if(!selected.has(object.id)){selected=new Set([object.id]);}
    const group=selectedObjects();operation={kind:"move",start:p,originals:group.map(o=>({o,x:o.x,y:o.y}))};
  }
  updateInspector();sendGeometry();
});
canvas.addEventListener("pointermove",e=>{
  if(!operation)return;const p=point(e);
  if(operation.kind==="draw"){operation.object.w=p.x-operation.start.x;operation.object.h=p.y-operation.start.y;}
  else {const dx=p.x-operation.start.x,dy=p.y-operation.start.y;operation.originals.forEach(v=>{v.o.x=v.x+dx;v.o.y=v.y+dy;});}
  updateInspector();sendGeometry();
});
canvas.addEventListener("pointerup",e=>{
  if(operation?.kind==="draw"){normalize(operation.object);if(operation.object.w<8||operation.object.h<8){objects=objects.filter(o=>o!==operation.object);selected.clear();}setTool("select");}
  operation=null;updateInspector();sendGeometry();saveDesign();
});

function updateInspector(){
  const list=selectedObjects(),single=list.length===1?list[0]:null;
  selectionStatus.textContent=list.length?`${list.length} Objekt${list.length>1?"e":""} ausgewählt${single?` · ${shapeNames[single.type]}`:""}`:"Kein Objekt ausgewählt";
  for(const field of Object.values(fields))field.disabled=!single;
  if(single){fields.objectWidth.value=Math.round(single.w);fields.objectHeight.value=Math.round(single.h);fields.objectX.value=Math.round(single.x);fields.objectY.value=Math.round(single.y);}
}
for(const [id,field] of Object.entries(fields))field.addEventListener("change",()=>{const o=selectedObjects()[0];if(!o)return;const value=Math.max(id==="objectX"||id==="objectY"?-200:10,+field.value||0);if(id==="objectWidth")o.w=value;if(id==="objectHeight")o.h=value;if(id==="objectX")o.x=value;if(id==="objectY")o.y=value;sendGeometry();saveDesign();updateInspector();});

document.getElementById("join").onclick=()=>{const list=selectedObjects();if(list.length<2)return;const group=nextGroup++;list.forEach(o=>o.group=group);sendGeometry();saveDesign();};
document.getElementById("ungroup").onclick=()=>{selectedObjects().forEach(o=>o.group=null);sendGeometry();saveDesign();};
document.getElementById("duplicate").onclick=()=>{const copies=selectedObjects().map(o=>({...o,id:nextId++,x:o.x+18,y:o.y+18,group:null}));objects.push(...copies);selected=new Set(copies.map(o=>o.id));updateInspector();sendGeometry();saveDesign();};
document.getElementById("remove").onclick=()=>{objects=objects.filter(o=>!selected.has(o.id));selected.clear();updateInspector();sendGeometry();saveDesign();};
document.getElementById("clear").onclick=()=>{objects=[];selected.clear();updateInspector();sendGeometry();saveDesign();};
window.addEventListener("keydown",e=>{if((e.key==="Delete"||e.key==="Backspace")&&!e.target.matches("input"))document.getElementById("remove").click();});

function areaOf(o){return o.type==="ellipse"?Math.PI*o.w*o.h/4:o.type==="triangle"?o.w*o.h/2:o.w*o.h;}
function geometryAnalysis(){
  if(!objects.length)return{modes:[1],frequency:+controls.pitch.value,totalArea:0,bounds:null,groups:0};
  const totalArea=objects.reduce((s,o)=>s+areaOf(o),0),minX=Math.min(...objects.map(o=>o.x)),minY=Math.min(...objects.map(o=>o.y)),maxX=Math.max(...objects.map(o=>o.x+o.w)),maxY=Math.max(...objects.map(o=>o.y+o.h));
  const modes=[];
  for(const o of objects){const area=areaOf(o),scale=Math.sqrt(totalArea/Math.max(area,1)),aspect=Math.max(o.w,o.h)/Math.max(1,Math.min(o.w,o.h));shapeModes[o.type].slice(0,4).forEach((mode,index)=>modes.push(mode*scale*(1+(aspect-1)*.055*index)));}
  const grouped=[...new Set(objects.filter(o=>o.group).map(o=>o.group))];
  for(const group of grouped){const members=objects.filter(o=>o.group===group);if(members.length>1)modes.push(1+members.length*.18);}
  const clean=[1,...modes.filter(m=>m>.65&&m<12).sort((a,b)=>a-b)].filter((m,i,a)=>i===0||Math.abs(m-a[i-1])>.035).slice(0,12);
  const largest=Math.max(maxX-minX,maxY-minY,80),frequency=Math.max(40,Math.min(1600,+controls.pitch.value*260/largest));
  return{modes:clean,frequency,totalArea,bounds:{x:minX,y:minY,w:maxX-minX,h:maxY-minY},groups:grouped.length};
}
function params(){const g=geometryAnalysis();return{instrument:controls.instrument.value,pitch:g.frequency,size:100,excitation:+controls.excitation.value,damping:+controls.damping.value,brightness:+controls.brightness.value,volume:+controls.volume.value,customModes:g.modes};}
function updateLabels(){for(const id of ["pitch","excitation","damping","brightness","volume"]){const value=+controls[id].value;document.getElementById(id+"Out").textContent=(id==="pitch"?Math.round(value):value.toFixed(2))+(id==="pitch"?" Hz":"");}const g=geometryAnalysis();analysis.innerHTML=objects.length?`<strong>${objects.length} Teil${objects.length>1?"e":""}, ${g.groups} Verbindung${g.groups===1?"":"en"}</strong><br>Fläche: ${Math.round(g.totalArea).toLocaleString("de-CH")} px²<br>Klanggrundton: ${g.frequency.toFixed(1)} Hz<br>Resonanzmoden: ${g.modes.map(v=>v.toFixed(2)).join(" · ")}`:"Zeichne mindestens ein Objekt.";}
function sendGeometry(){updateLabels();if(synth)synth.port.postMessage(params());}
Object.values(controls).forEach(control=>control.addEventListener("input",()=>{sendGeometry();saveDesign();}));

async function ensureAudio(){if(!audioContext){audioContext=new AudioContext({latencyHint:"interactive"});await audioContext.audioWorklet.addModule("synth-processor.js");synth=new AudioWorkletNode(audioContext,"geometric-synth",{outputChannelCount:[1]});synth.connect(audioContext.destination);synth.port.onmessage=e=>{if(e.data.level!==undefined)audioLevel=e.data.level;};sendGeometry();}if(audioContext.state!=="running")await audioContext.resume();document.getElementById("audioState").textContent="Audio aktiv";}
document.getElementById("render").onclick=async()=>{if(!objects.length)return;await ensureAudio();sounding=true;synth.port.postMessage({...params(),gate:true});};
document.getElementById("stop").onclick=()=>{sounding=false;if(synth)synth.port.postMessage({gate:false});};

function offlineAudio(duration=5){const p=params(),rate=44100,count=rate*duration,data=new Float32Array(count),phases=new Float64Array(p.customModes.length),harmonics=12;let phase=0,previous=0;for(let i=0;i<count;i++){const t=i/rate,attack=Math.min(1,i/(rate*.03)),release=Math.min(1,(count-i)/(rate*.08)),env=Math.min(attack,release),freq=p.pitch,valueBody=p.customModes.reduce((sum,ratio,m)=>{phases[m]+=2*Math.PI*freq*ratio/rate;return sum+Math.sin(phases[m]+m*.21)*Math.exp(-m*(.42+p.damping*1.8));},0);let value;if(p.instrument==="flute"){phase+=2*Math.PI*freq*(1+.0028*p.excitation*Math.sin(2*Math.PI*5*t))/rate;const noise=Math.random()*2-1;value=.72*Math.sin(phase)+.14*Math.sin(2*phase)+.18*valueBody+(noise-previous)*(.01+p.excitation*.03);previous=noise;}else{phase+=2*Math.PI*freq*(1+.0038*p.excitation*Math.sin(2*Math.PI*5.3*t))/rate;let string=0,norm=0,roll=.72+(1-p.brightness)*1.25;for(let h=1;h<=harmonics;h++){const a=1/Math.pow(h,roll);string+=Math.sin(phase*h)*a;norm+=a;}value=.76*Math.tanh(string/norm*(.65+p.excitation*2.2))+.24*valueBody;}data[i]=Math.tanh(value*(1.1+p.excitation*1.9))*env*p.volume*.85;}return{data,rate};}
function wavBlob(audio){const buffer=new ArrayBuffer(44+audio.data.length*2),view=new DataView(buffer),write=(offset,text)=>[...text].forEach((c,i)=>view.setUint8(offset+i,c.charCodeAt(0)));write(0,"RIFF");view.setUint32(4,36+audio.data.length*2,true);write(8,"WAVE");write(12,"fmt ");view.setUint32(16,16,true);view.setUint16(20,1,true);view.setUint16(22,1,true);view.setUint32(24,audio.rate,true);view.setUint32(28,audio.rate*2,true);view.setUint16(32,2,true);view.setUint16(34,16,true);write(36,"data");view.setUint32(40,audio.data.length*2,true);audio.data.forEach((v,i)=>view.setInt16(44+i*2,Math.max(-1,Math.min(1,v))*32767,true));return new Blob([buffer],{type:"audio/wav"});}
document.getElementById("wav").onclick=()=>{if(!objects.length)return;const url=URL.createObjectURL(wavBlob(offlineAudio())),a=document.createElement("a");a.href=url;a.download="geometric-resonator.wav";a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);};
document.getElementById("example").onclick=()=>{objects=[{id:nextId++,type:"ellipse",x:210,y:120,w:290,h:210,group:1},{id:nextId++,type:"triangle",x:390,y:240,w:210,h:240,group:1},{id:nextId++,type:"rect",x:125,y:290,w:210,h:100,group:1}];nextGroup=2;selected=new Set(objects.map(o=>o.id));updateInspector();sendGeometry();saveDesign();};

function saveDesign(){localStorage.setItem("geometric-resonator-design",JSON.stringify({objects,nextId,nextGroup,values:Object.fromEntries(Object.entries(controls).map(([id,c])=>[id,c.value]))}));}
function loadDesign(){try{const saved=JSON.parse(localStorage.getItem("geometric-resonator-design"));if(!saved)return;objects=saved.objects||[];nextId=saved.nextId||1;nextGroup=saved.nextGroup||1;for(const[id,value]of Object.entries(saved.values||{}))if(controls[id])controls[id].value=value;}catch(_error){}}

function drawObject(o){ctx.save();const chosen=selected.has(o.id),grouped=o.group!==null;ctx.fillStyle=grouped?"rgba(64,184,214,.20)":"rgba(25,74,94,.38)";ctx.strokeStyle=chosen?"#ffd166":grouped?"#55e4ff":"#7ea5b7";ctx.lineWidth=chosen?3:2;ctx.setLineDash(grouped?[8,4]:[]);ctx.beginPath();if(o.type==="ellipse")ctx.ellipse(o.x+o.w/2,o.y+o.h/2,Math.abs(o.w/2),Math.abs(o.h/2),0,0,Math.PI*2);else if(o.type==="triangle"){ctx.moveTo(o.x+o.w/2,o.y);ctx.lineTo(o.x+o.w,o.y+o.h);ctx.lineTo(o.x,o.y+o.h);ctx.closePath();}else ctx.rect(o.x,o.y,o.w,o.h);ctx.fill();ctx.stroke();if(chosen){ctx.setLineDash([]);ctx.fillStyle="#ffd166";for(const[x,y]of[[o.x,o.y],[o.x+o.w,o.y],[o.x+o.w,o.y+o.h],[o.x,o.y+o.h]])ctx.fillRect(x-4,y-4,8,8);}ctx.restore();}
function draw(){const r=wrap.getBoundingClientRect(),w=r.width,h=r.height;ctx.clearRect(0,0,w,h);ctx.strokeStyle="#102b38";ctx.lineWidth=1;for(let x=0;x<w;x+=25){ctx.beginPath();ctx.moveTo(x,0);ctx.lineTo(x,h);ctx.stroke();}for(let y=0;y<h;y+=25){ctx.beginPath();ctx.moveTo(0,y);ctx.lineTo(w,y);ctx.stroke();}animationPhase+=.025;if(sounding&&objects.length){const b=geometryAnalysis().bounds,pulse=8+audioLevel*120+Math.sin(animationPhase)*3;ctx.strokeStyle="rgba(255,95,189,.35)";ctx.lineWidth=2;ctx.strokeRect(b.x-pulse,b.y-pulse,b.w+pulse*2,b.h+pulse*2);}objects.forEach(drawObject);requestAnimationFrame(draw);}

loadDesign();updateInspector();sendGeometry();resizeCanvas();draw();
