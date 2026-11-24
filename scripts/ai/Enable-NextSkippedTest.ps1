#Requires -Version 7.6
<#
.SYNOPSIS
指定グループ、またはグループ名のないSkipを一件だけ解除します。
.PARAMETER Group
Skip理由「[グループ名 001] 理由」のグループ名。省略時はグループ名なしだけを対象とします。
.PARAMETER Path
調べるC#ファイルまたはディレクトリ。相対パスはリポジトリ基準です。
.EXAMPLE
pwsh -NoProfile -File scripts/ai/Enable-NextSkippedTest.ps1 -Group 空白処理 -WhatIf
.EXAMPLE
pwsh -NoProfile -File scripts/ai/Enable-NextSkippedTest.ps1
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $Group = '',
    [string[]] $Path = @('Kotowari.Tests', 'JsonParserのテスト', 'SpracheJson.Reference.Tests')
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))

# PowerShell同梱のC#構文解析器を使い、コードの文字列・コメントを置換対象にしない。
foreach ($assembly in @('Microsoft.CodeAnalysis.dll', 'Microsoft.CodeAnalysis.CSharp.dll')) {
    [Reflection.Assembly]::LoadFrom((Join-Path $PSHOME $assembly)) | Out-Null
}

function Get-SourceFile([string] $Location) {
    $fullPath = if ([IO.Path]::IsPathRooted($Location)) {
        [IO.Path]::GetFullPath($Location)
    } else {
        [IO.Path]::GetFullPath((Join-Path $repositoryRoot $Location))
    }
    $item = Get-Item -LiteralPath $fullPath
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "リンクは対象にできません: $fullPath" }
    if (-not $item.PSIsContainer) {
        if ($item.Extension -ne '.cs') { throw "C#ファイルではありません: $fullPath" }
        return $item.FullName
    }
    foreach ($child in Get-ChildItem -LiteralPath $fullPath) {
        if ($child.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
        if ($child.PSIsContainer) {
            if ($child.Name -notin @('bin', 'obj', 'artifacts', 'packages', 'TestResults', 'node_modules')) {
                Get-SourceFile $child.FullName
            }
        } elseif ($child.Extension -eq '.cs') {
            $child.FullName
        }
    }
}

function Get-LiteralReason($Expression) {
    if ($Expression -is [Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax] -and
        $Expression.Token.Value -is [string]) {
        return $Expression.Token.ValueText
    }
    throw 'Skip理由は文字列リテラルで指定してください。式や定数参照は評価しません。'
}

function Test-TheoryDataRow($Creation) {
    if ($Creation -is [Microsoft.CodeAnalysis.CSharp.Syntax.ObjectCreationExpressionSyntax]) {
        return $Creation.Type.ToString() -match '(^|[.:])TheoryDataRow(?:<.*>)?$'
    }
    if ($Creation -is [Microsoft.CodeAnalysis.CSharp.Syntax.ImplicitObjectCreationExpressionSyntax]) {
        foreach ($ancestor in $Creation.Ancestors()) {
            $type = if ($ancestor -is [Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax] -or
                $ancestor -is [Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclarationSyntax]) {
                $ancestor.Type
            } elseif ($ancestor -is [Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax]) {
                $ancestor.ReturnType
            }
            if ($null -ne $type -and $type.ToString() -match '\bTheoryDataRow(?:<|\b)') { return $true }
            if ($ancestor -is [Microsoft.CodeAnalysis.CSharp.Syntax.MemberDeclarationSyntax]) { break }
        }
    }
    return $false
}

function Get-RemovalSpan($Items, $Item, $SingleSpan) {
    $index = $Items.IndexOf($Item)
    if ($Items.Count -eq 1) { return $SingleSpan }
    if ($index -lt $Items.Count - 1) {
        return [Microsoft.CodeAnalysis.Text.TextSpan]::FromBounds($Item.Span.Start, $Items.GetSeparator($index).Span.End)
    }
    return [Microsoft.CodeAnalysis.Text.TextSpan]::FromBounds($Items.GetSeparator($index - 1).Span.Start, $Item.Span.End)
}

function New-Candidate($Reason, $Node, $Span, $File, $Text, $Bytes, $Tree, $Kind) {
    $match = [regex]::Match($Reason, '^\[(?:(?<group>.+?)\s+)?(?<number>[0-9]+)\]\s*(?<reason>.*)$')
    if (-not $match.Success) {
        $match = [regex]::Match($Reason, '^(?:(?<group>.+?)\s+)?(?<number>[0-9]+):\s*(?<reason>.*)$')
    }
    $name = ''
    $number = $null
    if ($match.Success) {
        $name = $match.Groups['group'].Value
        $parsed = 0
        if (-not [int]::TryParse($match.Groups['number'].Value, [ref] $parsed) -or $parsed -lt 1) {
            throw "Skip番号は正の整数にしてください: $File / $Reason"
        }
        $number = $parsed
    }
    [pscustomobject]@{
        Group = $name; Number = $number; Reason = $Reason; File = $File
        Line = $Tree.GetLineSpan($Node.Span).StartLinePosition.Line + 1
        Kind = $Kind; Span = $Span; Text = $Text; Bytes = $Bytes; Node = $Node
    }
}

$utf8 = [Text.UTF8Encoding]::new($false, $true)
$files = @($Path | ForEach-Object { Get-SourceFile $_ } | Sort-Object -Unique)
$candidates = @(
    foreach ($file in $files) {
        $bytes = [IO.File]::ReadAllBytes($file)
        # BOMも文字として保持し、変更した部分以外のバイト列を維持する。
        $text = $utf8.GetString($bytes)
        $tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($text)
        $errors = @($tree.GetDiagnostics() | Where-Object Severity -EQ Error)
        if ($errors.Count) { throw "C#構文エラーがあるため変更しません: $file / $($errors[0])" }
        foreach ($node in $tree.GetRoot().DescendantNodes()) {
            if ($node -is [Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax]) {
                $name = ($node.Name.ToString() -split '\.')[-1] -replace 'Attribute$', ''
                if ($name -notin @('Fact', 'Theory', 'InlineData', 'MemberData', 'ClassData') -or
                    $null -eq $node.ArgumentList) { continue }
                foreach ($argument in $node.ArgumentList.Arguments) {
                    if ($argument.NameEquals.Name.Identifier.ValueText -ne 'Skip') { continue }
                    if ($argument.Expression.ToString() -eq 'null') { continue }
                    $reason = Get-LiteralReason $argument.Expression
                    $span = Get-RemovalSpan $node.ArgumentList.Arguments $argument $node.ArgumentList.Span
                    New-Candidate $reason $node $span $file $text $bytes $tree $name
                }
            } elseif ($node -is [Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax] -and
                $node.Left.ToString() -eq 'Skip' -and
                $node.Parent -is [Microsoft.CodeAnalysis.CSharp.Syntax.InitializerExpressionSyntax] -and
                (Test-TheoryDataRow $node.Parent.Parent)) {
                if ($node.Right.ToString() -eq 'null') { continue }
                $reason = Get-LiteralReason $node.Right
                $span = Get-RemovalSpan $node.Parent.Expressions $node $node.Span
                New-Candidate $reason $node $span $file $text $bytes $tree 'TheoryDataRow'
            } elseif ($node -is [Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax] -and
                $node.Expression -is [Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax] -and
                $node.Expression.Name.Identifier.ValueText -eq 'WithSkip') {
                $receiver = $node.Expression.Expression
                $creation = $receiver
                while ($creation -is [Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax] -and
                    $creation.Expression -is [Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax]) {
                    $creation = $creation.Expression.Expression
                }
                if (-not (Test-TheoryDataRow $creation)) { continue }
                if ($node.ArgumentList.Arguments.Count -ne 1) { throw "WithSkipの引数を確認してください: $file" }
                if ($node.ArgumentList.Arguments[0].Expression.ToString() -eq 'null') { continue }
                $reason = Get-LiteralReason $node.ArgumentList.Arguments[0].Expression
                $span = [Microsoft.CodeAnalysis.Text.TextSpan]::FromBounds($receiver.Span.End, $node.Span.End)
                New-Candidate $reason $node $span $file $text $bytes $tree 'WithSkip'
            }
        }
    }
)

$eligible = @($candidates | Where-Object { $_.Group -ceq $Group })
if (-not $eligible.Count) { throw "該当するSkipがありません。グループ省略時はグループ名なしだけが対象です。指定: '$Group'" }
$duplicates = @($eligible | Where-Object { $null -ne $_.Number } | Group-Object Number | Where-Object Count -GT 1)
if ($duplicates.Count) { throw "グループ内のSkip番号が重複しています: '$Group' / $($duplicates.Name -join ', ')" }
$selected = $eligible | Sort-Object @{ Expression = { $null -eq $_.Number } }, Number, File,
    @{ Expression = { $_.Node.Span.Start } } | Select-Object -First 1

# Theory全体のSkipがあると、データ側だけを解除しても実行されない。
if ($selected.Kind -in @('InlineData', 'MemberData', 'ClassData')) {
    $method = $selected.Node.Ancestors() |
        Where-Object { $_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax] } |
        Select-Object -First 1
    foreach ($attribute in $method.AttributeLists.Attributes) {
        if ($attribute.Name.ToString() -match '(^|\.)Theory(Attribute)?$') {
            foreach ($argument in $attribute.ArgumentList.Arguments) {
                if ($argument.NameEquals.Name.Identifier.ValueText -eq 'Skip' -and
                    $argument.Expression.ToString() -ne 'null') {
                    throw 'Theory全体もSkipされています。データの実行を妨げるSkipを先に整理してください。'
                }
            }
        }
    }
}
$updated = $selected.Text.Remove($selected.Span.Start, $selected.Span.Length)
$newTree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($updated)
if (@($newTree.GetDiagnostics() | Where-Object Severity -EQ Error).Count) { throw '解除後のC#構文に問題があるため変更しません。' }
$changed = $false
if ($PSCmdlet.ShouldProcess("$($selected.File):$($selected.Line)", "Skipを一件解除: $($selected.Reason)")) {
    if (-not [Linq.Enumerable]::SequenceEqual([IO.File]::ReadAllBytes($selected.File), $selected.Bytes)) {
        throw '解析中にファイルが変わったため書き込みません。'
    }
    [IO.File]::WriteAllBytes($selected.File, $utf8.GetBytes($updated))
    $changed = $true
}
[pscustomobject]@{
    Group = $selected.Group; Number = $selected.Number; Reason = $selected.Reason
    File = $selected.File; Line = $selected.Line; Kind = $selected.Kind; Changed = $changed
}
