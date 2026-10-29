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
 if(argc!=3)throw std::runtime_error("expected input/output");std::ifstream f(argv[1],std::ios::binary);if(read<uint32_t>(f)!=0x35444f4c)throw std::runtime_error("magic");
 uint32_t nv=read<uint32_t>(f),ni=read<uint32_t>(f),na=read<uint32_t>(f),target=read<uint32_t>(f);float limit=read<float>(f);uint32_t options=read<uint32_t>(f);if(na>32||ni%3||target>ni)throw std::runtime_error("counts");
 std::vector<float>p(nv*3),a(nv*na),w(na);std::vector<unsigned char>locks(nv);std::vector<unsigned int>idx(ni),out;
 f.read((char*)p.data(),p.size()*4);f.read((char*)a.data(),a.size()*4);f.read((char*)w.data(),w.size()*4);f.read((char*)locks.data(),nv);f.read((char*)idx.data(),ni*4);if(!f)throw std::runtime_error("payload");
 std::vector<float>ps,at;float error=0;size_t count=0;auto key=[](unsigned int x,unsigned int y,unsigned int z){if(x<y&&x<z)return std::array<unsigned int,3>{{x,y,z}};if(y<z)return std::array<unsigned int,3>{{y,z,x}};return std::array<unsigned int,3>{{z,x,y}};};std::set<std::array<unsigned int,3>> original;for(size_t i=0;i<ni;i+=3)original.insert(key(idx[i],idx[i+1],idx[i+2]));
 for(int attempt=0;;attempt++){
  ps=p;at=a;out=idx;count=meshopt_simplifyWithUpdate(out.data(),ni,ps.data(),nv,12,at.data(),na*4,w.data(),na,locks.data(),target,limit,options,&error);
  std::vector<unsigned char>affected(nv,0);size_t bad=0;
  for(size_t i=0;i<count;i+=3){unsigned int x=out[i],y=out[i+1],z=out[i+2];bool unchanged=original.count(key(x,y,z));for(auto j:{x,y,z})for(int k=0;k<3;k++)unchanged&=ps[j*3+k]==p[j*3+k];if(unchanged)continue;
   double u[3],v[3],g[3],area=0,dot=0;for(int k=0;k<3;k++){u[k]=double(ps[y*3+k])-ps[x*3+k];v[k]=double(ps[z*3+k])-ps[x*3+k];}g[0]=u[1]*v[2]-u[2]*v[1];g[1]=u[2]*v[0]-u[0]*v[2];g[2]=u[0]*v[1]-u[1]*v[0];for(int k=0;k<3;k++){area+=g[k]*g[k];dot+=g[k]*(double(at[x*na+k])+at[y*na+k]+at[z*na+k]);}if(area<1e-18||dot< -1e-16){affected[x]=affected[y]=affected[z]=1;bad++;}
  }
  if(!bad)break;if(attempt>=32)throw std::runtime_error("orientation protection failed");bool changed=false;for(size_t i=0;i<ni;i+=3)if(affected[idx[i]]||affected[idx[i+1]]||affected[idx[i+2]])for(int k=0;k<3;k++)if(!(locks[idx[i+k]]&1)){locks[idx[i+k]]|=1;changed=true;}std::cerr<<"protect rings "<<bad<<" retry "<<attempt+1<<"\n";if(!changed)throw std::runtime_error("protection stalled");
 }
 std::ofstream g(argv[2],std::ios::binary);uint32_t magic=0x35444f4c,n=count;g.write((char*)&magic,4);g.write((char*)&n,4);g.write((char*)&error,4);g.write((char*)out.data(),n*4);g.write((char*)ps.data(),ps.size()*4);g.write((char*)at.data(),at.size()*4);if(!g)throw std::runtime_error("save");std::cout<<ni/3<<" -> "<<count/3<<" triangles; error "<<error<<"\n";
 }catch(std::exception&e){std::cerr<<e.what()<<"\n";return 1;}return 0;}
