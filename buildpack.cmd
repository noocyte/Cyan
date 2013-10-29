mkdir release

@call msbuild src\Cyan\Cyan.csproj /t:clean
@call msbuild src\Cyan\Cyan.csproj /p:Configuration=Release

src\.nuget\NuGet.exe pack -sym src\Cyan\Cyan.csproj -OutputDirectory release