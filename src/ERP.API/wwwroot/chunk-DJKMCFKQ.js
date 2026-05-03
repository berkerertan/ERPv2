import{a as b}from"./chunk-KZBY5SMP.js";import{b as S}from"./chunk-533TRF6S.js";import"./chunk-RGAVFUWM.js";import"./chunk-LJBGPCP7.js";import{$a as a,Eb as h,Fb as p,Gb as v,sb as s,tb as m,ub as l,vc as g,xb as f}from"./chunk-PRNXKBAA.js";var L=["canvas"],c=class r{canvasRef;gl=null;program=null;animationId=null;timeUniform=null;resolutionUniform=null;time=1;resizeObserver=null;VS=`
    attribute vec2 a_pos;
    void main() {
      gl_Position = vec4(a_pos, 0.0, 1.0);
    }
  `;FS=`
    precision highp float;
    uniform vec2  resolution;
    uniform float time;

    float random(in float x) {
      return fract(sin(x) * 1e4);
    }

    void main(void) {
      vec2 uv = (gl_FragCoord.xy * 2.0 - resolution.xy) / min(resolution.x, resolution.y);

      // pixelate / mosaic effect
      vec2 fMosaicScal = vec2(4.0, 2.0);
      vec2 vScreenSize = vec2(256.0, 256.0);
      uv.x = floor(uv.x * vScreenSize.x / fMosaicScal.x) / (vScreenSize.x / fMosaicScal.x);
      uv.y = floor(uv.y * vScreenSize.y / fMosaicScal.y) / (vScreenSize.y / fMosaicScal.y);

      float t         = time * 0.06 + random(uv.x) * 0.4;
      float lineWidth = 0.0008;

      vec3 color = vec3(0.0);
      for (int j = 0; j < 3; j++) {
        for (int i = 0; i < 5; i++) {
          color[j] += lineWidth * float(i * i)
            / abs(fract(t - 0.01 * float(j) + float(i) * 0.01) * 1.0 - length(uv));
        }
      }

      gl_FragColor = vec4(color[2], color[1], color[0], 1.0);
    }
  `;ngAfterViewInit(){this.initWebGL()}ngOnDestroy(){this.animationId!==null&&cancelAnimationFrame(this.animationId),this.resizeObserver?.disconnect(),this.gl&&this.program&&this.gl.deleteProgram(this.program)}initWebGL(){let t=this.canvasRef.nativeElement,e=t.getContext("webgl");if(!e)return;this.gl=e;let n=this.compile(e,this.VS,e.VERTEX_SHADER),i=this.compile(e,this.FS,e.FRAGMENT_SHADER);if(!n||!i)return;let o=e.createProgram();if(e.attachShader(o,n),e.attachShader(o,i),e.linkProgram(o),!e.getProgramParameter(o,e.LINK_STATUS)){console.error("Shader link error:",e.getProgramInfoLog(o));return}this.program=o,e.useProgram(o);let C=new Float32Array([-1,-1,1,-1,-1,1,1,1]),A=e.createBuffer();e.bindBuffer(e.ARRAY_BUFFER,A),e.bufferData(e.ARRAY_BUFFER,C,e.STATIC_DRAW);let d=e.getAttribLocation(o,"a_pos");e.enableVertexAttribArray(d),e.vertexAttribPointer(d,2,e.FLOAT,!1,0,0),this.timeUniform=e.getUniformLocation(o,"time"),this.resolutionUniform=e.getUniformLocation(o,"resolution");let x=t.parentElement??t;this.resizeObserver=new ResizeObserver(()=>this.resize()),this.resizeObserver.observe(x),this.resize();let u=()=>{this.time+=.012,e.uniform1f(this.timeUniform,this.time),e.drawArrays(e.TRIANGLE_STRIP,0,4),this.animationId=requestAnimationFrame(u)};u()}resize(){let t=this.canvasRef.nativeElement,e=t.parentElement;if(!e||!this.gl)return;let n=window.devicePixelRatio||1;t.width=e.offsetWidth*n,t.height=e.offsetHeight*n,this.gl.viewport(0,0,t.width,t.height),this.resolutionUniform&&this.gl.uniform2f(this.resolutionUniform,t.width,t.height)}compile(t,e,n){let i=t.createShader(n);return t.shaderSource(i,e),t.compileShader(i),t.getShaderParameter(i,t.COMPILE_STATUS)?i:(console.error("Shader compile error:",t.getShaderInfoLog(i)),t.deleteShader(i),null)}static \u0275fac=function(e){return new(e||r)};static \u0275cmp=a({type:r,selectors:[["app-shader-animation"]],viewQuery:function(e,n){if(e&1&&h(L,5),e&2){let i;p(i=v())&&(n.canvasRef=i.first)}},decls:2,vars:0,consts:[["canvas",""]],template:function(e,n){e&1&&f(0,"canvas",null,0)},styles:["[_nghost-%COMP%]{position:absolute;inset:0;display:block;overflow:hidden}canvas[_ngcontent-%COMP%]{position:absolute;inset:0;width:100%;height:100%;display:block}"],changeDetection:0})};var y=class r{static \u0275fac=function(e){return new(e||r)};static \u0275cmp=a({type:r,selectors:[["app-auth-layout"]],decls:7,vars:0,consts:[[1,"auth-layout"],[1,"auth-bg"],[1,"shader-vignette"],[1,"auth-container"]],template:function(e,n){e&1&&(s(0,"div",0),l(1,"app-version-badge"),s(2,"div",1),l(3,"app-shader-animation")(4,"div",2),m(),s(5,"div",3),l(6,"router-outlet"),m()())},dependencies:[g,S,c,b],styles:[".auth-layout[_ngcontent-%COMP%]{min-height:100vh;display:flex;align-items:center;justify-content:center;position:relative;overflow:hidden;background:#000}.auth-bg[_ngcontent-%COMP%]{position:absolute;inset:0;z-index:0}.shader-vignette[_ngcontent-%COMP%]{position:absolute;inset:0;pointer-events:none;background:radial-gradient(ellipse 80% 80% at 50% 50%,transparent 30%,rgba(0,0,0,.55) 100%),radial-gradient(ellipse 50% 60% at 50% 50%,rgba(249,115,22,.04) 0%,transparent 70%)}.auth-container[_ngcontent-%COMP%]{position:relative;z-index:1;width:100%;max-width:440px;padding:var(--space-6)}@media(max-width:480px){.auth-container[_ngcontent-%COMP%]{padding:var(--space-4);max-width:100%}}"]})};export{y as AuthLayoutComponent};
