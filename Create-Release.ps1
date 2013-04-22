$SolutionDir = split-path -parent $PSCommandPath
$wixdir = Join-Path (Join-Path $SolutionDir "packages") "wix.3.7"
$TargetDir = Join-Path $SolutionDir "publish"
$WxsFile = Join-Path $SolutionDir "Solutionizer.wxs"

function Get-WiX {
    if (!(Test-Path $WixDir)) {
        New-Item -ItemType Directory -Path $WixDir
    }

    $wixarchive = Join-Path $WixDir "wix.zip"
    if (!(Test-Path $wixarchive)) {
        Write-Host "$wixarchive download"
        DownloadFile "http://wixtoolset.org/releases/v3.8.415.0/wix38-binaries.zip" $wixarchive
    } else {
        Write-Host "wix.zip exists" -Debug
    }

    if (!(Test-Path (Join-Path $WixDir "heat.exe"))) {
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::ExtractToDirectory($wixarchive, $WixDir)
    } else {
        Write-Host "WiX is already expanded" -Debug
    }
}

function DownloadFile($url, $targetFile) {
   $uri = New-Object "System.Uri" "$url"
   $request = [System.Net.HttpWebRequest]::Create($uri)
   $request.set_Timeout(15000) #15 second timeout
   $response = $request.GetResponse()
   $totalLength = [System.Math]::Floor($response.get_ContentLength()/1024)
   $responseStream = $response.GetResponseStream()
   $targetStream = New-Object -TypeName System.IO.FileStream -ArgumentList $targetFile, Create
   $buffer = new-object byte[] 16KB
   $count = $responseStream.Read($buffer,0,$buffer.length)
   $downloadedBytes = $count
   while ($count -gt 0) {
       $targetStream.Write($buffer, 0, $count)
       $count = $responseStream.Read($buffer,0,$buffer.length)
       $downloadedBytes = $downloadedBytes + $count
       Write-Progress -activity "Downloading file '$($url.split('/') | Select -Last 1)'" -status "Downloaded ($([System.Math]::Floor($downloadedBytes/1024))K of $($totalLength)K): " -PercentComplete ((([System.Math]::Floor($downloadedBytes/1024)) / $totalLength)  * 100)
   }
   Write-Progress -activity "Finished downloading file '$($url.split('/') | Select -Last 1)'"
   $targetStream.Flush()
   $targetStream.Close()
   $targetStream.Dispose()
   $responseStream.Dispose()
}


Get-WiX

$solutionizerExe = Join-Path $SolutionDir "Solutionizer/bin/Debug/Solutionizer.exe"
$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($solutionizerExe).FileVersion

if (!(Test-Path $TargetDir)) {
    New-Item -ItemType Directory -Path $TargetDir
}

Push-Location $SolutionDir
Invoke-Expression "$(Join-Path $wixdir 'candle.exe') -out '$TargetDir\Solutionizer.wixobj' '$WxsFile'"
Invoke-Expression "$(Join-Path $wixdir 'light.exe') -ext WixNetFxExtension -ext WixUIExtension -ext WixUtilExtension -out '$TargetDir\Solutionizer-$Version.msi' '$TargetDir\Solutionizer.wixobj'"
Pop-Location