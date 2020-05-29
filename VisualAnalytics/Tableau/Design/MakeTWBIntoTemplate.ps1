[CmdletBinding(SupportsShouldProcess=$true)]
param (
    [parameter(Mandatory=$true, HelpMessage="File to process")]
    [string] $inputFile,
    [parameter(Mandatory=$true, HelpMessage="Path to save as")]
    [string] $outputPath = "",
    [parameter(Mandatory=$true, HelpMessage="File to save as")]
    [string] $outputFile = ""
    )

#---------------------------------------------------------------------
# Script begins
$scriptFolderPath = Split-Path -Path $MyInvocation.MyCommand.Path

#---------------------------------------------------------------------
# Load input and output
Write-Output "Passed inputFile:"
$inputFile

Write-Output "Passed outputPath:"
$outputPath
$outputPath = Resolve-Path -Path $outputPath
$outputPath

Write-Output "Passed outputFile:"
$outputFile
$outputFile = Join-Path -Path $outputPath -ChildPath $outputFile
$outputFile 

#---------------------------------------------------------------------
# Load input file
$inputDocument = [xml](Get-Content -Path $inputFile)
# $inputDocument


#---------------------------------------------------------------------
# Edit each datasource

foreach ($dataSourceNode in $inputDocument.workbook.datasources.datasource)
{

    #---------------------------------------------------------------------
    # Edit connections
    foreach ($namedConnectionNode in $dataSourceNode.connection."named-connections"."named-connection")
    {
        $connectionNode = $namedConnectionNode.connection
        $connectionNode

        $connectionNode.directory

        [array]$directoryTokens = $connectionNode.directory -split "/"
        $connectionNode.directory = "./" + $directoryTokens[$directoryTokens.length - 1]
        $connectionNode.directory
    }

    #---------------------------------------------------------------------
    # Edit extracts
    $extractNode = $dataSourceNode.extract.connection
    $extractNode

    $extractNode.dbname
    [array]$tokens = $extractNode.dbname -split "/"
    $extractNode.dbname = "./" + $tokens[$tokens.count - 1]
    $extractNode.dbname

}


#---------------------------------------------------------------------
# Save input file
$inputDocument.Save($outputFile)