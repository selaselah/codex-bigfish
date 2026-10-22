#include "meshoptimizer/meshoptimizer.h"
#include <fstream>
#include <vector>
#include <cstdint>
#include <stdexcept>
#include <iostream>
#include <array>
#include <set>
#include <cmath>
struct Vertex { float p[3], n[3], uv[2]; };
template<class T> T read(std::ifstream& f) { T x; f.read(reinterpret_cast<char*>(&x),sizeof(x)); if(!f) throw std::runtime_error("truncated mesh input"); return x; }
template<class T> void write(std::ofstream& f, T x) { f.write(reinterpret_cast<const char*>(&x),sizeof(x)); }
using Face=std::array<unsigned int,3>;
Face faceKey(unsigned int a,unsigned int b,unsigned int c) {
 if(a<b && a<c) return {a,b,c};
 if(b<c) return {b,c,a};
 return {c,a,b};
}
int main(int argc,char** argv) {
 try {
  if(argc!=3 && argc!=5) throw std::runtime_error("expected input and output paths");
  const float ratio=argc==5?std::stof(argv[3]):0.45f;
  const float tolerance=argc==5?std::stof(argv[4]):0.002f;
  if(!(ratio>0 && ratio<=1) || !(tolerance>0 && tolerance<=0.01f)) throw std::runtime_error("invalid reduction controls");
  std::ifstream in(argv[1],std::ios::binary);
  if(read<uint32_t>(in)!=0x46495348) throw std::runtime_error("invalid mesh input");
  uint32_t nv=read<uint32_t>(in), ns=read<uint32_t>(in);
  if(nv>1000000 || ns>1000 || !nv) throw std::runtime_error("invalid mesh counts");
  std::vector<Vertex> vertices(nv); in.read(reinterpret_cast<char*>(vertices.data()),nv*sizeof(Vertex));
  std::vector<unsigned char> locked(nv); in.read(reinterpret_cast<char*>(locked.data()),nv);
  std::ofstream out(argv[2],std::ios::binary); write(out,uint32_t(0x46495348)); write(out,ns);
  const float weights[5]={0.1f,0.1f,0.1f,1.f,1.f};
  for(uint32_t s=0;s<ns;s++) {
   bool reduce=read<uint32_t>(in)!=0; uint32_t ni=read<uint32_t>(in);
   if(ni>10000000 || ni%3) throw std::runtime_error("invalid triangle count");
   std::vector<unsigned int> indices(ni),result(ni);
   in.read(reinterpret_cast<char*>(indices.data()),ni*sizeof(uint32_t));
   if(!in) throw std::runtime_error("truncated index input");
   for(auto i:indices) if(i>=nv) throw std::runtime_error("invalid index");
   float error=0; size_t count=ni;
   if(reduce && ni) {
    std::set<Face> original;
    for(size_t i=0;i<ni;i+=3) original.insert(faceKey(indices[i],indices[i+1],indices[i+2]));
    // Index reduction retains the source shading normals. A new thin facet can
    // face against those normals even inside the geometric error tolerance.
    // Protect its original one-ring and retry, preserving original facets and
    // attributes rather than flipping winding or relaxing the error limit.
    for(unsigned int attempt=0;;attempt++) {
     count=meshopt_simplifyWithAttributes(result.data(),indices.data(),ni,
      vertices[0].p,nv,sizeof(Vertex),vertices[0].n,sizeof(Vertex),weights,5,locked.data(),
      size_t(ni*ratio)/3*3,tolerance,
      meshopt_SimplifyLockBorder|meshopt_SimplifyErrorAbsolute|meshopt_SimplifySparse|meshopt_SimplifyRegularizeLight,&error);
     std::vector<unsigned char> affected(nv,0);size_t opposed=0;
     for(size_t i=0;i<count;i+=3) {
      unsigned int a=result[i],b=result[i+1],c=result[i+2];
      if(original.count(faceKey(a,b,c))) continue;
      double u[3],v[3],g[3];
      for(int k=0;k<3;k++) { u[k]=double(vertices[b].p[k])-vertices[a].p[k];v[k]=double(vertices[c].p[k])-vertices[a].p[k]; }
      g[0]=u[1]*v[2]-u[2]*v[1];g[1]=u[2]*v[0]-u[0]*v[2];g[2]=u[0]*v[1]-u[1]*v[0];
      double area2=0,dot=0;
      for(int k=0;k<3;k++) { area2+=g[k]*g[k];dot+=g[k]*(double(vertices[a].n[k])+vertices[b].n[k]+vertices[c].n[k]); }
      if(area2>1e-24 && dot<0) { affected[a]=affected[b]=affected[c]=1;opposed++; }
     }
     if(!opposed) break;
     if(attempt>=8) throw std::runtime_error("could not preserve shading orientation during reduction");
     bool changed=false;
     for(size_t i=0;i<ni;i+=3) if(affected[indices[i]]||affected[indices[i+1]]||affected[indices[i+2]])
      for(int k=0;k<3;k++) { unsigned int v=indices[i+k];if(!locked[v]) { locked[v]=1;changed=true; } }
     if(!changed) throw std::runtime_error("shading orientation guard made no progress");
     std::cerr<<"Protecting source neighborhoods for "<<opposed<<" new opposed shading facets; retry "<<(attempt+1)<<"\n";
    }
   } else result=indices;
   write(out,error); write(out,uint32_t(count)); out.write(reinterpret_cast<const char*>(result.data()),count*sizeof(uint32_t));
  }
  if(!out) throw std::runtime_error("could not save mesh output");
 } catch(const std::exception& e) { std::cerr<<e.what()<<"\n"; return 1; }
 return 0;
}
