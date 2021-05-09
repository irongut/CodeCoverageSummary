# Code Coverage Summary Action

WORK IN PROGRESS

A GitHub Action to analyse test code coverage in Cobertura format and output a summary.

Written for use with [Coverlet](https://github.com/coverlet-coverage/coverlet) on .Net but it should work with any tests that output coverage in Cobertura format.

## Inputs

#### `filename`
**Required**

Code coverage file to analyse.

Note: Coverlet creates the coverage file in a random named directory (guid) so you need to copy it to a predictable path before running this Action, see [.Net 5 Workflow Example](#net-5-workflow-example) below.

#### `badge`

Include a badge in the output using [shields.io](https://shields.io/) - `true` or `false` (default).

If code coverage is less than 50% the badge will be red, if coverage is 50% - 74% it will be yellow and if coverage is 75% or over it will be green. 

#### `format`

Output Format - `markdown` or `text` (default).

#### `output`

Output Type - `console` (default), `file` or `both`.

`console` will output the coverage summary to the GitHub Action log.

`file` will output the coverage summary to `code-coverage-results.txt` or `code-coverage-results.md` in the workflow's working directory.

## Outputs

Code coverage summary in plain text or markdown format to the console (action log) or a file.

```
Code Coverage File: coverage/coverage.cobertura.xml
https://img.shields.io/badge/Code%20Coverage-77%25-success?style=flat
Line Rate = 77%, Lines Covered = 1107 / 1433
Branch Rate = 60%, Branches Covered = 321 / 532
Complexity = 917
Company.Example: Line Rate = 78%, Branch Rate = 60%, Complexity = 906
Company.Example.Library: Line Rate = 27%, Branch Rate = 100%, Complexity = 11
```

## Usage

```yaml
name: Code Coverage Summary Report
uses: irongut/CodeCoverageSummary@master
with:
  filename: coverage/coverage.cobertura.xml
```

### .Net 5 Workflow Example

```yaml
name: CI Build

on:
  push:
    branches: [ master ]
  pull_request:
    branches: [ master ]

jobs:
  build:
    runs-on: ubuntu-latest
    name: CI Build
    steps:
    - name: Checkout
      uses: actions/checkout@v2

    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 5.0.x

    - name: Restore Dependencies
      run: dotnet restore src/Example.sln

    - name: Build
      run: dotnet build src/Example.sln --configuration Release --no-restore

    - name: Test
      run: dotnet test src/Example.sln --configuration Release --no-build --verbosity normal --collect:"XPlat Code Coverage" --results-directory ./coverage

    - name: Copy Coverage To Predictable Location
      run: cp coverage/**/coverage.cobertura.xml coverage/coverage.cobertura.xml

    - name: Code Coverage Summary Report
      uses: irongut/CodeCoverageSummary@master
      with:
        filename: coverage/coverage.cobertura.xml
        badge: 'true'
```
