Install tbb on Ubuntu
  sudo apt-get install libtbb-dev

Install .NET on Ubuntu 16.10: https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore1x
  sudo sh -c 'echo "deb [arch=amd64] https://apt-mo.trafficmanager.net/repos/dotnet-release/ yakkety main" > /etc/apt/sources.list.d/dotnetdev.list'
  sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys B02C46DF417A0893
  sudo apt-get update
  sudo apt-get install dotnet-dev-1.0.4
  dotnet --version

Dotnet Core tutorials
  https://docs.microsoft.com/en-us/dotnet/articles/core/tutorials/index

Make new .NET app: https://docs.microsoft.com/en-us/dotnet/articles/core/tools/dotnet-new
  mkdir hwapp
  cd hwapp
  dotnet new
  dotnet new -t console
  dotnet new -t lib
  dotnet new -t web

Build and run .NET app
  dotnet restore
  dotnet build
  dotnet run

Something usefull:
  dotnet build -c Release -o ../bin -f netcoreapp1.0

project.json usefull settings
  "type": "platform",
  "runtimes":{
    "ubuntu.16.10-x64": { }
  }

Check architecture type
  objdump -f G5.Logic.dll | grep ^architecture

Project.json documentation:
  https://docs.microsoft.com/en-us/dotnet/articles/core/tools/project-json

Dotnet publish docs
  https://docs.microsoft.com/en-us/dotnet/articles/core/tools/dotnet-publish
  dotnet publish -c Release -o ../bin --version-suffix 123

Run bash scirpt
  chmod a+x Build.sh   #Gives everyone execute permissions
  chmod 700 Build.sh   #Gives read,write,execute permissions to the Owner
  ./Build.sh

Copy files to remote (linux) server using putty/SSH
  pscp -i "C:\2\private_putty.ppk" *.* ubuntu@18.218.92.203:/home/ubuntu/submission_2pn/
  pscp ubuntu@52.15.217.242:/home/ubuntu/submission_2pn.validation/submission_2pn.test_match.0.err ./
