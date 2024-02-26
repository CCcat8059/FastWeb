Push-Location
Set-Location $PSScriptRoot
cd ..

$name = 'FastWeb'
$assembly = "Community.PowerToys.Run.Plugin.$name"
$version = "v$((Get-Content ./plugin.json | ConvertFrom-Json).Version)"
$archs = @('x64', 'arm64')

git tag $version
git push --tags

Remove-Item ./release/*.zip -Recurse -Force -ErrorAction Ignore
foreach ($arch in $archs) {
	$releasePath = "./bin/$arch/Release/net8.0-windows"

	dotnet build -c Release /p:Platform=$arch

	Remove-Item "./release/$name/*" -Recurse -Force -ErrorAction Ignore
	mkdir "./release/$name" -ErrorAction Ignore

	$files = @(
		"$releasePath/$assembly.dll",
		"$releasePath/plugin.json",
		"$releasePath/Images",
		"$releasePath/$assembly.deps.json",
		"$releasePath/webdata.json"
	)
	Copy-Item $files "./release/$name" -Recurse -Force
	Compress-Archive "./release/$name" "./release/$name-$version-$arch.zip" -Force
}

# gh release create $version (Get-ChildItem ./release/*.zip)
Pop-Location
