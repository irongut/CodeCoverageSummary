# Code Coverage Summary

A GitHub Action that reads Cobertura format code coverage files from your test suite and outputs a text or markdown summary. This summary can then be posted as a Pull Request comment, included in Release Notes, etc by another action to give you an immediate insight into the health of your code without using a third-party site.

Code Coverage Summary was designed for use with [Coverlet](https://github.com/coverlet-coverage/coverlet) and .Net but it should work with any test framework that outputs coverage in Cobertura format.

## Inputs

#### `filename`
**Required**

Code coverage file to analyse.

Note: Coverlet creates the coverage file in a random named directory (guid) so you need to copy it to a predictable path before running this Action, see the [.Net 5 Workflow Example](#net-5-workflow-example) below.

#### `badge`

Include a badge reporting the Line Rate coverage in the output using [shields.io](https://shields.io/) - `true` or `false` (default).

If the overall Line Rate is less than 50% the badge will be red, if it is 50% - 74% it will be yellow and if it is 75% or over it will be green. 

#### `format`

Output Format - `markdown` or `text` (default).

#### `output`

Output Type - `console` (default), `file` or `both`.

`console` will output the coverage summary to the GitHub Action log.

`file` will output the coverage summary to `code-coverage-results.txt` for text or `code-coverage-results.md` for markdown format in the workflow's working directory.

`both` will output the coverage summary to the Action log and a file as above.

## Outputs

#### Text Example
```
https://img.shields.io/badge/Code%20Coverage-77%25-success?style=flat
Line Rate = 77%, Lines Covered = 1107 / 1433
Branch Rate = 60%, Branches Covered = 321 / 532
Complexity = 917
Company.Example: Line Rate = 78%, Branch Rate = 60%, Complexity = 906
Company.Example.Library: Line Rate = 27%, Branch Rate = 100%, Complexity = 11
```

#### Markdown Example
![image](https://user-images.githubusercontent.com/27953302/117726304-4ac1c100-b1de-11eb-8d9a-6286ba1f5523.png)

## Usage

```yaml
name: Code Coverage Summary Report
uses: irongut/CodeCoverageSummary@v1.0.4
with:
  filename: coverage/coverage.cobertura.xml
```

### .Net 5 Workflow Example

```yaml
name: .Net 5 CI Build

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
      uses: irongut/CodeCoverageSummary@v1.0.4
      with:
        filename: coverage/coverage.cobertura.xml
        badge: true
        format: 'markdown'
        output: 'both'

    - name: Add Coverage PR Comment
      uses: marocchino/sticky-pull-request-comment@v2
      if: github.event_name == 'pull_request'
      with:
        recreate: true
        path: code-coverage-results.md
```

## Contributing

### Report Bugs

Please make sure the bug is not already reported by searching existing [issues].

If you're unable to find an existing issue addressing the problem please [open a new one][new-issue]. Be sure to include a title and clear description, as much relevant information as possible, a workflow sample and any logs demonstrating the problem.

### Suggest an Enhancement

Please [open a new issue][new-issue].

### Submit a Pull Request

Discuss your idea first, so that your changes have a good chance of being merged in.

Submit your pull request against the `master` branch.

Pull requests that include documentation and relevant updates to README.md are merged faster, because you won't have to wait for somebody else to complete your contribution.

## License

Code Coverage Summary is available under the MIT license, see the [LICENSE](LICENSE) file for more info.

[issues]: https://github.com/irongut/CodeCoverageSummary/issues
[new-issue]: https://github.com/irongut/CodeCoverageSummary/issues/new
