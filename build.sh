rm -rf ./publish
mkdir  ./publish

rm -rf ./publish/build.net
rm -rf ./publish/build.cur
rm -rf ./publish/build.lin64
rm -rf ./publish/build.lin64sc

dotnet publish --output ./publish/build.net -c Release --self-contained false /p:PublishSingleFile=false

dotnet publish --output ./publish/build.cur -c Release --use-current-runtime true --self-contained false /p:PublishSingleFile=true /p:PublishReadyToRun=true

dotnet publish --output ./publish/build.lin64   -c Release -r linux-x64 --self-contained false /p:PublishSingleFile=true
dotnet publish --output ./publish/build.lin64sc -c Release -r linux-x64 --self-contained true  /p:PublishSingleFile=true /p:PublishTrimmed=true


7z a -y -t7z -stl -m0=lzma -mx=9 -ms=on -bb0 -bd -ssc -ssw ./publish/sdel-dotnet.7z  ./publish/build.net/     >> /dev/null
7z a -y -t7z -stl -m0=lzma -mx=9 -ms=on -bb0 -bd -ssc -ssw ./publish/sdel-lin64.7z   ./publish/build.lin64/   >> /dev/null
7z a -y -t7z -stl -m0=lzma -mx=9 -ms=on -bb0 -bd -ssc -ssw ./publish/sdel-lin64sc.7z ./publish/build.lin64sc/ >> /dev/null

echo
echo 'Published in '
echo `realpath ./publish`
echo
echo 'sdel-dotnet  for execute with dotnet sdel.dll'
echo 'sdel-lin64   for execute sdel (with .NET 7.0 on Linux)'
echo 'sdel-lin64sc for execute sdel on Linux x64 without .NET 7.0'
