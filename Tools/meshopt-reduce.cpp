#include "meshoptimizer/meshoptimizer.h"
#include <fstream>
#include <vector>
#include <cstdint>
#include <stdexcept>
#include <iostream>
struct Vertex { float p[3], n[3], uv[2]; };
template<class T> T read(std::ifstream& f) { T x; f.read(reinterpret_cast<char*>(&x),sizeof(x)); if(!f) throw std::runtime_error("truncated mesh input"); return x; }
template<class T> void write(std::ofstream& f, T x) { f.write(reinterpret_cast<const char*>(&x),sizeof(x)); }
int main(int argc,char** argv) {
 try {
  if(argc!=3) throw std::runtime_error("expected input and output paths");
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
   if(reduce && ni) count=meshopt_simplifyWithAttributes(result.data(),indices.data(),ni,
    vertices[0].p,nv,sizeof(Vertex),vertices[0].n,sizeof(Vertex),weights,5,locked.data(),
    size_t(ni*0.45f)/3*3,0.002f,
    meshopt_SimplifyLockBorder|meshopt_SimplifyErrorAbsolute|meshopt_SimplifySparse|meshopt_SimplifyRegularizeLight,&error);
   else result=indices;
   write(out,error); write(out,uint32_t(count)); out.write(reinterpret_cast<const char*>(result.data()),count*sizeof(uint32_t));
  }
  if(!out) throw std::runtime_error("could not save mesh output");
 } catch(const std::exception& e) { std::cerr<<e.what()<<"\n"; return 1; }
 return 0;
}
