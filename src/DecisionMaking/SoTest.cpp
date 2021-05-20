#include <iostream>
#include <vector>
#include "Common.h"


// export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:.
// echo $LD_LIBRARY_PATH
// g++ -std=c++11 -O2 SoTest.cpp -o SoTest -L. -ldec_making
int main()
{
  auto num = G5Cpp::getInt();
  std::cout << num << std::endl;

  std::cout << "Creating game context" << std::endl;
  void* gc = G5Cpp::CreateGameContext();
  std::cout << "Created game context" << std::endl;

  std::cout << "Releasing game context" << std::endl;
  G5Cpp::ReleaseGameContext(gc);
  std::cout << "Game context released" << std::endl;

  return 0;
}
