#include "isxdl.iss"
#include "services.iss"

[Setup]
AppId={{4AEF0881-AF27-4586-AC7E-68ED34778975}
AppName=UV Outliner
AppVerName=UV Outliner 2.4.2
AppPublisher=Fedir Nepyivoda
AppPublisherURL=http://www.uvoutliner.com/
AppSupportURL=http://www.uvoutliner.com/
AppUpdatesURL=http://www.uvoutliner.com/
DefaultDirName={pf}\UV Outliner
DefaultGroupName=UV Outliner
OutputDir=C:\Development\UV-Outliner\Setup\bin
OutputBaseFilename=uvoutliner-setup
Compression=lzma
SolidCompression=yes
ChangesAssociations=yes
ShowLanguageDialog=no

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
DOTNET40Title=Microsoft .NET Framework 4.0

DependenciesDir=Redist
;memos
en.DependenciesDownloadTitle=Download Dependencies
en.DependenciesInstallTitle=Install Dependencies

;messages
en.WinXPMsg=This program can be installed only on Windows XP/2003/Vista/7.
en.WinXPSp2Msg=Windows XP Service Pack 2 must be installed to proceed with setup. Please update your Windows.
en.MSInstMsg=Setup has detected that the following required component is not installed: Microsoft Windows Installer 3.1. To proceed, download and install the Windows Installer 3.1 Redistributable from http://go.microsoft.com/fwlink/?LinkId=50380, and the run Atola Insight Setup again.

en.DownloadMsg1=The following applications are required before setup can continue:
en.DownloadMsg2=Download and install now?

;install text

en.XPSP2DownloadSize=~1 MB
en.DOTNETInstallMsg=Installing Microsoft .NET Framework 4.0... (May take a few minutes)
en.DOTNETDownloadSize=~1 MB


[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[_ISTool]
EnableISX=true

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "C:\Development\UV-Outliner\Sources\bin\Release\uv.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Development\UV-Outliner\Setup\Redist\dotNetFx40_Client_setup.exe"; Flags: dontcopy ignoreversion
Source: "C:\Development\UV-Outliner\Setup\Templates\default.uvxml"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\UV Outliner"; Filename: "{app}\uv.exe"
Name: "{group}\{cm:UninstallProgram,UV Outliner}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\UV Outliner"; Filename: "{app}\uv.exe"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\UV Outliner"; Filename: "{app}\uv.exe"; Tasks: quicklaunchicon

[Run]
Filename: {ini:{tmp}\dep.ini,install,dotnet40}; Description: {cm:DOTNET40Title}; StatusMsg: {cm:DOTNETInstallMsg}; Parameters: "/C:""install"""; Flags: skipifdoesntexist
Filename: "{app}\uv.exe"; Description: "{cm:LaunchProgram,UV Outliner}"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCR; Subkey: ".uvxml"; ValueType: string; ValueName: ""; ValueData: "UVOutliner"; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".uvxml\ShellNew"; ValueType: string; ValueName: "command"; ValueData: """{app}\uv.exe"""; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".uvxml\ShellNew"; ValueType: string; ValueName: "filename"; ValueData: "{app}\default.uvxml"; Flags: uninsdeletevalue
Root: HKCR; Subkey: ".uvxml\ShellNew"; ValueType: string; ValueName: "iconpath"; ValueData: """{app}\uv.exe"",0"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "UVOutliner"; ValueType: string; ValueName: ""; ValueData: "UV Outliner document"; Flags: uninsdeletekey
Root: HKCR; Subkey: "UVOutliner\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\uv.exe"",0"
Root: HKCR; Subkey: "UVOutliner\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\uv.exe"" ""%1"""

[Code]
var
  dotnet40Path: string;
  neededDependenciesInstallMemo: string;
  installDotNet: boolean;

const
  dotnet40URL = 'http://download.microsoft.com/download/7/B/6/7B629E05-399A-4A92-B5BC-484C74B5124B/dotNetFx40_Client_setup.exe'; //'http://download.microsoft.com/download/1/B/E/1BE39E79-7E39-46A3-96FF-047F95396215/dotNetFx40_Full_setup.exe';

function InitializeSetup(): Boolean;
var
  SoftwareVersion: string;
  WindowsVersion: TWindowsVersion;
  ServerInst: string;

  KeyNames: TArrayOfString;
  S: String;
  i: Integer;
  ms,ls: cardinal;
  InstallerVer: String;
  res: Boolean;

  WinInstallerMinVer: Integer;
  WinInstallerMajVer: Integer;
  WinInstallerRequired: Boolean;

begin
  GetWindowsVersionEx(WindowsVersion);
  Result := true;
  installDotNet := false;

  // Check for Windows XP SP2
  if WindowsVersion.NTPlatform and
     (WindowsVersion.Major = 5) and
     (WindowsVersion.Minor = 1) and
     (WindowsVersion.ServicePackMajor < 2) then
  begin
    MsgBox(CustomMessage('WinXPSp2Msg'), mbError, MB_OK);
    Result := false;
    exit;
  end;
  
    // Check for Windows Installer 3.1
  res :=  GetVersionNumbersString(ExpandConstant('{sys}\msi.dll'), S);
  if (res = true) then
  begin
    WinInstallerRequired := False;
    WinInstallerMinVer := StrToIntDef(Copy(S, 1, 1), -1);

    if (WinInstallerMinVer = -1) or (WinInstallerMinVer < 3) then
      WinInstallerRequired := True
    else if (WinInstallerMinVer = 3) then
    begin
      WinInstallerMajVer := StrToIntDef(Copy(S, 3, 1), -1);
      if (WinInstallerMajVer < 1) then
        WinInstallerRequired := True;
    end;

    if WinInstallerRequired then
    begin
      MsgBox(CustomMessage('MSInstMsg'), mbError, MB_OK);
      Result := false;
      exit;
    end;
  end;

  // Check for required dotnetfx 4.0 installation
  if (not RegKeyExists(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4')) then
  begin
    neededDependenciesInstallMemo := neededDependenciesInstallMemo + '      ' + CustomMessage('DOTNET40Title') + #13;
    installDotNet := True;
  end;

end;


function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  s: string;
  dotnetpath: string;

begin
  if installDotNet then  
  begin
      ExtractTemporaryFile('dotNetFx40_Client_setup.exe');
      dotnetpath := ExpandConstant('{tmp}\dotNetFx40_Client_setup.exe'); 
      SetIniString('install', 'dotnet40', dotnetpath, ExpandConstant('{tmp}\dep.ini'));
  end;

  if neededDependenciesInstallMemo <> '' then s := s + CustomMessage('DependenciesInstallTitle') + ':' + NewLine + neededDependenciesInstallMemo + NewLine;

  s := s + MemoDirInfo + NewLine + NewLine + MemoGroupInfo
  if MemoTasksInfo <> '' then  s := s + NewLine + NewLine + MemoTasksInfo;

  Result := s
end;
