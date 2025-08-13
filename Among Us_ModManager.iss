[Setup]
AppName=Among Us Mod Manager
AppVersion=1.3.1
DefaultDirName={pf64}\AmongUsModManager
DefaultGroupName=Among Us Mod Manager
UninstallDisplayIcon={app}\Among Us_ModManager.exe
OutputBaseFilename=Among Us_ModManager_Setup
OutputDir=C:\Users\2023016043\Documents\Among UsModManager\InstallerOutput
SetupIconFile=C:\Users\2023016043\Documents\InstallerAssets\icon.ico
LicenseFile=C:\Users\2023016043\Documents\InstallerAssets\LICENSE.txt
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
AppCopyright=© 2025 Tabasco1410

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
[Tasks]
Name: "desktopicon"; Description: "デスクトップにショートカットを作成"; GroupDescription: "追加タスク:"; Flags: unchecked
Name: "startmenuicon"; Description: "スタートメニューにショートカットを作成"; GroupDescription: "追加タスク:"; Flags: unchecked

[Files]
Source: "C:\Users\2023016043\Documents\Among UsModManager\Among Us_ModManager\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Among Us Mod Manager"; Filename: "{app}\Among Us_ModManager.exe"; Tasks: startmenuicon
Name: "{commondesktop}\Among Us Mod Manager"; Filename: "{app}\Among Us_ModManager.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Among Us_ModManager.exe"; Description: "Among Us_ModManager を起動する"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{app}\Among Us_ModManager.exe"
Type: dirifempty; Name: "{app}"
