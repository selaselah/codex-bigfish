#!/bin/zsh
set -eu
tool_dir="${0:A:h}"
mkdir -p "$tool_dir/bin"
/usr/bin/clang++ -std=c++11 -O2 "$tool_dir/meshopt-reduce.cpp" "$tool_dir/meshoptimizer/simplifier.cpp" "$tool_dir/meshoptimizer/allocator.cpp" -o "$tool_dir/bin/meshopt-reduce"
