@echo off
SET "ProjectName=MapRenderer"
SET "SolutionDir=C:\Users\robin\Desktop\Games\RimWorld Modding\Source\MapRenderer\Source"
@echo on

del /S /Q "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\%ProjectName%\Defs\*"

xcopy /S /Y "%SolutionDir%\..\About\*" "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\%ProjectName%\About\"
xcopy /S /Y "%SolutionDir%\..\Assemblies\*" "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\%ProjectName%\Assemblies\"
xcopy /S /Y "%SolutionDir%\..\Languages\*" "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\%ProjectName%\Languages\"