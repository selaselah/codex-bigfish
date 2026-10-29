#include "meshoptimizer/meshoptimizer.h"
#include <fstream>
#include <vector>
#include <cstdint>
#include <iostream>
#include <stdexcept>
#include <set>
#include <array>
#include <algorithm>
#include <cmath>
template<class T>T read(std::ifstream& f){T x;f.read((char*)&x,sizeof x);if(!f)throw std::runtime_error("truncated LOD input");return x;}
int main(int argc,char**argv){try{
 if(argc!=3)throw std::runtime_error("expected LOD input/output paths");std::ifstream f(argv[1],std::ios::binary);
 if(read<uint32_t>(f)!=0x35444f4c)throw std::runtime_error("invalid LOD input");
 uint32_t nv=read<uint32_t>(f),ni=read<uint32_t>(f),na=read<uint32_t>(f),target=read<uint32_t>(f);float limit=read<float>(f);uint32_t options=read<uint32_t>(f);
 if(!nv||nv>1000000||ni%3||ni>10000000||na>32||target>ni)throw std::runtime_error("invalid LOD counts");
 std::vector<float>p(nv*3),a(nv*na),w(na);std::vector<unsigned char>locks(nv);std::vector<unsigned int>idx(ni),out(ni);
 f.read((char*)p.data(),p.size()*4);f.read((char*)a.data(),a.size()*4);f.read((char*)w.data(),w.size()*4);f.read((char*)locks.data(),nv);f.read((char*)idx.data(),ni*4);if(!f)throw std::runtime_error("truncated LOD payload");for(auto i:idx)if(i>=nv)throw std::runtime_error("invalid LOD index");
 uint32_t guardCount=0;std::vector<float>guardPositions,guardNormals;
 if(f.peek()!=std::char_traits<char>::eof()){guardCount=read<uint32_t>(f);if(guardCount>32)throw std::runtime_error("guard pose count");guardPositions.resize(guardCount*nv*3);guardNormals.resize(guardCount*nv*3);f.read((char*)guardPositions.data(),guardPositions.size()*4);f.read((char*)guardNormals.data(),guardNormals.size()*4);if(!f)throw std::runtime_error("guard poses truncated");}
 auto key=[](unsigned int x,unsigned int y,unsigned int z){if(x<y&&x<z)return std::array<unsigned int,3>{{x,y,z}};if(y<z)return std::array<unsigned int,3>{{y,z,x}};return std::array<unsigned int,3>{{z,x,y}};};
 std::set<std::array<unsigned int,3>>original;for(size_t i=0;i<ni;i+=3)original.insert(key(idx[i],idx[i+1],idx[i+2]));
 float error=0;size_t count=0;
 for(int attempt=0;;attempt++){
  count=meshopt_simplifyWithAttributes(out.data(),idx.data(),ni,p.data(),nv,12,a.data(),na*4,w.data(),na,locks.data(),target,limit,options,&error);
  std::vector<unsigned char>affected(nv,0);size_t bad=0;
  for(size_t i=0;i<count;i+=3){unsigned int x=out[i],y=out[i+1],z=out[i+2];if(original.count(key(x,y,z)))continue;
   double u[3],v[3],g[3],area=0,dot=0;for(int k=0;k<3;k++){u[k]=double(p[y*3+k])-p[x*3+k];v[k]=double(p[z*3+k])-p[x*3+k];}g[0]=u[1]*v[2]-u[2]*v[1];g[1]=u[2]*v[0]-u[0]*v[2];g[2]=u[0]*v[1]-u[1]*v[0];for(int k=0;k<3;k++){area+=g[k]*g[k];dot+=g[k]*(double(a[x*na+k])+a[y*na+k]+a[z*na+k]);}
   bool invalid=area<1e-18||dot < -1e-16;
   for(uint32_t pose=0;pose<guardCount;pose++){
    const float* pp=guardPositions.data()+pose*nv*3;const float* nn=guardNormals.data()+pose*nv*3;
    for(int k=0;k<3;k++){u[k]=double(pp[y*3+k])-pp[x*3+k];v[k]=double(pp[z*3+k])-pp[x*3+k];}g[0]=u[1]*v[2]-u[2]*v[1];g[1]=u[2]*v[0]-u[0]*v[2];g[2]=u[0]*v[1]-u[1]*v[0];area=dot=0;for(int k=0;k<3;k++){area+=g[k]*g[k];dot+=g[k]*(double(nn[x*3+k])+nn[y*3+k]+nn[z*3+k]);}invalid|=area<1e-18||dot < -1e-16;
   }
   if(invalid){affected[x]=affected[y]=affected[z]=1;bad++;}
  }
  if(!bad)break;if(attempt>=32)throw std::runtime_error("LOD orientation protection failed");bool changed=false;
  for(size_t i=0;i<ni;i+=3)if(affected[idx[i]]||affected[idx[i+1]]||affected[idx[i+2]])for(int k=0;k<3;k++)if(!(locks[idx[i+k]]&1)){locks[idx[i+k]]|=1;changed=true;}
  std::cerr<<"Protect source rings for "<<bad<<" new opposed/degenerate facets; retry "<<attempt+1<<"\n";if(!changed)throw std::runtime_error("LOD orientation protection stalled");
 }
 std::ofstream g(argv[2],std::ios::binary);uint32_t magic=0x35444f4c,n=(uint32_t)count;g.write((char*)&magic,4);g.write((char*)&n,4);g.write((char*)&error,4);g.write((char*)out.data(),n*4);if(!g)throw std::runtime_error("cannot save LOD output");
 std::cout<<ni/3<<" -> "<<count/3<<" triangles; combined attribute/position error "<<error<<"\n";
 }catch(std::exception&e){std::cerr<<e.what()<<"\n";return 1;}return 0;}
