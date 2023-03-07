cd .nuget
rm *.nupkg
cd ..
dotnet pack --configuration Release --output .nuget
cd .nuget
dotnet nuget push "*.nupkg" --source "naninovel"
read -r -p "Press Enter key to exit..."
