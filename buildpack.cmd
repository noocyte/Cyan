mkdir release

@call msbuild Cyan\Cyan.csproj /t:clean
@call msbuild Cyan\Cyan.csproj /p:Configuration=Release

.nuget\NuGet.exe pack -sym Cyan\Cyan.csproj -OutputDirectory release

@echo Publish: .nuget\NuGet.exe publish release\Proactima.Cyan.[version].nupkg
